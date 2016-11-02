// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open System.Reflection
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering

let generateTypeDoc writer t = 
    renderType writer t
    
let generateAssemblyDoc outputFolder (assembly:DAssembly) = 
    let assemblyFolder = Path.Combine(outputFolder,assembly.name)

    if Directory.Exists assemblyFolder then 
        Directory.Delete(assemblyFolder, true)

    Directory.CreateDirectory(assemblyFolder) |> ignore
    
    assembly |> renderAssembly (fun x -> new StreamWriter(Path.Combine(assemblyFolder, (getFullName x) + ".md")))
                               (fun x -> (getFullName x) + ".md")
                               (fun x -> new StreamWriter(Path.Combine(assemblyFolder, x.name + ".md")))
    
let generateAssemblyFileDoc outputFolder assemblyFile  = 
    generateAssemblyDoc outputFolder (assemblyLoader.Load assemblyFile)

