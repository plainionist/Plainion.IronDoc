
Plainion.IronDoc generates .NET XML API documentation.

Unlike other projects like

- [DocFx](https://dotnet.github.io/docfx/)
- [FSharp.Formatting](https://github.com/tpetricek/FSharp.Formatting)
- [NuDoq](https://github.com/kzu/NuDoq)

this project ranks simplicity over flexibility - like it was in the good old days of JavaDoc. 
Therefore it generates Markdown files which will then be rendered nicely by GitHub.

# Usage

Simply run IronDoc from command line:

```Cmd
Plainion.IronDoc.exe -assembly Plainion.IronDoc.exe -output docs/api
```

optionally you can specify the path to soure sources to include ReadMe.md files from your sources in the output:

```Cmd
Plainion.IronDoc.exe -assembly Plainion.IronDoc.exe -sources \ws\Plainion.IronDoc\src\Plainion.IronDoc -output docs/api
```

## Plainion.CI integration

Plainion.IronDoc is integrated in (Plainion.CI)[https://github.com/plainionist/Plainion.CI].


## MsBuild integration

In order to smoothly integrate IronDoc into MsBuild just

- Install the nuget package "Plainion.IronDoc.AfterBuild " for the projects you want to have API doc
- The property "IronDocOutput" defines the output location (default: $(SolutionDir)\doc\Api)
- The property "IronDocSourceFolder" defines the source location (default: $(MSBuildProjectDirectory))


# References

- Inspired by: <http://www.codeproject.com/Articles/1030797/Automatic-Markdown-formatting-for-VS-xml-documenta>
