// extensions to .Net
[<AutoOpen>]
module internal Plainion.IronDoc.Core

open System
open System.Text.RegularExpressions
open System.Xml.Linq
open System.Reflection

/// Call "Implicit" operator
/// see: http://codebetter.com/matthewpodwysocki/2009/06/11/f-duck-typing-and-structural-typing/
let inline implicit arg =
    ( ^a : (static member op_Implicit : ^b -> ^a) arg)

let (!!) : string -> XName = implicit

let (|InvariantEqual|_|) (str:string) arg = 
  if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some() else None

let substringAfter ( value : string ) ( sep : char ) =
    let pos = value.IndexOf (sep)
    value.Substring(pos + 1)

let tryRemoveAfter (sep : char) (value : string) =
    let pos = value.IndexOf (sep)
    if pos > -1 then
        value.Substring(0,pos)
    else
        value

let normalizeSpace (value : string) =
    Regex.Replace(value.Trim(), @"\s+", " ")

/// <summary>
/// A wrapper for MailboxProcessor that catches all unhandled
/// exceptions and reports them via the 'OnError' event, repeatedly
/// running the provided function until it returns normally.
/// </summary>
/// <remarks>
/// http://www.fssnip.net/p2
/// </remarks> 
type ResilientMailbox<'T> private(f:ResilientMailbox<'T> -> Async<unit>) as self =

    let event = Event<_>()

    let inbox = new MailboxProcessor<_>(fun _inbox ->

        let rec loop() = 
            async {
                try 
                    return! f self
                with e ->
                    event.Trigger(e)
                    return! loop()
            }
        loop())
    /// Triggered when an unhandled exception occurs
    member __.Error = event.Publish
    member __.Start() = inbox.Start()
    member __.Receive() = inbox.Receive()
    member __.Post(v:'T) = inbox.Post(v)
    member __.PostAndReply(buildMessage:(AsyncReplyChannel<'Reply> -> 'T)) = inbox.PostAndReply(buildMessage)
    static member Start(f) =
        let mbox = new ResilientMailbox<_>(f)
        mbox.Start()
        mbox

let handleLastChanceException (ex:Exception) = 
    match ex with
    | :? ReflectionTypeLoadException as e -> printfn "Exception: %A" ex
                                             e.LoaderExceptions |> Seq.iter (printfn " ==> LoaderException: %A")
    | _ -> printf "Exception: %A" ex

    Environment.Exit(1)