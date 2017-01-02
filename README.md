
IronDoc converts .NET XML API documentation into MarkDown.

# Usage

Simply run IronDoc from command line:

```Cmd
Plainion.IronDoc.exe -assembly Plainion.IronDoc.exe -output docs/api
```

optionally you can specify the path to soure sources to include ReadMe.md files from your sources in the output:

```Cmd
Plainion.IronDoc.exe -assembly Plainion.IronDoc.exe -sources \ws\Plainion.IronDoc\src\Plainion.IronDoc -output docs/api
```

## MsBuild integration

In order to smoothly integrate IronDoc into MsBuild just

- Install the nuget package "Plainion.IronDoc.AfterBuild " for the projects you want to have API doc
- The property "IronDocOutput" defines the output location (default: $(SolutionDir)\doc\Api)
- The property "IronDocSourceFolder" defines the source location (default: $(MSBuildProjectDirectory))


# Motivation

Generating API documentation for .NET projects have always be a pain.

Eventhough the situation has improved in the recently with

- [DocFx](https://dotnet.github.io/docfx/)
- [FSharp.Formatting](https://github.com/tpetricek/FSharp.Formatting)
- [NuDoq](https://github.com/kzu/NuDoq)

it is not (yet) as simple as it was in the good old days in Java with the built-in javadoc.
Some existing solutions are complex. Others seem to be a bit out-dated.

This project puts the focus on simplicity. It aims to provide the simplest API/Code documentation possible - 
this is: generate MarkDown files from .NET XML API documentation and put those together with README.md files into the 
repository so that GitHub can nicely render it alltogether.

Of course simplicitly comes with reduced flexibility. If your project grows and the need for more advanced features
increases  I encourage you to switch to one of the "bigger solutions" out there.


# References

- Inspired by: <http://www.codeproject.com/Articles/1030797/Automatic-Markdown-formatting-for-VS-xml-documenta>
