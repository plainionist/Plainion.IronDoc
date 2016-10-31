// generating Markdown API doc
namespace Plainion.IronDoc.Rendering

open System.IO
open Plainion.IronDoc.Parsing

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

    let processMemberDoc (writer:TextWriter) (memb:Member) (level :int) = 
        memb.Doc.Summary 
        |> Seq.map processInline
        |> Seq.iter writer.WriteLine

        let headlineMarker = "#".PadLeft((level + 1), '#')

        writer.WriteLine ()
        writer.WriteLine (headlineMarker + " Syntax")

        // TODO: write syntax again - later in several languages

        if memb.Doc.Params.Length > 0 then
            writer.WriteLine ()
            writer.WriteLine ( headlineMarker + "#" + " Parameters")

            memb.Doc.Params 
            |> Seq.iter( fun p -> 
                writer.WriteLine ()

                writer.Write "> **"

                writer.Write p.cref

                writer.Write ":**  " 
                writer.Write p.description
                writer.WriteLine ()
            )

        if memb.Doc.Returns.Length > 0 then
            writer.WriteLine ()
            writer.WriteLine (headlineMarker + "#" + " Return value")

            memb.Doc.Returns 
            |> Seq.map processInline
            |> Seq.iter writer.WriteLine

        if memb.Doc.Exceptions.Length > 0 then
            writer.WriteLine();
            writer.WriteLine("> " + headlineMarker + " Exceptions");

            memb.Doc.Exceptions
            |> Seq.iter( fun ex -> 
                writer.WriteLine ()

                writer.Write "**"

                let (CRef crefValue) = ex.cref
                let cref = substringAfter crefValue ':' 
                writer.Write cref

                writer.WriteLine "**"
                writer.Write ex.description
                writer.WriteLine ()
            )

        if memb.Doc.Remarks.Length > 0 then
            writer.WriteLine ()
            writer.WriteLine (headlineMarker + " Remarks")

            memb.Doc.Remarks 
            |> Seq.map processInline
            |> Seq.iter writer.WriteLine

        if memb.Doc.Example.Length > 0 then
            writer.WriteLine ()
            writer.WriteLine (headlineMarker + " Example")

            memb.Doc.Example 
            |> Seq.map processInline
            |> Seq.iter writer.WriteLine

        if memb.Doc.Permissions.Length > 0 then
            writer.WriteLine();
            writer.WriteLine(headlineMarker + " Permissions");

            memb.Doc.Exceptions
            |> Seq.iter( fun ex -> 
                writer.WriteLine ()

                writer.Write "**"

                let (CRef crefValue) = ex.cref
                let cref = substringAfter crefValue ':' 
                writer.Write cref

                writer.WriteLine "**"
                writer.Write ex.description
                writer.WriteLine ()
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
            writer.WriteLine ()
            writer.WriteLine (headlineMarker + " See also")

            seeAlso 
            |> Seq.iter writer.WriteLine
                  
    let processMember (writer:TextWriter) m =
        writer.WriteLine()

        writer.Write "#### "
        writer.WriteLine m.Name

        writer.WriteLine()

        processMemberDoc writer m 4

    let processMembers (writer:TextWriter) (headline : string) allMembers = 
        let members = allMembers |> List.ofSeq
        
        if not ( List.isEmpty members ) then
            writer.WriteLine()
            writer.Write "### "
            writer.WriteLine headline

            members
            |> Seq.iter(fun m -> processMember writer m)

[<AutoOpen>]
module Api =
    open Plainion.IronDoc

    let render (writer : TextWriter) dtype = 
        let getDoc = apiDocLoader.Get dtype
                
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

        writer.WriteLine()
        writer.Write "## "
        writer.WriteLine( getFullName dtype )

        writer.WriteLine("**Namespace:** {0}", dtype.nameSpace)
        writer.WriteLine("**Assembly:** {0}", dtype.assembly)

        processMemberDoc writer { Name = dtype.name
                                  Doc = MemberType.Type(dtype) |> getDoc } 2

        dtype.fields
        |> Seq.map(fun x -> { Name = x.fieldType.FullName + " " + x.name
                              Doc = Field(x) |> getDoc } )
        |> processMembers writer "Fields"

        dtype.constructors
        |> Seq.map(fun x -> { Name = "Constructor(" + (getParameterSignature x.parameters) + ")"
                              Doc = Constructor(x) |> getDoc } )
        |> processMembers writer "Constructors" 

        dtype.properties 
        |> Seq.map(fun x -> { Name = x.propertyType.FullName + " " + x.name
                              Doc = Property(x) |> getDoc } )
        |> processMembers writer "Properties" 
    
        dtype.events
        |> Seq.map(fun x -> { Name = x.eventHandlerType.FullName + " " + x.name
                              Doc = Event(x) |> getDoc } )
        |> processMembers writer "Events" 

        dtype.methods
        |> Seq.map(fun x -> { Name = getMethodSignature x
                              Doc = Method(x) |> getDoc } ) 
        |> processMembers writer "Methods" 

    //    processMembers
    //        ctx 
    //        "Nested Types" 
    //        ( dtype.NestedTypes
    //          |> Seq.map(fun x -> { Name = getMethodSignature x
    //                                Doc = declaringTypeFullName + "." + x.name + getMethodParameterSignature mi |> sprintf "M:%s" |> getDoc } ) )
    //
