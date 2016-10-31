// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open System.Reflection
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering

let generateTypeDoc t xmlDoc writer = 
    let ctx = { Writer = writer
                Doc = xmlDoc }
    render ctx t
    
let generateAssemblyDoc (assembly : Assembly) xmlDoc writer = 
    let ctx = { Writer = writer
                Doc = xmlDoc }

    writer.Write "# "
    writer.WriteLine xmlDoc.AssemblyName

    let renderType = render ctx

    assembly.GetTypes()
    |> Seq.filter (fun t -> t.IsPublic)
    |> Seq.map createDType
    |> Seq.iter renderType
    
let generateAssemblyFileDoc assemblyFile outputFolder = 
    let assembly = assemblyLoader.Load assemblyFile
    let xmlDocFile = Path.ChangeExtension(assemblyFile, ".xml")
        
    if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

    use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
    let doc = loadApiDocFile xmlDocFile
    generateAssemblyDoc assembly doc writer

