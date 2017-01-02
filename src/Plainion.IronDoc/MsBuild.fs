namespace Plainion.IronDoc.MsBuild

open System
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

    member val SourceFolder : string = null with get, set

    [<Output>]
    member val Output : string = null with get, set

    override this.Execute() =
        this.Log.LogMessage( MessageImportance.Normal, "IronDoc generation started" )

        try
            let src = if String.IsNullOrEmpty(this.SourceFolder) then None else Some this.SourceFolder
            Workflows.generateAssemblyFileDoc this.Output this.Assembly src

            this.Log.LogMessage( MessageImportance.Normal, "IronDoc generation Finished. Output written to: {0}", this.Output )

            true
        with
        | ex ->
            this.Log.LogError( ex.ToString() )
            false
