using System;
using System.IO;
using System.Linq;
using NuDoq;

namespace Plainion.IronDoc
{
    class MarkdownVisitor : Visitor
    {
        private StreamWriter myWriter;

        public MarkdownVisitor( StreamWriter writer )
        {
            myWriter = writer;
        }

        public override void VisitAssembly( AssemblyMembers assembly )
        {
            myWriter.Write( "# " );
            myWriter.WriteLine( assembly.Assembly.GetName().Name );

            foreach( var group in assembly.Elements.OfType<TypeDeclaration>().GroupBy(td=>((Type)td.Info).Namespace ))
            {
                myWriter.WriteLine();
                myWriter.Write( "## " );
                myWriter.WriteLine( group.Key );

                foreach( var td in group )
                {
                    VisitType( td );
                }
            }
        }

        public override void VisitType( TypeDeclaration td )
        {
            var type =( Type )td.Info ;
            if( type.IsPublic )
            {
                myWriter.WriteLine();
                myWriter.Write( "## " );
                myWriter.WriteLine( type.Name );                
            }

            base.VisitType( td );
        }
    }
}
