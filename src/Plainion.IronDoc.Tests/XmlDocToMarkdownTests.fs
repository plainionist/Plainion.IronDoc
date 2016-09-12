﻿namespace Plainion.IronDoc.Tests

open System
open System.IO
open System.Collections.Generic
open NUnit.Framework
open Plainion.IronDoc.FSharp
open Plainion.IronDoc.Tests.Fakes
open Plainion.IronDoc.Tests.Fakes

[<TestFixture>]
module XmlDocToMarkdownTests =
    
    let getApiDoc assemblyLocation =
        let docFile = Path.Combine( Path.GetDirectoryName( assemblyLocation ), Path.GetFileNameWithoutExtension( assemblyLocation ) + ".xml" )

        XmlDocDocument.LoadFile docFile 

    let getApiDocCached = 
        memoize ( fun t -> getApiDoc t )

    let getTransformer () =
        let loader = new AssemblyLoader()
        new XmlDocTransformer( loader )
       
    let getTransformerCached = 
        let transformer = getTransformer
        fun () -> transformer

    let transform ( t : Type ) =
        use writer = new StringWriter()
        
        let apiDoc = getApiDocCached t.Assembly.Location
        let transformer = getTransformer ()
        transformer.TransformType t apiDoc writer
        
        writer.ToString();

    [<Test>]
    let SimpleSummary () = 
        let markdownDocument = transform typedefof<SimplePublicClass>

        Assert.That( markdownDocument, Does.Contain "This is a summary" )

    [<Test>]
    let OverwrittenMethods() =
        let markdownDocument = transform typedefof<OverwrittenMethods>

        Assert.That( markdownDocument, Does.Contain @"Returns nicely formatted message about the state of this object" )

    [<Test>]
    let NestedTypes() =
        let markdownDocument = transform typedefof<NestedType.Nested>

        Assert.That( markdownDocument, Does.Contain @"I am nested" )
