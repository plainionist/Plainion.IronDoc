namespace Plainion.IronDoc.FSharp

open System
open System.Reflection
open System.Linq
open System.Xml.Linq

module XmlDocDocument = 
    let private (!!) : string -> XName = Interop.implicit
    
    let private getMemberName (memberInfo : MemberInfo) = 
        match memberInfo.MemberType with
        | MemberTypes.Constructor -> "#ctor" // XML documentation uses slightly different constructor names
        | MemberTypes.NestedType -> memberInfo.DeclaringType.Name + "." + memberInfo.Name
        | _ -> memberInfo.Name
    
    let private getFullMemberName (memberInfo : MemberInfo) = 
        match memberInfo with
        | :? Type as t -> t.Namespace + "." + (getMemberName memberInfo) // member is a Type
        | _ -> memberInfo.DeclaringType.FullName + "." + (getMemberName memberInfo)
    
    /// elements are of the form "M:Namespace.Class.Method"
    let private getMemberId prefixCode memberName = sprintf "%s:%s" prefixCode memberName
    
    /// parameters are listed according to their type, not their name
    let private getMethodParameterSignature (memberInfo : MemberInfo) = 
        let parameters = (memberInfo :?> MethodBase).GetParameters()
        match parameters with
        | [||] -> ""
        | _ -> 
            "(" + (parameters
                   |> Seq.map (fun p -> p.ParameterType.FullName)
                   |> String.concat ",")
            + ")"
    
    let private getMemberElementName (mi : MemberInfo) = 
        match mi.MemberType with
        | MemberTypes.Constructor -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
        | MemberTypes.Method -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
        | MemberTypes.Event -> getMemberId "E" (getFullMemberName mi)
        | MemberTypes.Field -> getMemberId "F" (getFullMemberName mi)
        | MemberTypes.NestedType -> getMemberId "T" (getFullMemberName mi)
        | MemberTypes.TypeInfo -> getMemberId "T" (getFullMemberName mi)
        | MemberTypes.Property -> getMemberId "P" (getFullMemberName mi)
        | _ -> failwith "Unknown MemberType: " + mi.MemberType.ToString()
    
    type Contents(assemblyName, members : XElement list) = 
        member this.AssemblyName = assemblyName
        member this.Members = members
        member this.GetXmlDocumentation memberInfo = 
            let memberName = getMemberElementName memberInfo
            let doc = this.Members |> Seq.tryFind (fun m -> m.Attribute(!!"name").Value = memberName)
            match doc with
            | Some x -> x
            | None -> null
    
    let Load(root : XElement) = 
        new Contents(root.Element(!!"assembly").Element(!!"name").Value, 
                     root.Element(!!"members").Elements(!!"member") |> List.ofSeq)
    let LoadFile(file : string) = Load(XElement.Load file)
