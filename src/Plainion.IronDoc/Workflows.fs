﻿// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open System.Reflection
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering

let generateTypeDoc writer t = 
    render writer t
    
let generateAssemblyDoc outputFolder (assembly:DAssembly) = 
    let assemblyFolder = Path.Combine(outputFolder, assembly.name)

    if Directory.Exists assemblyFolder then 
        Directory.Delete(assemblyFolder, true)

    Directory.CreateDirectory(assemblyFolder) |> ignore
    
    let renderType dtype =
        use writer = new StreamWriter(Path.Combine(assemblyFolder, (getFullName dtype) + ".md"))
        render writer dtype

    assembly.assembly.GetTypes()
    |> Seq.filter (fun t -> t.IsPublic)
    |> Seq.map (createDType assembly)
    |> Seq.iter renderType
    
let generateAssemblyFileDoc outputFolder assemblyFile  = 
    generateAssemblyDoc outputFolder (assemblyLoader.Load assemblyFile)

