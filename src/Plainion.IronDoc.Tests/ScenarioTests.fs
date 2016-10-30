namespace Plainion.IronDoc.Tests

open System
open System.IO
open System.Collections.Generic
open NUnit.Framework
open Plainion.IronDoc
open Plainion.IronDoc.Tests.Fakes
open Plainion.IronDoc.Tests.Fakes

type internal OverwrittenMethods() =
    /// <summary>
    /// Returns nicely formatted message about the state of this object
    /// </summary>
    override this.ToString() =
        "silence"

/// <summary>
/// This is a summary
/// </summary>
/// <remarks>
/// And here are some remarks
/// </remarks>
type internal SimplePublicClass() = class end

// test specific scenarios - parsing + rendering
[<TestFixture>]
module ParsingTests =
    
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
    let ``Simple class summary rendered`` () = 
        let markdownDocument = transform typedefof<SimplePublicClass>

        // TODO: to unspecific
        Assert.That( markdownDocument, Does.Contain "This is a summary" )

    [<Test>]
    let ``Overwritten method is rendered``() =
        let markdownDocument = transform typedefof<OverwrittenMethods>

        // TODO: to unspecific
        Assert.That( markdownDocument, Does.Contain @"Returns nicely formatted message about the state of this object" )

    [<Test>]
    let ``Nested type summary is rendered``() =
        let markdownDocument = transform typedefof<Scenarios.NestedType.Nested>

        // TODO: to unspecific
        Assert.That( markdownDocument, Does.Contain @"I am nested" )
