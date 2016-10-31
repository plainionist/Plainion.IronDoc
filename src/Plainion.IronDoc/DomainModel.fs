// DomainModel only
[<AutoOpen>]
module Plainion.IronDoc.DomainModel

open System.Xml.Linq
open System

type Field = { name : string
               fieldType : Type }

type Parameter = { name : string
                   parameterType : Type }

type Constructor = { parameters : Parameter list }

type Property = { name : string
                  propertyType : Type }

type Event = { name : string
               eventHandlerType : Type }

type Method = { name : string
                parameters : Parameter list
                returnType : Type }

type DType = { assembly : string
               nameSpace : string
               name : string 
               fields : Field list 
               constructors : Constructor list 
               properties : Property list 
               events : Event list 
               methods : Method list 
               nestedTypes : DType list 
             }

let getFullName t =
    sprintf "%s.%s" t.nameSpace t.name

type MemberType =
    | Type of DType
    | Field of Field
    | Constructor of Constructor
    | Property of Property
    | Event of Event
    | Method of Method
    | NestedType of DType

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

type ApiDoc = { Summary : Inline list
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
