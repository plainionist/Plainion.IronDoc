// command line interface to Api doc processor
module Plainion.IronDoc.Program

open System.IO
open Plainion.IronDoc
open Plainion
open System

let usage() =
    printfn "Plainion.IronDoc [Options]"
    printfn ""
    printfn "Options:"
    printfn  "  -h                 - Prints this help"
    printfn  "  -assembly <file>   - .Net assembly to generate documention for"
    printfn  "  -sources <dir>     - full path to sources of this assembly (optional)"
    printfn  "  -output <dir>      - full path to output folder"

type CommandLineOptions = {
    printHelp : bool
    assembly : string
    sources : string option
    output : string }

let rec parseCommandLineRec args optionsSoFar = 
    match args with 
    | [] -> optionsSoFar  
    | "-h"::xs -> 
        let newOptionsSoFar = { optionsSoFar with printHelp = true}
        parseCommandLineRec xs newOptionsSoFar 
    | "-assembly"::xs -> 
        match xs with
        | h::xss -> 
            let newOptionsSoFar = { optionsSoFar with assembly=h}
            parseCommandLineRec xss newOptionsSoFar 
        | _ -> 
            printfn "Assembly needs a second argument"
            parseCommandLineRec xs optionsSoFar 
    | "-output"::xs -> 
        match xs with
        | h::xss -> 
            let newOptionsSoFar = { optionsSoFar with output=h}
            parseCommandLineRec xss newOptionsSoFar 
        | _ -> 
            printfn "Output needs a second argument"
            parseCommandLineRec xs optionsSoFar 
    | "-sources"::xs -> 
        match xs with
        | h::xss -> 
            let newOptionsSoFar = { optionsSoFar with sources=Some h}
            parseCommandLineRec xss newOptionsSoFar 
        | _ -> 
            printfn "Sources needs a second argument"
            parseCommandLineRec xs optionsSoFar 
    | x::xs -> 
        printfn "Option '%s' is unrecognized" x
        parseCommandLineRec xs optionsSoFar 

let parseCommandLine args = 
    let defaultOptions = {
        printHelp = false
        assembly = null
        sources = None
        output = null
        }

    let options = parseCommandLineRec ( List.ofArray args ) defaultOptions 

    if options.printHelp then
        None
    else if options.assembly = null then
        failwith "No assembly specified"
    else if options.output = null then
        Some { options with output=Path.ChangeExtension( options.assembly, ".md" )}
    else
        Some options
       

[<EntryPoint>]
let main argv = 
    let options = parseCommandLine argv
    match options with
    | None -> 
        usage ()
        0
    | Some x ->
        printfn "Generating documentation to: %s" x.output

        try
            Workflows.generateAssemblyFileDoc x.output x.assembly x.sources
            0
        with
        | ex -> Console.Error.WriteLine( ExceptionExtensions.Dump(ex) )
                1
