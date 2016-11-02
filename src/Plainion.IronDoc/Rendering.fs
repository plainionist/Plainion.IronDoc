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

    let ifNotEmpty (seq,f) =
        match Seq.isEmpty seq with
        | true -> ()
        | false -> f seq

    let (>>=) m f = ifNotEmpty(m,f)

    let renderInline inl =
        let nl = Environment.NewLine
    
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
                  
    let renderMembersOfKind (writer:TextWriter) level (headline : string) members = 
        writer.WriteLine()
        renderHeadline writer level headline

        members
        |> Seq.iter(fun (name,doc) ->
            renderHeadline writer (level+1) name

            renderApiDoc writer doc (renderHeadline writer (level+2))
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

    let renderType (writer:TextWriter) dtype = 
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

            let renderMembers = renderMembersOfKind writer level

            dtype.fields
            |> List.map(fun x -> x.fieldType.FullName + " " + x.name, Field(x) |> getDoc )
            >>= renderMembers "Fields" 

            dtype.constructors
            |> List.map(fun x -> "Constructor(" + (getParameterSignature x.parameters) + ")", Constructor(x) |> getDoc )
            >>= renderMembers "Constructors"

            dtype.properties 
            |> List.map(fun x -> x.propertyType.FullName + " " + x.name, Property(x) |> getDoc)
            >>= renderMembers "Properties"
    
            dtype.events
            |> List.map(fun x -> x.eventHandlerType.FullName + " " + x.name, Event(x) |> getDoc )
            >>= renderMembers "Events"

            dtype.methods
            |> List.map(fun x -> getMethodSignature x, Method(x) |> getDoc ) 
            >>= renderMembers "Methods"

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

    let renderAssembly getTextWriter getUri getSummaryWriter (dassembly:DAssembly) = 
        let dtypes = dassembly.assembly.GetTypes()
                     |> Seq.filter (fun t -> t.IsPublic)
                     |> Seq.map (createDType dassembly)

        dtypes
        |> Seq.iter(fun x ->
            use writer = getTextWriter x
            renderType writer x )   

        use writer = getSummaryWriter dassembly
        renderHeadline writer 1 dassembly.name

        dtypes
        |> Seq.groupBy(fun x -> x.nameSpace )
        |> Seq.iter(fun x -> 
            renderHeadline writer 2 (fst x)
            writer.WriteLine()

            (snd x)
            |> Seq.iter(fun dtype -> writer.WriteLine( sprintf "* [%s](%s)" dtype.name (getUri dtype)))
        )