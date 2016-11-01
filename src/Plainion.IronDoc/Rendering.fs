// generating Markdown API doc
// 
// general approach: we render 
// - all public/protected types and members 
// - only the given documentation per member. If e.g. no "see also" tags given this section will also not be rendered
//   as it would anyway not give any value
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

    let nl = Environment.NewLine

    let ifNotEmpty (seq,f) =
        match Seq.isEmpty seq with
        | true -> ()
        | false -> f seq

    let (>>=) m f = ifNotEmpty(m,f)

    let processText txt =
        // TODO: insert NewLine after e.g. 100 characters
        normalizeSpace txt  

    let processSee (cref:string) =
        sprintf "*See:* %s" (cref.Trim())

    let processParameterRef (name:string) =
        sprintf "*%s*" name

    let processTypeParameterRef (name:string) =
        sprintf "_%s_" name

    let processC (str:string) =
        sprintf "`%s`" (str.Trim())

    let processCode (str:string) =
        sprintf "%s```%s%s%s```%s" nl nl (str.Trim()) nl nl

    let processPara (str:string) =
        sprintf "%s%s%s" nl (processText str) nl

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

    let processInlines (writer:TextWriter) (items:Inline list) =
        writer.WriteLine ()

        items
        |> Seq.map processInline
        |> Seq.iter(fun x -> x.TrimEnd() >>= writer.WriteLine)
    
    let renderHeadline (writer:TextWriter) headlineMarker headline =
        writer.WriteLine ()
        writer.WriteLine ( headlineMarker + " " + headline)

    let renderCRefDescriptions (writer:TextWriter) items =
        items 
        |> Seq.iter( fun x -> 
            writer.WriteLine ()

            let (CRef crefValue) = x.cref
            let cref = substringAfter crefValue ':' 
            writer.WriteLine("*{0}*", cref)
            writer.WriteLine x.description
        )

    let processParameters (writer:TextWriter) headline (items:CRefDescription list) =
        headline "Parameters"
        items |> renderCRefDescriptions writer

    let processReturns (writer:TextWriter) headline (items:Inline list) =
        headline "Return value"
        items |> processInlines writer 

    let processExceptions (writer:TextWriter) headline (items:CRefDescription list) =
        headline "Exceptions"
        items |> renderCRefDescriptions writer

    let processRemarks (writer:TextWriter) headline (items:Inline list) =
        headline "Remarks"
        items |> processInlines writer 

    let processExample (writer:TextWriter) headline (items:Inline list) =
        headline "Example"
        items |> processInlines writer 

    let processPermissions (writer:TextWriter) headline (items:CRefDescription list) =
        headline "Permissions"
        items |> renderCRefDescriptions writer

    let processSeeAlso (writer:TextWriter) headline (doc:ApiDoc) =
        let seeAlso = [ doc.Summary
                        doc.Remarks
                        doc.Returns
                        doc.Example
                      ]
                      |> Seq.concat
                      |> Seq.choose(fun x -> match x with
                                             | SeeAlso i -> Some i
                                             | _ -> None  )
                      |> Seq.distinct
                      |> List.ofSeq

        if seeAlso.Length > 0 then
            headline "See also"

            seeAlso 
            |> Seq.iter(fun x -> 
                writer.WriteLine ()
    
                let (CRef crefValue) = x
                let cref = substringAfter crefValue ':' 
                writer.WriteLine("* " + cref)
            )

    let processMemberDoc (writer:TextWriter) doc headline = 
        doc.Summary >>= processInlines writer 

        doc.Params >>= processParameters writer headline
        doc.Returns >>= processReturns writer headline
        doc.Exceptions >>= processExceptions writer headline
        doc.Remarks >>= processRemarks writer headline
        doc.Example >>= processExample writer headline
        doc.Permissions >>= processPermissions writer headline

        doc |> processSeeAlso writer headline
                  
    let renderMembers (writer:TextWriter) (headline : string) members = 
        writer.WriteLine()
        renderHeadline writer "##" headline

        members
        |> Seq.iter(fun m ->
            renderHeadline writer "###" m.Name

            processMemberDoc writer m.Doc (renderHeadline writer "####")
        )

    let renderTypeHeader (writer:TextWriter) (dtype,doc) =
        renderHeadline writer "#" ( getFullName dtype )

        writer.WriteLine()
        writer.WriteLine("**Namespace:** {0}", dtype.nameSpace)
        writer.WriteLine()
        writer.WriteLine("**Assembly:** {0}", dtype.assembly.name)

        processMemberDoc writer doc (renderHeadline writer "##")

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

        renderTypeHeader writer (dtype, MemberType.Type(dtype) |> getDoc)

        dtype.fields
        |> List.map(fun x -> { Name = x.fieldType.FullName + " " + x.name
                               Doc = Field(x) |> getDoc } )
        >>= renderMembers writer "Fields"

        dtype.constructors
        |> List.map(fun x -> { Name = "Constructor(" + (getParameterSignature x.parameters) + ")"
                               Doc = Constructor(x) |> getDoc } )
        >>= renderMembers writer "Constructors" 

        dtype.properties 
        |> List.map(fun x -> { Name = x.propertyType.FullName + " " + x.name
                               Doc = Property(x) |> getDoc } )
        >>= renderMembers writer "Properties" 
    
        dtype.events
        |> List.map(fun x -> { Name = x.eventHandlerType.FullName + " " + x.name
                               Doc = Event(x) |> getDoc } )
        >>= renderMembers writer "Events" 

        dtype.methods
        |> List.map(fun x -> { Name = getMethodSignature x
                               Doc = Method(x) |> getDoc } ) 
        >>= renderMembers writer "Methods" 

    //    processMembers
    //        ctx 
    //        "Nested Types" 
    //        ( dtype.NestedTypes
    //          |> Seq.map(fun x -> { Name = getMethodSignature x
    //                                Doc = declaringTypeFullName + "." + x.name + getMethodParameterSignature mi |> sprintf "M:%s" |> getDoc } ) )
    //
