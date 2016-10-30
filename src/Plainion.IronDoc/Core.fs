// extensions to .Net
[<AutoOpen>]
module internal Plainion.IronDoc.Core

open System
open System.Text.RegularExpressions
open System.Xml.Linq

/// Call "Implicit" operator
/// see: http://codebetter.com/matthewpodwysocki/2009/06/11/f-duck-typing-and-structural-typing/
let inline implicit arg =
    ( ^a : (static member op_Implicit : ^b -> ^a) arg)

let (!!) : string -> XName = implicit

/// simplify creation of guards
type internal Guard( on, off ) =
    do on()

    interface IDisposable with
        member x.Dispose() = 
            off()

let (|InvariantEqual|_|) (str:string) arg = 
  if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

let substringAfter ( value : string ) ( sep : char ) =
    let pos = value.IndexOf (sep)
    value.Substring(pos + 1)

let normalizeSpace (value : string) =
    Regex.Replace(value.Trim(), @"\s+", " ")

