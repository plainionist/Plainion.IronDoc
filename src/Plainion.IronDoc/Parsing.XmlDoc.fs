// reads relevant information from .Net Xml Documentaion
namespace Plainion.IronDoc.Parsing

open System
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc

type XmlDocDocument = { AssemblyName : string
                        Members : XElement list } 

[<AutoOpen>]
module private Impl =
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

    let getMemberId dtype mt =
        let getParametersSignature parameters = 
            match parameters with
            | [] -> ""
            | _ -> 
                "(" + (parameters
                        |> Seq.map (fun p -> p.parameterType.FullName)
                        |> String.concat ",")
                + ")"

        match mt with
        | Type x -> getFullName x |> sprintf "T:%s" 
        | Field x -> getFullName dtype + "." + x.name |> sprintf "F:%s" 
        | Constructor x -> getFullName dtype + "." + "#ctor" + getParametersSignature x.parameters |> sprintf "M:%s"
        | Property x -> getFullName dtype + "." + x.name |> sprintf "P:%s"
        | Event x -> getFullName dtype + "." + x.name |> sprintf "E:%s" |> sprintf "M:%s"
        | Method x ->getFullName dtype + "." + x.name + getParametersSignature x.parameters |> sprintf "M:%s"
        | NestedType x ->getFullName dtype + "." + x.name |> sprintf "T:%s"

[<AutoOpen>]
module XmlDocApi = 
    // ignored:  <list/> , <include/>, <value/>
    let getXmlDocumentation xmlDoc dtype mt = 
        let memberId = getMemberId dtype mt
        let doc = xmlDoc.Members |> Seq.tryFind (fun m -> m.Attribute(!!"name").Value = memberId)
    
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

    let loadApiDocFile(file : string) = 
        let root = XElement.Load (file)
        
        { XmlDocDocument.AssemblyName = root.Element(!!"assembly").Element(!!"name").Value
          XmlDocDocument.Members = root.Element(!!"members").Elements(!!"member") |> List.ofSeq }

