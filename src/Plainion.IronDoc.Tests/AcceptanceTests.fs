namespace Plainion.IronDoc.Tests

open System
open System.IO
open NUnit.Framework
open Plainion.IronDoc
open Plainion.IronDoc.Tests.Fakes.Acceptance

// contains tests for specific parsing scenarios
[<TestFixture>]
module AcceptanceTests =
    open Plainion.IronDoc.Parsing
    
    let myAssemblyLocation = typedefof<UseCase1>.Assembly.Location

    let expected (t:Type) =
        let root = Path.GetDirectoryName( myAssemblyLocation )
        let assemblyName = Path.GetFileNameWithoutExtension( myAssemblyLocation )
        let subFolders = t.Namespace.Substring(assemblyName.Length).Split('.') 
                         |> Path.Combine
        
        File.ReadAllText( Path.Combine(root,subFolders, t.Name + ".md"))
       
    let transform ( t : Type ) =
        use writer = new StringWriter()
        
        let dType = createDType t
        Workflows.generateTypeDoc dType writer
        
        writer.ToString()

    let verifyLineBased (actual:string) (expected:string) =
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

    [<Test>]
    let ``Standard API documentation tags are rendered nicely`` () = 
        let typeToTest = typedefof<UseCase1>

        let markdownDocument = transform typeToTest

        verifyLineBased markdownDocument (expected typeToTest)

