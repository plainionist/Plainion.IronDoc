﻿// builds up the processing-rendering pipeline
[<AutoOpen>]
module Plainion.IronDoc.Workflows

open System.IO
open Plainion.IronDoc.Parsing
open Plainion.IronDoc.Rendering
open System
open System.Diagnostics

let generateTypeDoc writer t = 
    renderType writer t

type Tree =
  | Tree of string * Tree list

/// Root folder per assembly.
/// Sub-folder per namespace
/// One file per type.
/// Summary page per namespace listing all types
/// (with readme.md from source folder at top if available)    
let generateAssemblyDoc outputFolder (assembly:DAssembly) = 
    let namespaceTypesMap =
        assembly.assembly.GetTypes()
        |> Seq.filter (fun t -> t.IsPublic)
        |> Seq.map (createDType assembly)
        |> Seq.groupBy(fun x -> x.nameSpace )
        |> dict
    
    let rec createTrees (namespaces:list<list<string>>) = [
        for top in namespaces |> Seq.groupBy(fun x -> Seq.head x) do
            let name = top |> fst
            let children = 
                snd top 
                |> Seq.map(function | [] -> [] | h::t -> t)
                |> Seq.filter(fun x -> x.IsEmpty |> not)
                |> List.ofSeq
            yield if children.Length = 0 then Tree(name,[]) else Tree(name, createTrees children)
    ]

    let nsTrees = 
        namespaceTypesMap
        |> Seq.map(fun x -> x.Key.Split( [|'.'|], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray)
        |> Seq.sortBy(fun x ->x.Length)
        |> List.ofSeq
        |> createTrees

    let rec renderTree (path:string list) (tree:Tree) =
        let (Tree(name, children)) = tree

        let fullPath = [ yield! path; yield name ]

        let subFolder = Path.Combine(fullPath |> Array.ofList)
        let folder = Path.Combine(outputFolder, subFolder)

        if Directory.Exists folder then 
            Directory.Delete(folder, true)

        Directory.CreateDirectory(folder) |> ignore

        let fullName = String.Join(".", fullPath)
        let types = namespaceTypesMap.[fullName]

        // render individual type files
        types
        |> Seq.iter(fun x ->
            use writer = new StreamWriter(Path.Combine(folder, x.name + ".md"))
            renderType writer x )   

        // render summary file
        use writer =  new StreamWriter(Path.Combine(folder, "ReadMe.md")) 
        renderHeadline writer 1 fullName
        writer.WriteLine()

        renderHeadline writer 2 "Types"
        writer.WriteLine()

        types
        |> Seq.iter(fun x -> writer.WriteLine( sprintf "* [%s](%s)" x.name (x.name + ".md") ))

        if children.IsEmpty |> not then
            renderHeadline writer 2 "Namespaces"
            writer.WriteLine()

            children
            |> Seq.iter(fun child -> 
                let (Tree(name, children)) = child
                writer.WriteLine( sprintf "* [%s.%s](%s/ReadMe.md)" fullName name name )
                renderTree fullPath child)

    nsTrees
    |> Seq.iter (renderTree [])

    
let generateAssemblyFileDoc outputFolder assemblyFile  = 
    generateAssemblyDoc outputFolder (assemblyLoader.Load assemblyFile)

