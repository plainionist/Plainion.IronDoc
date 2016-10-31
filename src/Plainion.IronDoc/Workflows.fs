// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open System.Reflection
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering

let generateTypeDoc t writer = 
    let ctx = { Writer = writer }
    render ctx t
    
let generateAssemblyDoc (assembly : Assembly) writer = 
    let ctx = { Writer = writer }

    writer.Write "# "
    writer.WriteLine(assembly.GetName().Name)

    let renderType = render ctx

    assembly.GetTypes()
    |> Seq.filter (fun t -> t.IsPublic)
    |> Seq.map createDType
    |> Seq.iter renderType
    
let generateAssemblyFileDoc assemblyFile outputFolder = 
    let assembly = assemblyLoader.Load assemblyFile
        
    if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

    use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
    generateAssemblyDoc assembly writer

