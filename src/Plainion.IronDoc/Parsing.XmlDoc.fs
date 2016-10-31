// reads relevant information from .Net Xml Documentaion
[<AutoOpen>]
module Plainion.IronDoc.Parsing.XmlDoc

open System
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc

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

let getMemberId (m:obj) =
    match m with
    | :? DType as t -> getFullName t |> sprintf "T:%s" 
    | _ -> failwithf "Unknown member type: %s" (m.GetType().ToString())

// ignored:  <list/> , <include/>, <value/>
let getXmlDocumentation xmlDoc memberId = 
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

let loadApiDoc (root : XElement) = 
    { XmlDocDocument.AssemblyName = root.Element(!!"assembly").Element(!!"name").Value
      XmlDocDocument.Members = root.Element(!!"members").Elements(!!"member") |> List.ofSeq }
    
let loadApiDocFile(file : string) = 
    loadApiDoc(XElement.Load file)

