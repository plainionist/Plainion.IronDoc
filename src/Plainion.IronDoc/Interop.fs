[<AutoOpen>]
module internal Plainion.IronDoc.Interop

open System
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
