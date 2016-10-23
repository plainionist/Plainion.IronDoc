[<AutoOpen>]
module Plainion.IronDoc.FSharp.DomainModel

open System.Xml.Linq

type CRef = CRef of string
type File = File of string
type Path = Path of string

type CRefDescription = { cref : CRef 
                         description : string } 


// https://msdn.microsoft.com/en-us/library/5ast78ax.aspx

type Inline =
    | Text of string
    | C of string
    | Code of string
    | Para of string
    | ParamRef of CRef
    | TypeParamRef of CRef
    | See of CRef
    | SeeAlso of CRef

type MemberDoc = { Xml : XElement
                   Summary : Inline list }
//    | Remarks of Inline list
//    | Returns of Inline list
//    | Example of Inline list
//    | Exception of CRefDescription
//    | Param of CRefDescription
//    | TypeParam of CRefDescription
//    | Permission of CRefDescription
//    | Value of Inline list
//    | Include of (File * Path)
