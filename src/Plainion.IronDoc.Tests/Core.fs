[<AutoOpen>]
module Plainion.IronDoc.Tests.Core

open System
open System.IO
open NUnit.Framework
open Plainion.IronDoc
open Plainion.IronDoc.Parsing

let renderApiDoc ( t : Type ) =
    use writer = new StringWriter()
        
    let dAssembly = { DAssembly.name = t.Assembly.GetName().Name
                      location = t.Assembly.Location
                      assembly = t.Assembly }
    let dType = createDType dAssembly t
    Workflows.generateTypeDoc writer dType
        
    writer.ToString()

module XAssert = 
    let lineBasedEquals (actual:string) (expected:string) =
        let rec diff (actual:string list) (expected:string list) =
            match actual,expected with
            | ha::ta, he::te -> Assert.AreEqual(he, ha)
                                diff ta te
            | [], [] -> ()
            | ha::ta,[] -> Assert.Fail( sprintf "Actual is longer than expected: %A" actual ) 
            | [],he::te -> Assert.Fail( sprintf "Expected is longer than actual: %A" expected ) 
        
        let splitLines (s:string) =
            s.Split([|Environment.NewLine|], StringSplitOptions.None) |> List.ofArray

        diff (splitLines actual) (splitLines expected)
