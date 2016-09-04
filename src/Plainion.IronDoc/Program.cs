using System;
using System.IO;
using Plainion.IronDoc.FSharp;

namespace Plainion.IronDoc
{
    class Program
    {
        private static void Main( string[] args )
        {
            string assembly = null;
            string outputFile = null;

            for( int i = 0; i < args.Length; ++i )
            {
                if( args[ i ].Equals( "-h", StringComparison.OrdinalIgnoreCase ) )
                {
                    Usage();
                    Environment.Exit( 0 );
                }
                else if( args[ i ].Equals( "-assembly", StringComparison.OrdinalIgnoreCase ) )
                {
                    if( i + 1 == args.Length )
                    {
                        throw new ArgumentException( "-assembly requires an argument" );
                    }

                    i++;
                    assembly = args[ i ];
                }
                else if( args[ i ].Equals( "-output", StringComparison.OrdinalIgnoreCase ) )
                {
                    if( i + 1 == args.Length )
                    {
                        throw new ArgumentException( "-output requires an argument" );
                    }

                    i++;
                    outputFile = args[ i ];
                }
                else
                {
                    throw new ArgumentException( "Unknown argument: " + args[ i ] );
                }
            }

            if( assembly == null )
            {
                throw new ArgumentException( "No assembly specified" );
            }

            if( outputFile == null )
            {
                outputFile = Path.ChangeExtension( assembly, ".md" );
                Console.WriteLine( "Generating documentation to: {0}", outputFile );
            }

            var loader = new AssemblyLoader();

            var transformer = new XmlDocTransformer( loader );
            transformer.TransformFile( assembly, outputFile );
        }

        private static void Usage()
        {
            Console.WriteLine( "Plainion.IronDoc [Options]" );
            Console.WriteLine();
            Console.WriteLine( "Options:" );
            Console.WriteLine( "  -h                 - Prints this help" );
            Console.WriteLine( "  -assembly <file>   - .Net assembly to generate documention for" );
            Console.WriteLine( "  -output <dir>      - full path to output folder" );
        }
    }
}
