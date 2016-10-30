// DomainModel only
[<AutoOpen>]
module Plainion.IronDoc.DomainModel

open System.Xml.Linq

// Documentation about valid .Net XML documentation tags
// https://msdn.microsoft.com/en-us/library/5ast78ax.aspx

type CRef = CRef of string
type File = File of string
type Path = Path of string

type CRefDescription = { cref : CRef 
                         description : string } 

type Inline =
    | Text of string
    | C of string
    | Code of string
    | Para of string
    | ParamRef of CRef
    | TypeParamRef of CRef
    | See of CRef
    | SeeAlso of CRef

type MemberDoc = { Summary : Inline list
                   Remarks : Inline list
                   Params : CRefDescription list
                   Returns : Inline list
                   Exceptions : CRefDescription list
                   Example : Inline list
                   Permissions : CRefDescription list
                   TypeParams : CRefDescription list
                 }

let NoDoc = { Summary = []
              Remarks = []
              Params = []
              Returns = []
              Exceptions = []
              Example = []
              Permissions = []
              TypeParams = []
             }

type Member = { Assembly : string
                Namespace : string
                Name : string
                Doc : MemberDoc }