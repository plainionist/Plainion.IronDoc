namespace Plainion.IronDoc.FSharp

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Reflection

//type AssemblyLoader =
//    let myAssemblyBaseDirs = new List<string> { AppDomain.CurrentDomain.BaseDirectory }

//    new () =
//        AppDomain.CurrentDomain.AssemblyResolve.AddHandler( OnAssemblyResolve )
//        //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve.Add OnReflectionOnlyAssemblyResolve
//
//    let OnAssemblyResolve sender ( args : ResolveEventArgs ) =
//        AppDomain.CurrentDomain.GetAssemblies()
//                .FirstOrDefault( fun asm -> String.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));

//    private Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
//    {
//        var loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
//                .FirstOrDefault(asm => string.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));
//
//        if (loadedAssembly != null)
//        {
//            return loadedAssembly;
//        }
//
//        var assemblyName = new AssemblyName(args.Name);
//        var dependentAssemblyPath = GetAssemblyPath(assemblyName);
//
//        if (!File.Exists(dependentAssemblyPath))
//        {
//            try
//            {
//                // e.g. .NET assemblies, assemblies from GAC
//                return Assembly.ReflectionOnlyLoad(args.Name);
//            }
//            catch
//            {
//                // ignore exception here - e.g. System.Windows.Interactivity - app will work without
//                Debug.WriteLine("Failed to load: " + assemblyName);
//                return null;
//            }
//        }
//
//        return Load(dependentAssemblyPath);
//    }
//
//    private string GetAssemblyPath(AssemblyName assemblyName)
//    {
//        return myAssemblyBaseDirs
//            .SelectMany(baseDir => new string[] {".dll", ".exe"}
//                .Select( ext => Path.Combine(baseDir, assemblyName.Name + ext)))
//            .FirstOrDefault(File.Exists);
//    }
//
//    interface IDisposable with
//        member this.Dispose() =
//            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;
//            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
//
//    member this.Load assemblyFile =
//        myAssemblyBaseDirs.Add( Path.GetDirectoryName(assemblyFile) )
//
//        // Load assembly from byte[] to avoid getting the file locked by our process
//
//        let assemblyBytes = File.ReadAllBytes(assemblyFile)
//        Assembly.ReflectionOnlyLoad( assemblyBytes )
