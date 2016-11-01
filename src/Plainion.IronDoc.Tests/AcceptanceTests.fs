namespace Plainion.IronDoc.Tests

open System
open System.IO
open NUnit.Framework
open Plainion.IronDoc
open Plainion.IronDoc.Tests.Fakes.Acceptance

// contains tests for specific parsing scenarios
[<TestFixture>]
module AcceptanceTests =
   
    let getExpectedApiDoc (t:Type) =
        let assemblyLocation = t.Assembly.Location
        let root = Path.GetDirectoryName( assemblyLocation )
        let assemblyName = Path.GetFileNameWithoutExtension( assemblyLocation )
        let subFolders = t.Namespace.Substring(assemblyName.Length).Split('.') 
                         |> Path.Combine
        
        File.ReadAllText( Path.Combine(root,subFolders, t.Name + ".md"))
       
    [<Test>]
    let ``Standard API documentation tags are rendered nicely`` () = 
        let typeToTest = typedefof<UseCase1>

        let markdownDocument = renderApiDoc typeToTest

        XAssert.lineBasedEquals markdownDocument (getExpectedApiDoc typeToTest)

