namespace Plainion.IronDoc.FSharp

open System
open System.Diagnostics
open System.IO
open System.Linq
open System.Reflection

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

    
type AssemblyLoader() =
    let mutable myAssemblyBaseDirs = [ AppDomain.CurrentDomain.BaseDirectory ]
    
    let onAssemblyResolve = System.ResolveEventHandler( fun _ e ->
        match ( LoaderUtils.resolveAssembly e.Name ) with
        | Some x -> x
        | None -> null )

    let onReflectionOnlyAssemblyResolve = System.ResolveEventHandler( fun _ e ->
        myAssemblyBaseDirs |> LoaderUtils.resolveReflectionOnlyAssembly e.Name )

    member this.Load assemblyFile =
        myAssemblyBaseDirs = Path.GetDirectoryName(assemblyFile) :: myAssemblyBaseDirs |> ignore

        let register () =
            AppDomain.CurrentDomain.add_AssemblyResolve onAssemblyResolve
            AppDomain.CurrentDomain.add_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve
        
        let unregister () =     
            AppDomain.CurrentDomain.remove_AssemblyResolve onAssemblyResolve
            AppDomain.CurrentDomain.remove_ReflectionOnlyAssemblyResolve onReflectionOnlyAssemblyResolve

        use g = new Interop.Guard( register, unregister )

        LoaderUtils.loadAssembly assemblyFile