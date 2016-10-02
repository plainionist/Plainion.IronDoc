namespace Plainion.IronDoc.FSharp

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Reflection
open System.Xml.Linq
open Plainion.IronDoc.FSharp

module internal LoaderUtils = 

    let loadAssembly assemblyFile =
        // Load assembly from byte[] to avoid getting the file locked by our process

        let assemblyBytes = File.ReadAllBytes assemblyFile
        Assembly.ReflectionOnlyLoad assemblyBytes

    let resolveAssembly name =
        AppDomain.CurrentDomain.GetAssemblies ()
        |> Array.tryFind( fun asm -> String.Equals(asm.FullName, name, StringComparison.OrdinalIgnoreCase) )

    let getAssemblyPath ( assemblyName : AssemblyName ) baseDirs =
        let assemblyExtensions = [ ".dll"; ".exe" ]

        baseDirs
        |> List.collect( fun baseDir -> assemblyExtensions |> List.map( fun ext -> Path.Combine(baseDir, assemblyName.Name + ext) ) )
        |> List.tryFind File.Exists

    let resolveReflectionOnlyAssembly ( assemblyName : string ) baseDirs =
        let loadedAssembly = 
            AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies ()
            |> Array.tryFind( fun asm -> String.Equals(asm.FullName, assemblyName, StringComparison.OrdinalIgnoreCase) )

        match loadedAssembly with
        | Some x -> x
        | None ->
            let assemblyName = new AssemblyName( assemblyName )
            let dependentAssemblyPath = baseDirs |> getAssemblyPath assemblyName

            match dependentAssemblyPath with
            | None -> null
            | Some x -> 
                if not ( File.Exists x ) then
                    try
                        // e.g. .NET assemblies, assemblies from GAC
                        Assembly.ReflectionOnlyLoad assemblyName.Name
                    with
                    | _ -> 
                        // ignore exception here - e.g. System.Windows.Interactivity - app will work without
                        Debug.WriteLine ( "Failed to load: " + assemblyName.ToString() )
                        null
                else
                    loadAssembly x

    
type LoadAssemblyMsg = 
    | LoadAssembly of string * replyChannel : AsyncReplyChannel<Assembly>
    | Stop 

// https://kimsereyblog.blogspot.de/2016/07/manage-mutable-state-using-actors-with.html
type AssemblyLoaderApi = {
    Load: string -> Assembly
    Stop: unit -> unit
}

module AssemblyLoader = 
    let instance =
        let agent = MailboxProcessor<LoadAssemblyMsg>.Start(fun inbox ->
            let loadAssembly baseDirs assembly = 
                let newBaseDirs = Path.GetDirectoryName(assembly) :: baseDirs
                                  |> Seq.distinct
                                  |> List.ofSeq

                let onAssemblyResolve = System.ResolveEventHandler( fun _ e ->
                    match ( LoaderUtils.resolveAssembly e.Name ) with
                    | Some x -> x
                    | None -> null )

                let onReflectionOnlyAssemblyResolve = System.ResolveEventHandler( fun _ e ->
                    newBaseDirs |> LoaderUtils.resolveReflectionOnlyAssembly e.Name )

                let register () =
                    AppDomain.CurrentDomain.add_AssemblyResolve onAssemblyResolve
                    AppDomain.CurrentDomain.add_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve
        
                let unregister () =     
                    AppDomain.CurrentDomain.remove_AssemblyResolve onAssemblyResolve
                    AppDomain.CurrentDomain.remove_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve

                use g = new Interop.Guard( register, unregister )

                newBaseDirs, LoaderUtils.loadAssembly assembly

            let rec loop baseDirs =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | LoadAssembly (file, replyChannel) -> 
                        let newBaseDirs, assembly = file |> loadAssembly baseDirs
                    
                        replyChannel.Reply assembly

                        return! loop newBaseDirs
                    | Stop -> return ()
                }
            loop [ AppDomain.CurrentDomain.BaseDirectory ] ) 
        { Load = fun assembly -> agent.PostAndReply( fun replyChannel -> LoadAssembly( assembly, replyChannel ) )
          Stop = fun () -> agent.Post Stop }

module XmlDocDocument = 
    let private (!!) : string -> XName = Interop.implicit
    
    let private getMemberName (memberInfo : MemberInfo) = 
        match memberInfo.MemberType with
        | MemberTypes.Constructor -> "#ctor" // XML documentation uses slightly different constructor names
        | MemberTypes.NestedType -> memberInfo.DeclaringType.Name + "." + memberInfo.Name
        | _ -> memberInfo.Name
    
    let private getFullMemberName (memberInfo : MemberInfo) = 
        match memberInfo with
        | :? Type as t -> t.Namespace + "." + (getMemberName memberInfo) // member is a Type
        | _ -> memberInfo.DeclaringType.FullName + "." + (getMemberName memberInfo)
    
    /// elements are of the form "M:Namespace.Class.Method"
    let private getMemberId prefixCode memberName = sprintf "%s:%s" prefixCode memberName
    
    /// parameters are listed according to their type, not their name
    let private getMethodParameterSignature (memberInfo : MemberInfo) = 
        let parameters = (memberInfo :?> MethodBase).GetParameters()
        match parameters with
        | [||] -> ""
        | _ -> 
            "(" + (parameters
                   |> Seq.map (fun p -> p.ParameterType.FullName)
                   |> String.concat ",")
            + ")"
    
    let private getMemberElementName (mi : MemberInfo) = 
        match mi.MemberType with
        | MemberTypes.Constructor -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
        | MemberTypes.Method -> getMemberId "M" (getFullMemberName mi + getMethodParameterSignature mi)
        | MemberTypes.Event -> getMemberId "E" (getFullMemberName mi)
        | MemberTypes.Field -> getMemberId "F" (getFullMemberName mi)
        | MemberTypes.NestedType -> getMemberId "T" (getFullMemberName mi)
        | MemberTypes.TypeInfo -> getMemberId "T" (getFullMemberName mi)
        | MemberTypes.Property -> getMemberId "P" (getFullMemberName mi)
        | _ -> failwith "Unknown MemberType: " + mi.MemberType.ToString()
    
    type Contents(assemblyName, members : XElement list) = 
        member this.AssemblyName = assemblyName
        member this.Members = members
        member this.GetXmlDocumentation memberInfo = 
            let memberName = getMemberElementName memberInfo
            let doc = this.Members |> Seq.tryFind (fun m -> m.Attribute(!!"name").Value = memberName)
            match doc with
            | Some x -> x
            | None -> null
    
    let Load(root : XElement) = 
        new Contents(root.Element(!!"assembly").Element(!!"name").Value, 
                     root.Element(!!"members").Elements(!!"member") |> List.ofSeq)
    let LoadFile(file : string) = Load(XElement.Load file)
