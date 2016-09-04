namespace Plainion.IronDoc.FSharp

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc.FSharp

type internal TransformationContext = 
    { Loader : AssemblyLoader
      Writer : TextWriter
      Doc : XmlDocDocument.Contents }

module internal Transformer = 
    let tryProcessMemberDoc memb level = 
        0 |> ignore

    let processMembers headline allMembers getMemberName = 
        0 |> ignore
//    private void ProcessMembers<T>(string headline, IEnumerable<T> allMembers, Func<T, string> GetMemberName) where T : MemberInfo
//    {
//        var members = allMembers.ToList();
//
//        if (!members.Any())
//        {
//            return;
//        }
//
//        myWriter.WriteLine();
//        myWriter.Write("### ");
//        myWriter.WriteLine(headline);
//
//        foreach (var member in members)
//        {
//            myWriter.WriteLine();
//
//            myWriter.Write("#### ");
//            myWriter.WriteLine(GetMemberName(member));
//
//            myWriter.WriteLine();
//
//            TryProcessMemberDoc(myDocument.GetXmlDocumentation(member), 4);
//        }
//    }

    let processType (ctx : TransformationContext) (t : Type) = 
        ctx.Writer.WriteLine()
        ctx.Writer.Write "## "
        ctx.Writer.WriteLine t.FullName

        tryProcessMemberDoc (ctx.Doc.GetXmlDocumentation t) 2

        let bindingFlags = BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.DeclaredOnly

        processMembers 
            "Fields" 
            (t.GetFields(bindingFlags) |> Seq.filter (fun m -> not m.IsPrivate)) 
            (fun (field : FieldInfo) -> field.FieldType.FullName + " " + field.Name)

        0 |> ignore

