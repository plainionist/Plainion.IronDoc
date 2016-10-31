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

type Member = { Name : string
                Doc : ApiDoc }

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

let processType (ctx : TransformationContext) dtype = 
    ctx.Writer.WriteLine()
    ctx.Writer.Write "## "
    ctx.Writer.WriteLine( getFullName dtype )

    ctx.Writer.WriteLine("**Namespace:** {0}", dtype.Namespace)
    ctx.Writer.WriteLine("**Assembly:** {0}", dtype.Assembly)

    let getDoc = getXmlDocumentation ctx.Doc
            
    let getParametersSignature parameters = 
        match parameters with
        | [] -> ""
        | _ -> 
            "(" + (parameters
                    |> Seq.map (fun p -> p.parameterType.FullName)
                    |> String.concat ",")
            + ")"
    
    let declaringTypeFullName = getFullName dtype

    processMemberDoc ctx { Name = dtype.Name
                           Doc = getMemberId dtype |> getDoc } 2

    let getParameterSignature parameters = 
        parameters
        |> Seq.map( fun p -> p.parameterType.ToString() )
        |> String.concat ","

    // TODO: use "Member" and "DType" as API to ApiDoc

    dtype.Fields
    |> Seq.map(fun x -> { Name = x.fieldType.FullName + " " + x.name
                          Doc = declaringTypeFullName + "." + x.name |> sprintf "F:%s" |> getDoc } )
    |> processMembers ctx "Fields"

    processMembers
        ctx 
        "Constructors" 
        ( dtype.Constructors
          |> Seq.map(fun x -> { Name = "Constructor(" + (getParameterSignature x.parameters) + ")"
                                Doc = declaringTypeFullName + "." + "#ctor" + getParametersSignature x.parameters |> sprintf "M:%s" |> getDoc } ) )

    processMembers
        ctx 
        "Properties" 
        ( dtype.Properties 
          |> Seq.map(fun x -> { Name = x.propertyType.FullName + " " + x.name
                                Doc = declaringTypeFullName + "." + x.name |> sprintf "P:%s" |> getDoc } ) )
    
    processMembers
        ctx 
        "Events" 
        ( dtype.Events
          |> Seq.map(fun x -> { Name = x.eventHandlerType.FullName + " " + x.name
                                Doc = declaringTypeFullName + "." + x.name |> sprintf "E:%s" |> getDoc } ) )

    let getMethodSignature m =
        let returnType =
            match m.returnType with 
            | t when t = typeof<System.Void> -> "void" 
            | _ -> m.returnType.FullName

        returnType + " " + m.name + "(" + (getParameterSignature m.parameters)+ ")"

    processMembers
        ctx 
        "Methods" 
        ( dtype.Methods
          |> Seq.map(fun x -> { Name = getMethodSignature x
                                Doc = declaringTypeFullName + "." + x.name + getParametersSignature x.parameters |> sprintf "M:%s" |> getDoc } ) )

//    processMembers
//        ctx 
//        "Nested Types" 
//        ( dtype.NestedTypes
//          |> Seq.map(fun x -> { Name = getMethodSignature x
//                                Doc = declaringTypeFullName + "." + x.name + getMethodParameterSignature mi |> sprintf "M:%s" |> getDoc } ) )
//
//// t.Namespace + "." + mi.DeclaringType.Name + "." + mi.Name |> sprintf "T:%s"
//
