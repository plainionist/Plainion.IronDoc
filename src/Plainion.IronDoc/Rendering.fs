namespace Plainion.IronDoc.FSharp

open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Reflection
open System.Xml.Linq

type internal TransformationContext = 
    { Loader : AssemblyLoaderApi
      Writer : TextWriter
      Doc : XmlDocDocument.Contents }

module internal Transformer = 
    let private (!!) : string -> XName = Interop.implicit

    let substringAfter ( value : string ) ( sep : char ) =
        let pos = value.IndexOf (sep)
        value.Substring(pos + 1)

    let normalizeSpace (value : string) =
        Regex.Replace(value.Trim(), @"\s+", " ")

    let tryProcessSeeAlso ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine "> *See also: "
            ctx.Writer.WriteLine ( e.Attribute(!!"cref").Value.ToString() )

    let tryProcessSee ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine ()
            ctx.Writer.Write "> *See: "
            ctx.Writer.Write ( e.Attribute(!!"cref").Value )
            ctx.Writer.WriteLine "*"

    let tryProcessPermission ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine ()
            ctx.Writer.Write "**Permission:** *"
            ctx.Writer.Write ( e.Attribute(!!"cref").Value )
            ctx.Writer.WriteLine "*"
            ctx.Writer.WriteLine (normalizeSpace e.Value )

    let tryProcessParameterRef ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine "*"
            ctx.Writer.Write ( e.Attribute(!!"name").Value )
            ctx.Writer.WriteLine "*"

    let tryProcessInclude ctx (e:XElement) =
        if e <> null then
            ctx.Writer.Write "[External file]({"
            ctx.Writer.Write ( e.Attribute(!!"file").Value)
            ctx.Writer.WriteLine "})"

    let tryProcessCode ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine()
            ctx.Writer.Write "`"
            ctx.Writer.Write e.Value
            ctx.Writer.WriteLine "`"

    let tryProcessC ctx (e:XElement) =
        if e <> null then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine "```"
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ( normalizeSpace e.Value )
            ctx.Writer.WriteLine "```"

    let processMemberDoc ctx (memb :XElement) (level :int) = 
        memb.Elements(!!"summary")
        |> Seq.iter( fun s -> 
            ctx.Writer.WriteLine(normalizeSpace s.Value )

            memb.Elements(!!"para")
            |> Seq.iter( fun p -> ctx.Writer.WriteLine(normalizeSpace p.Value))
        )

        let headlineMarker = "#".PadLeft((level + 1), '#')

        if memb.Elements(!!"remarks").Any() then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ("> " + headlineMarker + " Remarks")

            memb.Elements(!!"remarks")
            |> Seq.iter( fun r -> 
                ctx.Writer.WriteLine ()
                ctx.Writer.Write "> "
                ctx.Writer.WriteLine (normalizeSpace r.Value )
            )

        if memb.Elements(!!"param").Any() then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ("> " + headlineMarker + " Parameters")

            memb.Elements(!!"param")
            |> Seq.iter( fun p -> 
                ctx.Writer.WriteLine ()

                ctx.Writer.Write "> **"

                ctx.Writer.Write ( if p.Attribute(!!"name") <> null then "" else p.Attribute(!!"name").Value )

                ctx.Writer.Write ":**  " 
                ctx.Writer.Write ( normalizeSpace p.Value )
                ctx.Writer.WriteLine ()
            )

        if memb.Elements(!!"returns").Any() then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ("> " + headlineMarker + " Return value")

            memb.Elements(!!"returns")
            |> Seq.iter( fun r -> 
                ctx.Writer.WriteLine ()
                ctx.Writer.Write "> "
                ctx.Writer.WriteLine (normalizeSpace r.Value );
            )

        if memb.Elements(!!"exception").Any() then
            ctx.Writer.WriteLine();
            ctx.Writer.WriteLine("> " + headlineMarker + " Exceptions");

            memb.Elements(!!"exception")
            |> Seq.iter( fun ex -> 
                ctx.Writer.WriteLine ()

                ctx.Writer.Write "> **"

                let cref = substringAfter ( ex.Attribute(!!"cref").Value) ':' 
                ctx.Writer.Write cref

                ctx.Writer.Write ":**  "
                ctx.Writer.Write ex.Value
                ctx.Writer.WriteLine ()
            )

        if memb.Elements(!!"example").Any() then
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ("> " + headlineMarker + " Example")
            ctx.Writer.WriteLine ()
            ctx.Writer.WriteLine ">"
            
            memb.Elements(!!"example")
            |> Seq.iter( fun e -> tryProcessC ctx e )

        tryProcessC ctx (memb.Element(!!"c"))
        tryProcessCode ctx (memb.Element(!!"code"))
        tryProcessInclude ctx (memb.Element(!!"include"))
        tryProcessParameterRef ctx (memb.Element(!!"paramref"))
        tryProcessPermission ctx (memb.Element(!!"permission"))
        tryProcessSee ctx (memb.Element(!!"see"))
        tryProcessSeeAlso ctx (memb.Element(!!"seealso"))

    let tryProcessMemberDoc ctx memb level = 
        if memb <> null then processMemberDoc ctx memb level

    let processMember ctx memb ( getMemberName : _ -> string) =
        ctx.Writer.WriteLine()

        ctx.Writer.Write "#### "
        ctx.Writer.WriteLine ( getMemberName memb )

        ctx.Writer.WriteLine()

        tryProcessMemberDoc ctx ( ctx.Doc.GetXmlDocumentation memb ) 4

    let processMembers ctx ( headline : string ) allMembers getMemberName = 
        let members = allMembers |> List.ofSeq
        
        if not ( List.isEmpty members ) then
            ctx.Writer.WriteLine()
            ctx.Writer.Write "### "
            ctx.Writer.WriteLine headline

            members
            |> Seq.iter ( fun m -> processMember ctx m getMemberName)

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

    let processType (ctx : TransformationContext) (t : Type) = 
        ctx.Writer.WriteLine()
        ctx.Writer.Write "## "
        ctx.Writer.WriteLine t.FullName

        tryProcessMemberDoc ctx (ctx.Doc.GetXmlDocumentation t) 2

        let bindingFlags = BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.DeclaredOnly

        processMembers
            ctx 
            "Fields" 
            (t.GetFields(bindingFlags) |> Seq.filter (fun m -> not m.IsPrivate)) 
            (fun field  -> field.FieldType.FullName + " " + field.Name)

        processMembers
            ctx
            "Constructors"
            (t.GetConstructors(bindingFlags) |> Seq.filter( fun m -> not m.IsPrivate))
            (fun m -> getMemberName m)

        processMembers
            ctx
            "Properties"
            (t.GetProperties(bindingFlags) |> Seq.filter( fun m -> not m.GetMethod.IsPrivate))
            ( fun property -> property.PropertyType.FullName + " " + property.Name )

        processMembers
            ctx
            "Events"
            (t.GetEvents(bindingFlags) |> Seq.filter( fun m -> not m.AddMethod.IsPrivate))
            ( fun evt -> evt.EventHandlerType.FullName + " " + evt.Name)

        processMembers
            ctx
            "Methods"
            (t.GetMethods(bindingFlags) |> Seq.filter( fun m -> not m.IsSpecialName) |> Seq.filter( fun m -> not m.IsPrivate))
            (fun m -> getMemberName m)

type XmlDocTransformer(loader : AssemblyLoaderApi) = 
    let myLoader = loader
    
    member this.TransformType t xmlDoc writer = 
        let ctx = 
            { Loader = myLoader
              Writer = writer
              Doc = xmlDoc }
        Transformer.processType ctx t
    
    member this.TransformAssembly (assembly : Assembly) xmlDoc writer = 
        let ctx = 
            { Loader = myLoader
              Writer = writer
              Doc = xmlDoc }

        writer.Write "# "
        writer.WriteLine xmlDoc.AssemblyName

        assembly.GetTypes()
        |> Seq.filter (fun t -> t.IsPublic)
        |> Seq.iter (fun t -> Transformer.processType ctx t)
    
    member this.TransformFile assemblyFile outputFolder = 
        let assembly = myLoader.Load assemblyFile
        let xmlDoc = Path.ChangeExtension(assemblyFile, ".xml")
        
        if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

        use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
        let doc = XmlDocDocument.LoadFile xmlDoc
        this.TransformAssembly assembly doc writer

