module Plainion.IronDoc.Rendering

open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Reflection
open System.Xml.Linq

type TransformationContext = 
    { Loader : AssemblyLoaderApi
      Writer : TextWriter
      Doc : XmlDocDocument }

let substringAfter ( value : string ) ( sep : char ) =
    let pos = value.IndexOf (sep)
    value.Substring(pos + 1)

let normalizeSpace (value : string) =
    Regex.Replace(value.Trim(), @"\s+", " ")

let processSeeAlso ctx (cref:string) =
    ctx.Writer.WriteLine ()
    ctx.Writer.WriteLine "> *See also: "
    ctx.Writer.WriteLine cref

let processSee ctx (cref:string) =
    ctx.Writer.WriteLine ()
    ctx.Writer.Write "> *See: "
    ctx.Writer.Write cref
    ctx.Writer.WriteLine "*"

let processPermission ctx (e:XElement) =
    ctx.Writer.WriteLine ()
    ctx.Writer.Write "**Permission:** *"
    ctx.Writer.Write ( e.Attribute(!!"cref").Value )
    ctx.Writer.WriteLine "*"
    ctx.Writer.WriteLine (normalizeSpace e.Value )

let processParameterRef ctx (name:string) =
    ctx.Writer.Write " *"
    ctx.Writer.Write name
    ctx.Writer.Write "* "

let processTypeParameterRef ctx (name:string) =
    ctx.Writer.Write " _"
    ctx.Writer.Write name
    ctx.Writer.Write "_ "

let processInclude ctx (e:XElement) =
    ctx.Writer.Write "[External file]({"
    ctx.Writer.Write ( e.Attribute(!!"file").Value)
    ctx.Writer.WriteLine "})"

let processC ctx (str:string) =
    ctx.Writer.Write "`"
    ctx.Writer.Write str
    ctx.Writer.Write "`"

let processCode ctx str =
    ctx.Writer.WriteLine ()
    ctx.Writer.WriteLine "```"
    ctx.Writer.WriteLine ()
    ctx.Writer.WriteLine ( normalizeSpace str )
    ctx.Writer.WriteLine "```"

let processPara ctx (str:string) =
    ctx.Writer.WriteLine()
    ctx.Writer.WriteLine str
    ctx.Writer.WriteLine()

let processIfNotNull ctx (e:XElement) f =
    if e <> null then f ctx e

let processXml ctx (memb :XElement) (level :int) = 
    let headlineMarker = "#".PadLeft((level + 1), '#')

    if memb.Elements(!!"remarks").Any() then
        ctx.Writer.WriteLine ()
        ctx.Writer.WriteLine ("> " + headlineMarker + " Remarks")

        memb.Elements(!!"remarks")
        |> Seq.iter( fun r -> 
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
        |> Seq.iter( fun e -> if e <> null then processC ctx e.Value )

    let tryXml name = processIfNotNull ctx (memb.Element(name))

    tryXml !!"c" (fun ctx e -> processC ctx e.Value )
    tryXml !!"code" (fun ctx e -> processCode ctx e.Value )
    tryXml !!"paramref" (fun ctx e -> processParameterRef ctx (e.Attribute(!!"name").Value) )
    tryXml !!"see" (fun ctx e -> processSee ctx (e.Attribute(!!"cref").Value) )
    tryXml !!"seealso" (fun ctx e -> processSeeAlso ctx (e.Attribute(!!"cref").Value) )
    
let processInline ctx inl =
    match inl with
    | Text x -> ctx.Writer.WriteLine(normalizeSpace x )  
    | C x -> processC ctx x  
    | Code x -> processCode ctx x  
    | Para x -> processPara ctx x  
    | ParamRef x -> let (CRef cref) = x  
                    processParameterRef ctx cref
    | TypeParamRef x -> let (CRef cref) = x  
                        processTypeParameterRef ctx cref
    | See x -> let (CRef cref) = x  
               processSee ctx cref
    | SeeAlso x -> let (CRef cref) = x  
                   processSeeAlso ctx cref

let tryProcessMemberDoc ctx memDoc level = 
    match memDoc with
    | None -> ()
    | Some doc -> doc.Summary |> Seq.iter( fun i -> processInline ctx i )
                  processXml ctx doc.Xml level
                  
let processMember ctx memb ( getMemberName : _ -> string) =
    ctx.Writer.WriteLine()

    ctx.Writer.Write "#### "
    ctx.Writer.WriteLine ( getMemberName memb )

    ctx.Writer.WriteLine()

    tryProcessMemberDoc ctx ( GetXmlDocumentation ctx.Doc memb ) 4

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

    tryProcessMemberDoc ctx (GetXmlDocumentation ctx.Doc t) 2

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

let TransformType t xmlDoc writer = 
    let ctx = 
        { Loader = assemblyLoader
          Writer = writer
          Doc = xmlDoc }
    processType ctx t
    
let TransformAssembly (assembly : Assembly) xmlDoc writer = 
    let ctx = 
        { Loader = assemblyLoader
          Writer = writer
          Doc = xmlDoc }

    writer.Write "# "
    writer.WriteLine xmlDoc.AssemblyName

    assembly.GetTypes()
    |> Seq.filter (fun t -> t.IsPublic)
    |> Seq.iter (fun t -> processType ctx t)
    
let TransformFile assemblyFile outputFolder = 
    let assembly = assemblyLoader.Load assemblyFile
    let xmlDoc = Path.ChangeExtension(assemblyFile, ".xml")
        
    if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

    use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
    let doc = LoadApiDocFile xmlDoc
    TransformAssembly assembly doc writer