//        ProcessMembers("Constructors",
//            type.GetConstructors(bindingFlags).Where(m => !m.IsPrivate),
//            ctor => type.Name + "(" + string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType)) + ")");
//
//        ProcessMembers("Properties",
//            type.GetProperties(bindingFlags).Where(m => !m.GetMethod.IsPrivate),
//            property => property.PropertyType.FullName + " " + property.Name);
//
//        ProcessMembers("Events",
//            type.GetEvents(bindingFlags).Where(m => !m.AddMethod.IsPrivate),
//            evt => evt.EventHandlerType.FullName + " " + evt.Name);
//
//        ProcessMembers("Methods",
//            type.GetMethods(bindingFlags)
//                .Where(m => !m.IsSpecialName)
//                .Where(m => !m.IsPrivate),
//            method => (method.ReturnType == typeof(void) ? "void" : method.ReturnType.FullName)
//                + " " + method.Name
//                + "(" + string.Join(", ", method.GetParameters().Select(p => p.ParameterType)) + ")");
//
//    private void TryProcessMemberDoc(XElement member, int level)
//    {
//        if (member == null)
//        {
//            return;
//        }
//
//        foreach (var summary in member.Elements("summary"))
//        {
//            myWriter.WriteLine(Utils.NormalizeSpace(summary.Value));
//
//            foreach (var p in member.Elements("para"))
//            {
//                myWriter.WriteLine(Utils.NormalizeSpace(p.Value));
//            }
//        }
//
//        var headlineMarker = "#".PadLeft(level + 1, '#');
//
//        if (member.Elements("remarks").Any())
//        {
//            myWriter.WriteLine();
//            myWriter.WriteLine("> " + headlineMarker + " Remarks");
//
//            foreach (var remarks in member.Elements("remarks"))
//            {
//                myWriter.WriteLine();
//                myWriter.Write("> ");
//                myWriter.WriteLine(Utils.NormalizeSpace(remarks.Value));
//            }
//        }
//
//        if (member.Elements("param").Any())
//        {
//            myWriter.WriteLine();
//            myWriter.WriteLine("> " + headlineMarker + " Parameters");
//
//            foreach (var param in member.Elements("param"))
//            {
//                myWriter.WriteLine();
//
//                myWriter.Write("> **");
//
//                myWriter.Write(param.Attribute("name") == null ? string.Empty : param.Attribute("name").Value);
//
//                myWriter.Write(":**  ");
//                myWriter.Write(Utils.NormalizeSpace(param.Value));
//                myWriter.WriteLine();
//            }
//        }
//
//        if (member.Elements("returns").Any())
//        {
//            myWriter.WriteLine();
//            myWriter.WriteLine("> " + headlineMarker + " Return value");
//
//            foreach (var r in member.Elements("returns"))
//            {
//                myWriter.WriteLine();
//                myWriter.Write("> ");
//                myWriter.WriteLine(Utils.NormalizeSpace(r.Value));
//            }
//        }
//
//        if (member.Elements("exception").Any())
//        {
//            myWriter.WriteLine();
//            myWriter.WriteLine("> " + headlineMarker + " Exceptions");
//
//            foreach (var ex in member.Elements("exception"))
//            {
//                myWriter.WriteLine();
//
//                myWriter.Write("> **");
//
//                var cref = Utils.SubstringAfter(ex.Attribute("cref").Value, ':');
//                myWriter.Write(cref);
//
//                myWriter.Write(":**  ");
//                myWriter.Write(ex.Value);
//                myWriter.WriteLine();
//            }
//        }
//
//        if (member.Elements("example").Any())
//        {
//            myWriter.WriteLine();
//            myWriter.WriteLine("> " + headlineMarker + " Example");
//            myWriter.WriteLine();
//            myWriter.WriteLine(">");
//
//            foreach (var param in member.Elements("example"))
//            {
//                TryProcessC(param);
//            }
//        }
//
//        TryProcessC(member.Element("c"));
//        TryProcessCode(member.Element("code"));
//        TryProcessInclude(member.Element("include"));
//        TryProcessParameterRef(member.Element("paramref"));
//        TryProcessPermission(member.Element("permission"));
//        TryProcessSee(member.Element("see"));
//        TryProcessSeeAlso(member.Element("seealso"));
//    }
//
//    private void TryProcessC(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine();
//        myWriter.WriteLine("```");
//        myWriter.WriteLine();
//        myWriter.WriteLine(Utils.NormalizeSpace(e.Value));
//        myWriter.WriteLine("```");
//    }
//
//    private void TryProcessCode(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine();
//        myWriter.Write("`");
//        myWriter.Write(e.Value);
//        myWriter.WriteLine("`");
//    }
//
//    private void TryProcessInclude(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.Write("[External file]({");
//        myWriter.Write(e.Attribute("file").Value);
//        myWriter.WriteLine("})");
//    }
//
//    private void TryProcessParameterRef(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine("*");
//        myWriter.Write(e.Attribute("name").Value);
//        myWriter.WriteLine("*");
//    }
//
//    private void TryProcessPermission(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine();
//
//        myWriter.Write("**Permission:** *");
//        myWriter.Write(e.Attribute("cref").Value);
//        myWriter.WriteLine("*");
//
//        myWriter.WriteLine(Utils.NormalizeSpace(e.Value));
//    }
//
//    private void TryProcessSee(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine();
//        myWriter.Write("> *See: ");
//        myWriter.Write(e.Attribute("cref").Value);
//        myWriter.WriteLine("*");
//    }
//
//    private void TryProcessSeeAlso(XElement e)
//    {
//        if (e == null) return;
//
//        myWriter.WriteLine();
//        myWriter.WriteLine("> *See also: ");
//        myWriter.WriteLine(e.Attribute("cref").Value);
//    }
//
type XmlDocTransformer(loader : AssemblyLoader) = 
    let myLoader = loader
    
    member this.TransformType t xmlDoc writer = 
        let ctx = 
            { Loader = myLoader
              Writer = writer
              Doc = xmlDoc }
        Transformer.processType ctx t
    
    member this.TransformAssembly (assembly : Assembly) xmlDoc writer = 
        let ctx = 
            { Loader = myLoader
              Writer = writer
              Doc = xmlDoc }

        writer.Write "# "
        writer.WriteLine xmlDoc.AssemblyName

        assembly.GetTypes()
        |> Seq.filter (fun t -> t.IsPublic)
        |> Seq.iter (fun t -> Transformer.processType ctx t)
    
    member this.TransformFile assemblyFile outputFolder = 
        let assembly = myLoader.Load assemblyFile
        let xmlDoc = Path.ChangeExtension(assemblyFile, ".xml")
        
        if not (Directory.Exists outputFolder) then Directory.CreateDirectory(outputFolder) |> ignore

        use writer = new StreamWriter(Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(assemblyFile) + ".md"))
        let doc = XmlDocDocument.LoadFile xmlDoc
        this.TransformAssembly assembly doc writer
