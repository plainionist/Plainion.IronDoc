﻿namespace Plainion.IronDoc.MsBuild

open System
open System.Linq
open System.Reflection
open Microsoft.Build.Framework
open Microsoft.Build.Utilities
open Plainion.IronDoc

/// <summary>
/// MsBuild task to generate API documentation
/// </summary>
type IronDoc() =
    inherit Task()

    [<Required>]
    member val Assembly : string = null with get, set

    [<Output>]
    member val Output : string = null with get, set

    override this.Execute() =
        this.Log.LogMessage( MessageImportance.Normal, "IronDoc generation started" )

        try
            Workflows.generateAssemblyFileDoc this.Assembly this.Output 

            this.Log.LogMessage( MessageImportance.Normal, "IronDoc generation Finished. Output written to: {0}", this.Output )

            true
        with
        | :? ReflectionTypeLoadException as ex -> 
            ex.LoaderExceptions
            |> Seq.map( fun e -> e.Message.ToString() )
            |> Seq.iter( fun msg -> this.Log.LogError( msg ) )
            false
        | ex ->
            this.Log.LogError( ex.ToString() )
            false
