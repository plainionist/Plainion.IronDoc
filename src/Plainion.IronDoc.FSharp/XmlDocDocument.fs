namespace Plainion.IronDoc.FSharp

open System.Reflection
open System.Linq
open System.Xml.Linq

module XmlDocDocument = 

    let private (!!) : string -> XName = Interop.implicit

    let private GetMemberElementName ( memberInfo : MemberInfo ) =
        ""

//            char prefixCode;
//            string memberName = (member is Type)
//                                    ? ((Type)member).FullName // member is a Type
//                                    : (member.DeclaringType.FullName + "." + member.Name); // member belongs to a Type
//
//            switch (member.MemberType)
//            {
//                case MemberTypes.Constructor:
//                    // XML documentation uses slightly different constructor names
//                    memberName = memberName.Replace(".ctor", "#ctor");
//                    goto case MemberTypes.Method;
//                case MemberTypes.Method:
//                    prefixCode = 'M';
//
//                    // parameters are listed according to their type, not their name
//                    string paramTypesList = String.Join(
//                        ",",
//                        ((MethodBase)member).GetParameters()
//                            .Cast<ParameterInfo>()
//                            .Select(x => x.ParameterType.FullName
//                            ).ToArray()
//                        );
//                    if (!String.IsNullOrEmpty(paramTypesList)) memberName += "(" + paramTypesList + ")";
//                    break;
//
//                case MemberTypes.Event:
//                    prefixCode = 'E';
//                    break;
//
//                case MemberTypes.Field:
//                    prefixCode = 'F';
//                    break;
//
//                case MemberTypes.NestedType:
//                    // XML documentation uses slightly different nested type names
//                    memberName = memberName.Replace('+', '.');
//                    goto case MemberTypes.TypeInfo;
//                case MemberTypes.TypeInfo:
//                    prefixCode = 'T';
//                    break;
//
//                case MemberTypes.Property:
//                    prefixCode = 'P';
//                    break;
//
//                default:
//                    throw new ArgumentException("Unknown member type", "member");
//            }
//
//            // elements are of the form "M:Namespace.Class.Method"
//            return String.Format("{0}:{1}", prefixCode, memberName);

    type Contents( assemblyName, members : XElement list ) = 
        member this.AssemblyName = assemblyName
        member this.Members = members

        member this.GetXmlDocumentation memberInfo =
            let memberName = GetMemberElementName memberInfo
            let doc = this.Members |> Seq.tryFind( fun m -> m.Attribute( !!"name" ).Value = memberName )
            match doc with
            | Some x -> x
            | None -> null

    let Load ( root : XElement ) = 
        new Contents( 
            root.Element( !!"assembly").Element( !!"name").Value, 
            root.Element( !!"members").Elements( !!"member") |> List.ofSeq )

    let LoadFile ( file : string ) =
        Load( XElement.Load file )
