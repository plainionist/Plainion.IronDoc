using System;
using System.IO;
using NUnit.Framework;
using Plainion.IronDoc.FSharp;
using Plainion.IronDoc.Tests.Fakes;

namespace Plainion.IronDoc.Tests
{
    [TestFixture]
    class XmlDocToMarkdownTests
    {
        private XmlDocTransformer myTransformer;
        private XmlDocDocument.Contents myXmlDocumentation;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            var loader = new AssemblyLoader();

            myTransformer = new XmlDocTransformer( loader );

            var assembly = GetType().Assembly;
            var docFile = Path.Combine( Path.GetDirectoryName( assembly.Location ), Path.GetFileNameWithoutExtension( assembly.Location ) + ".xml" );

            myXmlDocumentation = XmlDocDocument.LoadFile( docFile );
        }

        [Test]
        public void SimpleSummary()
        {
            var markdownDocument = Transform( typeof( SimplePublicClass ) );

            Assert.That( markdownDocument, Does.Contain( @"
## Plainion.IronDoc.Tests.Fakes.SimplePublicClass
This is a summary
" ) );
        }

        [Test]
        public void OverwrittenMethods()
        {
            var markdownDocument = Transform( typeof( OverwrittenMethods ) );

            Assert.That( markdownDocument, Does.Contain( @"Returns nicely formatted message about the state of this object" ) );
        }

        [Test]
        public void NestedTypes()
        {
            var markdownDocument = Transform( typeof( NestedType.Nested ) );

            Assert.That( markdownDocument, Does.Contain( @"I am nested" ) );
        }

        private string Transform( Type type )
        {
            using( var writer = new StringWriter() )
            {
                myTransformer.Transform( type, myXmlDocumentation, writer );
                return writer.ToString();
            }
        }
    }
}
