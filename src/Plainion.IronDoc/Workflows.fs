// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering
open System.Threading

let generateTypeDoc writer t = 
    renderType writer t
    
let generateAssemblyDoc outputFolder (assembly:DAssembly) = 
    let assemblyFolder = Path.Combine(outputFolder,assembly.name)

    if Directory.Exists assemblyFolder then 
        Directory.Delete(assemblyFolder, true)

    Directory.CreateDirectory(assemblyFolder) |> ignore
    
    // seems that dir is not instantly created
    Thread.Sleep(50)

    let getTextWriter = fun x -> new StreamWriter(Path.Combine(assemblyFolder, (getFullName x) + ".md"))
    let getUri = fun x -> (getFullName x) + ".md"
    let getSummaryWriter =  fun (x:DAssembly) -> new StreamWriter(Path.Combine(assemblyFolder, x.name + ".md"))

    let dtypes = assembly.assembly.GetTypes()
                    |> Seq.filter (fun t -> t.IsPublic)
                    |> Seq.map (createDType assembly)

    dtypes
    |> Seq.iter(fun x ->
        use writer = getTextWriter x
        renderType writer x )   

    use writer = getSummaryWriter assembly
    renderHeadline writer 1 assembly.name

    dtypes
    |> Seq.groupBy(fun x -> x.nameSpace )
    |> Seq.iter(fun x -> 
        renderHeadline writer 2 (fst x)
        writer.WriteLine()

        (snd x)
        |> Seq.iter(fun dtype -> writer.WriteLine( sprintf "* [%s](%s)" dtype.name (getUri dtype)))
    )
    
let generateAssemblyFileDoc outputFolder assemblyFile  = 
    generateAssemblyDoc outputFolder (assemblyLoader.Load assemblyFile)

