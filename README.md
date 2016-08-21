
# Plainion.IronDoc

- Converts .NET XML API documentation into MarkDown formated files
- nicely integrates with GitHub documentation system
- Inspired by: <http://www.codeproject.com/Articles/1030797/Automatic-Markdown-formatting-for-VS-xml-documenta>

## Usage

Just install the nuget package for the projects you want to have API doc. IronDoc will then automatically during 
normal build create a <project file>.md file next to the <project file>.csproj file.

You can then check this file into your source control system. 

Hint: GitHub will automatically render .md files in HTML.

