// reads relevant information from .Net Xml Documentaion
namespace Plainion.IronDoc.Parsing

open System
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc

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

    type XmlDocDocument = XmlDocDocument of XElement list

    let createXmlDoc (root:XElement) =
        XmlDocDocument( root.Element(!!"members").Elements(!!"member") |> List.ofSeq )

    // ignored:  <list/> , <include/>, <value/>
    let getApiDoc xmlDoc dtype mt = 
        let memberId = getMemberId dtype mt
        let (XmlDocDocument docMembers ) = xmlDoc 
        let doc = docMembers |> Seq.tryFind (fun m -> m.Attribute(!!"name").Value = memberId)
    
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

    type GetApiDocMsg = 
        | Get of DType * MemberType * replyChannel : AsyncReplyChannel<ApiDoc>
        | Stop 

[<AutoOpen>]
module XmlDocApi = 
    open System.IO

    type ApiDocLoaderApi = {
        Get: DType -> MemberType -> ApiDoc
        Stop: unit -> unit
    }

    let apiDocLoader =
        let agent = ResilientMailbox<GetApiDocMsg>.Start(fun inbox ->
            let getXmlDoc xmlDocs dtype = 
                match xmlDocs |> Map.tryFind dtype.assembly.location with
                | Some d -> xmlDocs, d
                | None -> let docFile = Path.ChangeExtension(dtype.assembly.location, ".xml")
                          match File.Exists(docFile) with
                          | false -> xmlDocs,XmlDocDocument([])
                          | true -> let d = XElement.Load docFile |> createXmlDoc
                                    xmlDocs.Add(dtype.assembly.location, d),d

            let rec loop xmlDocs =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Get (dtype, mt, replyChannel) -> 
                        let newXmlDoc,xmlDoc = getXmlDoc xmlDocs dtype
                        let apiDoc = getApiDoc xmlDoc dtype mt
                    
                        replyChannel.Reply apiDoc

                        return! loop newXmlDoc
                    | Stop -> return ()
                }
            loop Map.empty ) 
        agent.Error.Add(handleLastChanceException)
        { Get = fun dtype mt -> agent.PostAndReply( fun replyChannel -> Get( dtype, mt, replyChannel ) )
          Stop = fun () -> agent.Post Stop }
