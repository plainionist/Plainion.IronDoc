
# Plainion.IronDoc

- Converts .NET XML API documentation into MarkDown formated files
- nicely integrates with GitHub documentation system
- Inspired by: <http://www.codeproject.com/Articles/1030797/Automatic-Markdown-formatting-for-VS-xml-documenta>


## Motivation

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


## Usage

Just install the nuget package for the projects you want to have API doc. IronDoc will then automatically during 
normal build create a <project file>.md file next to the <project file>.csproj file.

You can then check this file into your source control system. 

Hint: GitHub will automatically render .md files in HTML.

