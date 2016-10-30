// reads relevant information from .Net Xml Documentaion
[<AutoOpen>]
module Plainion.IronDoc.Parsing.XmlDoc

open System
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc

let getMemberName (memberInfo : MemberInfo) = 
    match memberInfo.MemberType with
    | MemberTypes.Constructor -> "#ctor" // XML documentation uses slightly different constructor names
    | MemberTypes.NestedType -> memberInfo.DeclaringType.Name + "." + memberInfo.Name
    | _ -> memberInfo.Name
    
let getFullMemberName (memberInfo : MemberInfo) = 
    match memberInfo with
    | :? Type as t -> t.Namespace + "." + (getMemberName memberInfo) // member is a Type
    | _ -> memberInfo.DeclaringType.FullName + "." + (getMemberName memberInfo)
    
/// elements are of the form "M:Namespace.Class.Method"
let getMemberId prefixCode memberName = 
    sprintf "%s:%s" prefixCode memberName
    
/// parameters are listed according to their type, not their name
let getMethodParameterSignature (memberInfo : MemberInfo) = 
    let parameters = (memberInfo :?> MethodBase).GetParameters()
    match parameters with
    | [||] -> ""
    | _ -> 
        "(" + (parameters
                |> Seq.map (fun p -> p.ParameterType.FullName)
                |> String.concat ",")
        + ")"
    
let getMemberElementName (mi : MemberInfo) = 
    match mi.MemberType with
    | MemberTypes.Constructor -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
    | MemberTypes.Method -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
    | MemberTypes.Event -> getMemberId "E" (getFullMemberName mi)
    | MemberTypes.Field -> getMemberId "F" (getFullMemberName mi)
    | MemberTypes.NestedType -> getMemberId "T" (getFullMemberName mi)
    | MemberTypes.TypeInfo -> getMemberId "T" (getFullMemberName mi)
    | MemberTypes.Property -> getMemberId "P" (getFullMemberName mi)
    | _ -> failwith "Unknown MemberType: " + mi.MemberType.ToString()

type XmlDocDocument = { AssemblyName : string
                        Members : XElement list } 

let parseXElement (e:XElement) =
    match e.Name.LocalName with
    | InvariantEqual "c" -> C e.Value
    | InvariantEqual "code" -> Code e.Value
    | InvariantEqual "para" -> Para e.Value
    | InvariantEqual "ParamRef" -> ParamRef (CRef (e.Attribute(!!"cref").Value))
    | InvariantEqual "TypeParamRef" -> TypeParamRef (CRef (e.Attribute(!!"cref").Value))
    | InvariantEqual "See" -> See (CRef (e.Attribute(!!"cref").Value))
    | InvariantEqual "SeeAlso" -> SeeAlso (CRef (e.Attribute(!!"cref").Value))
    | x -> failwithf "Failed to parse: %s" x

let parseXNode (node:XNode) =
    match node with
    | :? XText as txt -> Some ( Text (txt.Value.Trim() ) )
    | :? XElement as e -> Some( parseXElement e )
    | _ -> None

let parse (elements:XElement seq) =
    let parseMember (element:XElement) =
        element.Nodes()
        |> Seq.choose parseXNode
        |> List.ofSeq

    elements
    |> Seq.collect parseMember
    |> List.ofSeq

// ignored:  <list/> , <include/>, <value/>
let getXmlDocumentation xmlDoc memberInfo = 
    let memberName = getMemberElementName memberInfo
    let doc = xmlDoc.Members |> Seq.tryFind (fun m -> m.Attribute(!!"name").Value = memberName)
    
    match doc with
    | Some d -> { Summary = (parse (d.Elements(!!"summary")))
                  Remarks = (parse (d.Elements(!!"remarks")))
                  Params = d.Elements(!!"param") 
                           |> Seq.map(fun x -> { cref = CRef( x.Attribute(!!"name").Value )
                                                 description = normalizeSpace x.Value
                                               })
                           |> List.ofSeq
                  Returns = (parse (d.Elements(!!"returns")))
                  Exceptions = d.Elements(!!"exception") 
                               |> Seq.map(fun x -> { cref = CRef( x.Attribute(!!"cref").Value )
                                                     description = normalizeSpace x.Value
                                                })
                               |> List.ofSeq
                  Example = (parse (d.Elements(!!"example")))
                  Permissions = d.Elements(!!"permission") 
                                |> Seq.map(fun x -> { cref = CRef( x.Attribute(!!"cref").Value )
                                                      description = normalizeSpace x.Value
                                                    })
                                |> List.ofSeq
                  TypeParams = d.Elements(!!"typeparam") 
                               |> Seq.map(fun x -> { cref = CRef( x.Attribute(!!"name").Value )
                                                     description = normalizeSpace x.Value
                                                   })
                               |> List.ofSeq
                }
    | None -> NoDoc

let loadApiDoc (root : XElement) = 
    { XmlDocDocument.AssemblyName = root.Element(!!"assembly").Element(!!"name").Value
      XmlDocDocument.Members = root.Element(!!"members").Elements(!!"member") |> List.ofSeq }
    
let loadApiDocFile(file : string) = 
    loadApiDoc(XElement.Load file)

