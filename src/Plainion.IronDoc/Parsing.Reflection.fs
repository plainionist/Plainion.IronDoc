// reads relevant information from assembly
namespace Plainion.IronDoc.Parsing

[<AutoOpen>]
module private ReflectionImpl = 
    open System
    open System.IO
    open System.Reflection
    open System.Diagnostics
    open Plainion.IronDoc

    /// <summary>
    /// Load assembly from byte[] to avoid getting the file locked by our process
    /// </summary>
    let reflectionOnlyLoad assemblyFile =
        let assemblyBytes = File.ReadAllBytes assemblyFile
        { DAssembly.name = Path.GetFileNameWithoutExtension(assemblyFile)
          location = Path.GetFullPath(assemblyFile)
          assembly = Assembly.ReflectionOnlyLoad assemblyBytes }

    let getAssemblyLocation ( assemblyName : AssemblyName ) baseDirs =
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
            let dependentAssemblyPath = baseDirs |> getAssemblyLocation assemblyName

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
                    (reflectionOnlyLoad x).assembly

    let loadAssembly baseDirs assembly = 
        let newBaseDirs = Path.GetDirectoryName(assembly) :: baseDirs
                            |> Seq.distinct
                            |> List.ofSeq

        let onAssemblyResolve = System.ResolveEventHandler( fun _ e ->
            let assembly = AppDomain.CurrentDomain.GetAssemblies ()
                           |> Array.tryFind( fun asm -> String.Equals(asm.FullName, e.Name, StringComparison.OrdinalIgnoreCase) )

            match assembly with
            | Some x -> x
            | None -> null )

        let onReflectionOnlyAssemblyResolve = System.ResolveEventHandler( fun _ e ->
            newBaseDirs |> resolveReflectionOnlyAssembly e.Name )

        let register () =
            AppDomain.CurrentDomain.add_AssemblyResolve onAssemblyResolve
            AppDomain.CurrentDomain.add_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve
        
        let unregister () =     
            AppDomain.CurrentDomain.remove_AssemblyResolve onAssemblyResolve
            AppDomain.CurrentDomain.remove_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve

        use g = new Guard( register, unregister )

        newBaseDirs, reflectionOnlyLoad assembly
    
    type LoadAssemblyMsg = 
        | LoadAssembly of string * replyChannel : AsyncReplyChannel<DAssembly>
        | Stop 

[<AutoOpen>]
module ReflectionApi = 
    open System
    open System.Reflection
    open Plainion.IronDoc

    // https://kimsereyblog.blogspot.de/2016/07/manage-mutable-state-using-actors-with.html
    type AssemblyLoaderApi = {
        Load: string -> DAssembly
        Stop: unit -> unit
    }

    let assemblyLoader =
        let agent = MailboxProcessor<LoadAssemblyMsg>.Start(fun inbox ->
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

    let rec createDType assembly (t : Type) =
        let bindingFlags = BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly ||| BindingFlags.Public

        let getParameters (memberInfo : MemberInfo) = 
            (memberInfo :?> MethodBase).GetParameters()
            |> Seq.map(fun x -> { name = x.Name
                                  parameterType = x.ParameterType })
            |> List.ofSeq

        { assembly = assembly
          nameSpace = t.Namespace
          name = t.Name
          fields =  t.GetFields(bindingFlags) 
                    |> Seq.map(fun x -> { name = x.Name
                                          fieldType = x.FieldType })
                    |> List.ofSeq
          constructors = t.GetConstructors(bindingFlags) 
                         |> Seq.map(fun x -> { Constructor.parameters = getParameters x })
                         |> List.ofSeq
          properties = t.GetProperties(bindingFlags) 
                       |> Seq.map(fun x -> { name = x.Name
                                             propertyType = x.PropertyType })
                       |> List.ofSeq
          events = t.GetEvents(bindingFlags) 
                   |> Seq.map(fun x -> { name = x.Name
                                         eventHandlerType = x.EventHandlerType})
                   |> List.ofSeq
          methods = t.GetMethods(bindingFlags) 
                    |> Seq.filter(fun x -> not (x.IsSpecialName))
                    |> Seq.map(fun x -> { name = x.Name
                                          parameters = getParameters x
                                          returnType = x.ReturnType})
                    |> List.ofSeq
          nestedTypes = t.GetNestedTypes(bindingFlags) 
                        |> Seq.map (createDType assembly)
                        |> List.ofSeq
        }
