// command line interface to Api doc processor
module Plainion.IronDoc.Program

open System
open System.IO
open Plainion.IronDoc

let usage() =
    printf "Plainion.IronDoc [Options]"
    printf ""
    printf "Options:"
    printf  "  -h                 - Prints this help"
    printf  "  -assembly <file>   - .Net assembly to generate documention for"
    printf  "  -output <dir>      - full path to output folder"

type CommandLineOptions = {
    printHelp : bool
    assembly : string
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
            printfn "OrderBy needs a second argument"
            parseCommandLineRec xs optionsSoFar 
    | "-output"::xs -> 
        match xs with
        | h::xss -> 
            let newOptionsSoFar = { optionsSoFar with output=h}
            parseCommandLineRec xss newOptionsSoFar 
        | _ -> 
            printfn "OrderBy needs a second argument"
            parseCommandLineRec xs optionsSoFar 
    | x::xs -> 
        printfn "Option '%s' is unrecognized" x
        parseCommandLineRec xs optionsSoFar 

let parseCommandLine args = 
    let defaultOptions = {
        printHelp = false
        assembly = null
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
        printf "Generating documentation to: %s" x.output

        Workflows.generateAssemblyFileDoc x.assembly x.output

        0
