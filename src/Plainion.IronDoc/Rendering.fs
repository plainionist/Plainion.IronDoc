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

    let renderInline inl =
        let renderText txt =
            // TODO: insert NewLine after e.g. 100 characters
            normalizeSpace txt         

        match inl with
        | Text x -> renderText x
        | C x -> sprintf "`%s`" (x.Trim())  
        | Code x -> sprintf "%s```%s%s%s```%s" nl nl (x.Trim()) nl nl  
        | Para x -> sprintf "%s%s%s" nl (renderText x) nl  
        | ParamRef x -> let (CRef cref) = x  
                        sprintf "*%s*" cref
        | TypeParamRef x -> let (CRef cref) = x  
                            sprintf "_%s_" cref
        | See x -> let (CRef cref) = x  
                   sprintf "*See:* %s" (cref.Trim())
        | SeeAlso x -> String.Empty // ignore here - will be rendered later

    let renderHeadline (writer:TextWriter) level headline =
        writer.WriteLine ()

        let marker = String.Empty.PadRight(level,'#')

        writer.WriteLine ( marker + " " + headline)

    let renderInlines (writer:TextWriter) (items:Inline list) =
        writer.WriteLine ()

        items
        |> Seq.map renderInline
        |> Seq.iter(fun x -> x.TrimEnd() >>= writer.WriteLine)
    
    let renderCRefDescriptions (writer:TextWriter) items =
        items 
        |> Seq.iter( fun x -> 
            writer.WriteLine ()

            let (CRef crefValue) = x.cref
            let cref = substringAfter crefValue ':' 
            writer.WriteLine("*{0}*", cref)
            writer.WriteLine x.description
        )

    let renderSeeAlso (writer:TextWriter) headline (doc:ApiDoc) =
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

    let renderApiDoc (writer:TextWriter) doc headline = 
        doc.Summary >>= renderInlines writer 

        let renderSection renderItems headlineText items =
            headline headlineText
            items |> renderItems writer 

        doc.Params >>= renderSection renderCRefDescriptions "Parameters"
        doc.Returns >>= renderSection renderInlines "Return value"
        doc.Exceptions >>= renderSection renderCRefDescriptions "Exceptions"
        doc.Remarks >>= renderSection renderInlines "Remarks"
        doc.Example >>= renderSection renderInlines "Example"
        doc.Permissions >>= renderSection renderCRefDescriptions "Permissions"

        doc |> renderSeeAlso writer headline
                  
    let renderMembersOfKind (writer:TextWriter) (headline : string) level members = 
        writer.WriteLine()
        renderHeadline writer level headline

        members
        |> Seq.iter(fun m ->
            renderHeadline writer (level+1) m.Name

            renderApiDoc writer m.Doc (renderHeadline writer (level+2))
        )

    let renderTypeHeader (writer:TextWriter) level (dtype,doc) =
        renderHeadline writer level ( getFullName dtype )

        writer.WriteLine()
        writer.WriteLine("**Namespace:** {0}", dtype.nameSpace)
        writer.WriteLine()
        writer.WriteLine("**Assembly:** {0}", dtype.assembly.name)

        renderApiDoc writer doc (renderHeadline writer (level+1))

[<AutoOpen>]
module Api =
    open Plainion.IronDoc

    let render (writer : TextWriter) dtype = 
        renderTypeHeader writer 1 (dtype, MemberType.Type(dtype) |> apiDocLoader.Get dtype)

        let rec renderTypeMembers (writer : TextWriter) level dtype = 
            let getDoc = apiDocLoader.Get dtype
                
            let getParameterSignature parameters = 
                parameters
                |> Seq.map( fun p -> sprintf "%s %s" (p.parameterType.ToString()) p.name )
                |> String.concat ","

            let getMethodSignature m =
                let returnType =
                    match m.returnType with 
                    | t when t = typeof<System.Void> -> "void" 
                    | _ -> m.returnType.FullName

                returnType + " " + m.name + "(" + (getParameterSignature m.parameters)+ ")"

            dtype.fields
            |> List.map(fun x -> { Name = x.fieldType.FullName + " " + x.name
                                   Doc = Field(x) |> getDoc } )
            >>= renderMembersOfKind writer "Fields" level

            dtype.constructors
            |> List.map(fun x -> { Name = "Constructor(" + (getParameterSignature x.parameters) + ")"
                                   Doc = Constructor(x) |> getDoc } )
            >>= renderMembersOfKind writer "Constructors" level

            dtype.properties 
            |> List.map(fun x -> { Name = x.propertyType.FullName + " " + x.name
                                   Doc = Property(x) |> getDoc } )
            >>= renderMembersOfKind writer "Properties" level
    
            dtype.events
            |> List.map(fun x -> { Name = x.eventHandlerType.FullName + " " + x.name
                                   Doc = Event(x) |> getDoc } )
            >>= renderMembersOfKind writer "Events" level

            dtype.methods
            |> List.map(fun x -> { Name = getMethodSignature x
                                   Doc = Method(x) |> getDoc } ) 
            >>= renderMembersOfKind writer "Methods" level

            let renderNestedTypes items =
                writer.WriteLine()
                renderHeadline writer level "Nested types"

                items
                |> Seq.iter( fun x ->
                    renderHeadline writer (level+1) (getFullName x)
                    
                    let doc = NestedType(x) |> apiDocLoader.Get dtype
                    renderApiDoc writer doc (renderHeadline writer (level+2))
                    
                    x |> renderTypeMembers writer (level+2)
                )

            dtype.nestedTypes >>= renderNestedTypes

        renderTypeMembers writer 2 dtype