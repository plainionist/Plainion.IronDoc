namespace Plainion.IronDoc.Tests

open System
open System.IO
open System.Collections.Generic
open NUnit.Framework
open Plainion.IronDoc.FSharp
open Plainion.IronDoc.Tests.Fakes
open Plainion.IronDoc.Tests.Fakes

/// <summary>
/// This is use case number one
/// <para>a dedicated paragraph</para>
/// </summary>
/// <remarks>
/// And here are some remarks
/// </remarks>
type internal UseCase1() =

    member this.Run () =
        ()

// contains tests for specific parsing scenarios
[<TestFixture>]
module AcceptanceTests =
    
    let getApiDoc assemblyLocation =
        let docFile = Path.Combine( Path.GetDirectoryName( assemblyLocation ), Path.GetFileNameWithoutExtension( assemblyLocation ) + ".xml" )

        LoadApiDocFile docFile 

    let getApiDocCached = 
        memoize ( fun t -> getApiDoc t )
       
    let transform ( t : Type ) =
        use writer = new StringWriter()
        
        let apiDoc = getApiDocCached t.Assembly.Location
        Rendering.TransformType t apiDoc writer
        
        writer.ToString();

    [<Test>]
    let ``Standard API documentation tags are rendered nicely`` () = 
        let markdownDocument = transform typedefof<UseCase1>

        Assert.That( markdownDocument, Does.Contain "This is a summary" )

