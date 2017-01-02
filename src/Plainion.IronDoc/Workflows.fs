// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering
open System.Threading
open System

let generateTypeDoc writer t = 
    renderType writer t

/// Root folder per assembly.
/// Sub-folder per namespace
/// One file per type.
/// Summary page per namespace listing all types
/// (with readme.md from source folder at top if available)    
let generateAssemblyDoc outputFolder (assembly:DAssembly) = 
    let assemblyFolder = Path.Combine(outputFolder,assembly.name)

    if Directory.Exists assemblyFolder then 
        Directory.Delete(assemblyFolder, true)

    Directory.CreateDirectory(assemblyFolder) |> ignore
    
    // seems that dir is not instantly created
    Thread.Sleep(50)

    let namespaces =
        assembly.assembly.GetTypes()
        |> Seq.filter (fun t -> t.IsPublic)
        |> Seq.map (createDType assembly)
        |> Seq.groupBy(fun x -> x.nameSpace )
        |> List.ofSeq

    let rootNs = 
        namespaces
        |> Seq.map fst
        |> Seq.sortBy(fun x -> x.Length)
        |> Seq.head

    let renderNameSpace (ns:string,types) =
        let folder = 
            if ns.StartsWith(rootNs, StringComparison.Ordinal) then 
                let subNs = ns.Substring(rootNs.Length).Trim('.')
                Path.Combine(assemblyFolder, subNs)
            else 
                assemblyFolder

        if Directory.Exists folder |> not then 
            Directory.CreateDirectory(folder) |> ignore

        let types = types |> List.ofSeq

        // render individual type files
        types
        |> Seq.iter(fun x ->
            use writer = new StreamWriter(Path.Combine(folder, x.name + ".md"))
            renderType writer x )   

        // render summary file
        use writer =  new StreamWriter(Path.Combine(folder, "ReadMe.md")) 
        renderHeadline writer 1 ns
        writer.WriteLine()

        renderHeadline writer 2 "Types"
        writer.WriteLine()

        types
        |> Seq.iter(fun x -> writer.WriteLine( sprintf "* [%s](%s)" x.name (x.name + ".md") ))

    namespaces
    |> Seq.iter renderNameSpace

    
let generateAssemblyFileDoc outputFolder assemblyFile  = 
    generateAssemblyDoc outputFolder (assemblyLoader.Load assemblyFile)

