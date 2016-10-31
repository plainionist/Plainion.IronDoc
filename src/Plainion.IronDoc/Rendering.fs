// generating Markdown API doc
namespace Plainion.IronDoc.Rendering

open System.IO
open Plainion.IronDoc.Parsing

type TransformationContext = 
    { Loader : AssemblyLoaderApi
      Writer : TextWriter
      Doc : XmlDocDocument }

[<AutoOpen>]
module private MarkdownImpl =
    open System
    open System.Xml.Linq
    open Plainion.IronDoc

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

[<AutoOpen>]
module Api =
    open Plainion.IronDoc

    let render (ctx : TransformationContext) dtype = 
        let getDoc = getXmlDocumentation ctx.Doc dtype
                
        let getParameterSignature parameters = 
            parameters
            |> Seq.map( fun p -> p.parameterType.ToString() )
            |> String.concat ","

        let getMethodSignature m =
            let returnType =
                match m.returnType with 
                | t when t = typeof<System.Void> -> "void" 
                | _ -> m.returnType.FullName

            returnType + " " + m.name + "(" + (getParameterSignature m.parameters)+ ")"

        ctx.Writer.WriteLine()
        ctx.Writer.Write "## "
        ctx.Writer.WriteLine( getFullName dtype )

        ctx.Writer.WriteLine("**Namespace:** {0}", dtype.nameSpace)
        ctx.Writer.WriteLine("**Assembly:** {0}", dtype.assembly)

        processMemberDoc ctx { Name = dtype.name
                               Doc = MemberType.Type(dtype) |> getDoc } 2

        dtype.fields
        |> Seq.map(fun x -> { Name = x.fieldType.FullName + " " + x.name
                              Doc = Field(x) |> getDoc } )
        |> processMembers ctx "Fields"

        dtype.constructors
        |> Seq.map(fun x -> { Name = "Constructor(" + (getParameterSignature x.parameters) + ")"
                              Doc = Constructor(x) |> getDoc } )
        |> processMembers ctx "Constructors" 

        dtype.properties 
        |> Seq.map(fun x -> { Name = x.propertyType.FullName + " " + x.name
                              Doc = Property(x) |> getDoc } )
        |> processMembers ctx "Properties" 
    
        dtype.events
        |> Seq.map(fun x -> { Name = x.eventHandlerType.FullName + " " + x.name
                              Doc = Event(x) |> getDoc } )
        |> processMembers ctx "Events" 

        dtype.methods
        |> Seq.map(fun x -> { Name = getMethodSignature x
                              Doc = Method(x) |> getDoc } ) 
        |> processMembers ctx "Methods" 

    //    processMembers
    //        ctx 
    //        "Nested Types" 
    //        ( dtype.NestedTypes
    //          |> Seq.map(fun x -> { Name = getMethodSignature x
    //                                Doc = declaringTypeFullName + "." + x.name + getMethodParameterSignature mi |> sprintf "M:%s" |> getDoc } ) )
    //
