// generating Markdown API doc
[<AutoOpen>]
module Plainion.IronDoc.Rendering

open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc.Parsing

type TransformationContext = 
    { Loader : AssemblyLoaderApi
      Writer : TextWriter
      Doc : XmlDocDocument }

let processText txt =
    normalizeSpace txt  

let nl = Environment.NewLine

let processSeeAlso (cref:string) =
    sprintf "%s> *See also: %s" nl cref

let processSee (cref:string) =
    sprintf "*See:* %s" cref

let processParameterRef (name:string) =
    sprintf "*%s*" name

let processTypeParameterRef (name:string) =
    sprintf "_%s_" name

let processC (str:string) =
    sprintf "`%s`" str

let processCode str =
    sprintf "%s```%s%s%s```%s" nl nl ( normalizeSpace str ) nl nl

let processPara (str:string) =
    sprintf "%s%s%s" nl str nl

let processIfNotNull ctx (e:XElement) f =
    if e <> null then f ctx e

let processInline inl =
    match inl with
    | Text x -> processText x
    | C x -> processC x  
    | Code x -> processCode x  
    | Para x -> processPara x  
    | ParamRef x -> let (CRef cref) = x  
                    processParameterRef cref
    | TypeParamRef x -> let (CRef cref) = x  
                        processTypeParameterRef cref
    | See x -> let (CRef cref) = x  
               processSee cref
    | SeeAlso x -> String.Empty // ignore here - will be processed later

let processMemberDoc ctx (memb:Member) (level :int) = 
    memb.Doc.Summary 
    |> Seq.map processInline
    |> Seq.iter ctx.Writer.WriteLine

    // TODO: only for types unless we generate one file per member
//    ctx.Writer.WriteLine("**Namespace:** {0}", memb.Namespace)
//    ctx.Writer.WriteLine("**Assembly:** {0}", memb.Assembly)

    let headlineMarker = "#".PadLeft((level + 1), '#')

    ctx.Writer.WriteLine ()
    ctx.Writer.WriteLine (headlineMarker + " Syntax")

    // TODO: write syntax again - later in several languages

    if memb.Doc.Params.Length > 0 then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine ( headlineMarker + "#" + " Parameters")

        memb.Doc.Params 
        |> Seq.iter( fun p -> 
            ctx.Writer.WriteLine ()

            ctx.Writer.Write "> **"

            ctx.Writer.Write p.cref

            ctx.Writer.Write ":**  " 
            ctx.Writer.Write p.description
            ctx.Writer.WriteLine ()
        )

    if memb.Doc.Returns.Length > 0 then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine (headlineMarker + "#" + " Return value")

        memb.Doc.Returns 
        |> Seq.map processInline
        |> Seq.iter ctx.Writer.WriteLine

    if memb.Doc.Exceptions.Length > 0 then
        ctx.Writer.WriteLine();
        ctx.Writer.WriteLine("> " + headlineMarker + " Exceptions");

        memb.Doc.Exceptions
        |> Seq.iter( fun ex -> 
            ctx.Writer.WriteLine ()

            ctx.Writer.Write "**"

            let (CRef crefValue) = ex.cref
            let cref = substringAfter crefValue ':' 
            ctx.Writer.Write cref

            ctx.Writer.WriteLine "**"
            ctx.Writer.Write ex.description
            ctx.Writer.WriteLine ()
        )

    if memb.Doc.Remarks.Length > 0 then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine (headlineMarker + " Remarks")

        memb.Doc.Remarks 
        |> Seq.map processInline
        |> Seq.iter ctx.Writer.WriteLine

    if memb.Doc.Example.Length > 0 then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine (headlineMarker + " Example")

        memb.Doc.Example 
        |> Seq.map processInline
        |> Seq.iter ctx.Writer.WriteLine

    if memb.Doc.Permissions.Length > 0 then
        ctx.Writer.WriteLine();
        ctx.Writer.WriteLine(headlineMarker + " Permissions");

        memb.Doc.Exceptions
        |> Seq.iter( fun ex -> 
            ctx.Writer.WriteLine ()

            ctx.Writer.Write "**"

            let (CRef crefValue) = ex.cref
            let cref = substringAfter crefValue ':' 
            ctx.Writer.Write cref

            ctx.Writer.WriteLine "**"
            ctx.Writer.Write ex.description
            ctx.Writer.WriteLine ()
        )

    let seeAlso = [ memb.Doc.Summary
                    memb.Doc.Remarks
                    memb.Doc.Returns
                    memb.Doc.Example
                  ]
                  |> Seq.concat
                  |> Seq.choose(fun x -> match x with
                                         | SeeAlso i -> Some i
                                         | _ -> None  )
                  |> Seq.distinct
                  |> List.ofSeq

    if seeAlso.Length > 0 then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine (headlineMarker + " See also")

        seeAlso 
        |> Seq.iter ctx.Writer.WriteLine
                  
