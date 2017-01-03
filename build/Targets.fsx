// load dependencies from source folder to allow bootstrapping
#r "/bin/Plainion.CI/FAKE/FakeLib.dll"
#load "/bin/Plainion.CI/bits/PlainionCI.fsx"

open Fake
open PlainionCI

Target "CreatePackage" (fun _ ->
    !! ( outputPath </> "*.*Tests.*" )
    ++ ( outputPath </> "*nunit*" )
    ++ ( outputPath </> "TestResult.xml" )
    ++ ( outputPath </> "**/*.pdb" )
    |> DeleteFiles

    !! ( outputPath </> "Acceptance" )
    |> DeleteDirs

    PZip.PackRelease()

    [
        ("Plainion.IronDoc.*", Some "build", None)
        ( projectRoot </> "src/Plainion.IronDoc/MsBuild/Plainion.IronDoc.targets", Some "build", None)
    ]
    |> PNuGet.Pack (projectRoot </> "build" </> "Plainion.IronDoc.AfterBuild.nuspec") (projectRoot </> "pkg")
)

Target "DeployPackage" (fun _ ->
    let releaseDir = @"\bin\Plainion.IronDoc"

    CleanDir releaseDir

    let zip = PZip.GetReleaseFile()
    Unzip releaseDir zip

    PGitHub.Release [ zip ]

    //PNuGet.PublishPackage "Plainion.IronDoc.AfterBuild" (projectRoot </> "pkg")
    let release = getChangeLog()

    NuGetPublish  (fun p -> {p with OutputPath = (projectRoot </> "pkg")
                                    WorkingDir = projectRoot
                                    Project = "Plainion.IronDoc.AfterBuild"
                                    Version = release.AssemblyVersion
                                    PublishUrl = "https://www.nuget.org/api/v2/package"
                                    Publish = true }) 

)

RunTarget()
