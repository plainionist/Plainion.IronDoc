// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open Plainion.IronDoc.Parsing
open System.Reflection
open System.IO

let transformType t xmlDoc writer = 
    let ctx = 
        { Loader = assemblyLoader
          Writer = writer
          Doc = xmlDoc }
    processType ctx t
    
let transformAssembly (assembly : Assembly) xmlDoc writer = 
    let ctx = 
        { Loader = assemblyLoader
          Writer = writer
          Doc = xmlDoc }

    writer.Write "# "
    writer.WriteLine xmlDoc.AssemblyName

    assembly.GetTypes()
    |> Seq.filter (fun t -> t.IsPublic)
    |> Seq.iter (fun t -> processType ctx t)
    
let transformFile assemblyFile outputFolder = 
    let assembly = assemblyLoader.Load assemblyFile
    let xmlDocFile = Path.ChangeExtension(assemblyFile, ".xml")
        
    if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

    use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
    let doc = loadApiDocFile xmlDocFile
    transformAssembly assembly doc writer