let processMember ctx m =
    ctx.Writer.WriteLine()

    ctx.Writer.Write "#### "
    ctx.Writer.WriteLine m.Name

    ctx.Writer.WriteLine()

    processMemberDoc ctx m 4

let processMembers ctx (headline : string) allMembers = 
    let members = allMembers |> List.ofSeq
        
    if not ( List.isEmpty members ) then
        ctx.Writer.WriteLine()
        ctx.Writer.Write "### "
        ctx.Writer.WriteLine headline

        members
        |> Seq.iter(fun m -> processMember ctx m)

let getMethodSignatureWitoutReturnType (m:MethodBase) =
    let parameterSignature = 
        m.GetParameters()
        |> Seq.map( fun p -> p.ParameterType.ToString() )
        |> String.concat ","

    m.Name + "(" + parameterSignature + ")"

let getMethodSignature (m:MethodInfo) =
    let returnType =
        match m.ReturnType with 
        | t when t = typeof<System.Void> -> "void" 
        | _ -> m.ReturnType.FullName

    returnType + " " + getMethodSignatureWitoutReturnType m

let getMemberName (m:MemberInfo) =
    match m with
    | :? ConstructorInfo as x -> getMethodSignatureWitoutReturnType x
    | :? MethodInfo as x -> getMethodSignature x
    | _ -> "not implemented"


// TODO: separate parser from renderer further - take all required infos from reflection and put into "member descriptor" so that
//       we do not have to handle any reflection in rendering process any longer


let processType (ctx : TransformationContext) (t : Type) = 
    ctx.Writer.WriteLine()
    ctx.Writer.Write "## "
    ctx.Writer.WriteLine t.FullName

    let mt = { Assembly = t.Assembly.FullName
               Namespace = t.Namespace
               Name = t.Name
               Doc = GetXmlDocumentation ctx.Doc t }
    processMemberDoc ctx mt 2

    let bindingFlags = BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.DeclaredOnly

    processMembers
        ctx 
        "Fields" 
        ( t.GetFields(bindingFlags) 
          |> Seq.filter(fun m -> not m.IsPrivate)
          |> Seq.map(fun x -> { Assembly = mt.Assembly
                                Namespace = mt.Namespace
                                Name = x.FieldType.FullName + " " + x.Name
                                Doc = GetXmlDocumentation ctx.Doc x } ) )

    processMembers
        ctx 
        "Constructors" 
        ( t.GetConstructors(bindingFlags) 
          |> Seq.filter(fun m -> not m.IsPrivate)
          |> Seq.map(fun x -> { Assembly = mt.Assembly
                                Namespace = mt.Namespace
                                Name = getMemberName x
                                Doc = GetXmlDocumentation ctx.Doc x } ) )

    processMembers
        ctx 
        "Properties" 
        ( t.GetProperties(bindingFlags) 
          |> Seq.filter(fun m -> not m.GetMethod.IsPrivate)
          |> Seq.map(fun x -> { Assembly = mt.Assembly
                                Namespace = mt.Namespace
                                Name = x.PropertyType.FullName + " " + x.Name
                                Doc = GetXmlDocumentation ctx.Doc x } ) )
    
    processMembers
        ctx 
        "Events" 
        ( t.GetEvents(bindingFlags) 
          |> Seq.filter(fun m -> not m.AddMethod.IsPrivate)
          |> Seq.map(fun x -> { Assembly = mt.Assembly
                                Namespace = mt.Namespace
                                Name = x.EventHandlerType.FullName + " " + x.Name
                                Doc = GetXmlDocumentation ctx.Doc x } ) )

    processMembers
        ctx 
        "Methods" 
        ( t.GetMethods(bindingFlags) 
          |> Seq.filter(fun m -> not m.IsSpecialName && not m.IsPrivate)
          |> Seq.map(fun x -> { Assembly = mt.Assembly
                                Namespace = mt.Namespace
                                Name = getMemberName x
                                Doc = GetXmlDocumentation ctx.Doc x } ) )


