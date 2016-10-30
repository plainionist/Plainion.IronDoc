// reads relevant information from .Net Xml Documentaion
[<AutoOpen>]
module Plainion.IronDoc.Parsing.XmlDoc

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc
open System.Xml

type XmlDocDocument = { AssemblyName : string
                        Members : XElement list } 

let parseNode (node:XNode) =
    match node with
    | :? XText as txt -> Some ( Text (txt.Value.Trim() ) )
    | :? XElement as e -> Some(match e.Name.LocalName with
                               | InvariantEqual "c" -> C e.Value
                               | InvariantEqual "code" -> Code e.Value
                               | InvariantEqual "para" -> Para e.Value
                               | InvariantEqual "ParamRef" -> ParamRef (CRef (e.Attribute(!!"cref").Value))
                               | InvariantEqual "TypeParamRef" -> TypeParamRef (CRef (e.Attribute(!!"cref").Value))
                               | InvariantEqual "See" -> See (CRef (e.Attribute(!!"cref").Value))
                               | InvariantEqual "SeeAlso" -> SeeAlso (CRef (e.Attribute(!!"cref").Value))
                               | x -> failwithf "Failed to parse: %s" x
                               )
    | _ -> None

let parseElement (element:XElement) =
    element.Nodes()
    |> Seq.choose parseNode
    |> List.ofSeq

let parse (elements:XElement seq) =
    elements
    |> Seq.collect parseElement
    |> List.ofSeq

// ignored:  <list/> , <include/>, <value/>
let GetXmlDocumentation xmlDoc memberInfo = 
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

let LoadApiDoc (root : XElement) = 
    { XmlDocDocument.AssemblyName = root.Element(!!"assembly").Element(!!"name").Value
      XmlDocDocument.Members = root.Element(!!"members").Elements(!!"member") |> List.ofSeq }
    
let LoadApiDocFile(file : string) = 
    LoadApiDoc(XElement.Load file)

