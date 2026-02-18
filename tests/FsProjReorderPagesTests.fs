module tests.FsProjReorderPagesTests

open ElmishLand.FsProj
open Xunit

[<Fact>]
let ``Correct page file order, return empty preview`` () =
    let preview =
        previewPageFilesReordering
            """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Content Include="src\Pages\route.json" />
    <Compile Include="src\Pages\Page.fs" />
  </ItemGroup>
</Project>
"""

    Assert.Empty(preview)

[<Fact>]
let ``Incorrect page file order, return preview of fix`` () =
    let preview =
        previewPageFilesReordering
            """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\Page.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Content Include="src\Pages\route.json" />
  </ItemGroup>
</Project>
"""

    let expectedPreview =
        """
...
Reordered Compile entries:
[31m  - src\Pages\Page.fs (moved from position 2 to 3)
[0m[32m  + src\Pages\Page.fs (moved to end of directory)
[0m..."""

    Assert.Equal(expectedPreview.Replace("\r\n", "\n"), preview.Replace("\r\n", "\n"))
