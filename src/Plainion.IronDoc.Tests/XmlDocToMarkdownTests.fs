namespace Plainion.IronDoc.Tests.Fakes

open System
open System.IO
open NUnit.Framework
open Plainion.IronDoc.FSharp
open Plainion.IronDoc.Tests.Fakes

[<TestFixture>]
type XmlDocToMarkdownTests() =
    [<DefaultValue>] val mutable myTransformer : XmlDocTransformer
    [<DefaultValue>] val mutable myXmlDocumentation : XmlDocDocument.Contents

    [<OneTimeSetUpAttribute>]
    member this.FixtureSetUp() =
        let loader = new AssemblyLoader()

        this.myTransformer <- new XmlDocTransformer( loader )

        let assembly = this.GetType().Assembly
        let docFile = Path.Combine( Path.GetDirectoryName( assembly.Location ), Path.GetFileNameWithoutExtension( assembly.Location ) + ".xml" )

        this.myXmlDocumentation <- XmlDocDocument.LoadFile docFile 
        ()

    member private this.Transform t =
        use writer = new StringWriter()
        
        this.myTransformer.TransformType t this.myXmlDocumentation writer
        
        writer.ToString();

    [<Test>]
    member this.SimpleSummary () = 
        let markdownDocument = this.Transform typedefof<SimplePublicClass>

        Assert.That( markdownDocument, Does.Contain "This is a summary" )

    [<Test>]
    member this.OverwrittenMethods() =
        let markdownDocument = this.Transform typedefof<OverwrittenMethods>

        Assert.That( markdownDocument, Does.Contain @"Returns nicely formatted message about the state of this object" )

    [<Test>]
    member this.NestedTypes() =
        let markdownDocument = this.Transform typedefof<NestedType.Nested>

        Assert.That( markdownDocument, Does.Contain @"I am nested" )
