module internal Plainion.IronDoc.FSharp.Interop

open System

/// Call "Implicit" operator
/// see: http://codebetter.com/matthewpodwysocki/2009/06/11/f-duck-typing-and-structural-typing/
let inline implicit arg =
    ( ^a : (static member op_Implicit : ^b -> ^a) arg)

/// simplify creation of guards
type internal Guard( on, off ) =
    do on()

    interface IDisposable with
        member x.Dispose() = 
            off()
