module tests.EnsurePageFilesLastTests

open ElmishLand.FsProj
open Xunit

// [<Fact>]
let ``Ensure rewrite page files in fsproj works correctly`` () =
    let originalContent =
        """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Helper1.fs" />
    <Compile Include="src\Util1.fs" />
    <Compile Include="src\Helper2.fs" />
    <Compile Include="src\Helper3.fs" />
    <Compile Include="src/Module1/Module1.fs" />
    <Compile Include="src/Module1/ComponentEditor1.fs" />
    <Compile Include="src\Helper4.fs" />
    <Compile Include="src\Helper5.fs" />
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Compile Include="src\Pages\Page.fs" />
    <Compile Include="src/Pages/PageA/Layout.fs" />
    <Compile Include="src/Pages/PageA/Page.fs" />
    <Compile Include="src\Pages\PageC\Page.fs" />
    <Content Include="src\Pages\PageC\route.json" />
    <Compile Include="src\Pages\PageD\Page.fs" />
    <Compile Include="src\Pages\PageAM\Page.fs" />
    <Compile Include="src/Pages/PageB/_Id1/Layout.fs" />
    <Compile Include="src\Pages\PageB\_Id1\Page.fs" />
    <Content Include="src\Pages\PageB\_Id1\route.json" />
    <Compile Include="src\Pages\PageE\Page.fs" />
    <Compile Include="src\Pages\PageF\Page.fs" />
    <Compile Include="src\Pages\PageF\Section123\Page.fs" />
    <Compile Include="src\Pages\PageAP\Section6.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Layout.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection12.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection13.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection14.fs" />

  </ItemGroup>
</Project>
"""

    let newContent = ensurePageFilesLast originalContent
    Assert.Equal(originalContent, newContent)

[<Fact>]
let ``Ensure page 'Compile Include' order is unchanged if initially correct`` () =
    let originalContent =
        """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Compile Include="src\Pages\Page.fs" />
  </ItemGroup>
</Project>
"""

    let newContent = ensurePageFilesLast originalContent
    Assert.Equal(originalContent, newContent)

[<Fact>]
let ``Ensure page 'Compile Include' order is unchanged if initially correct and contains 'Content Include'`` () =
    let originalContent =
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

    let newContent = ensurePageFilesLast originalContent
    Assert.Equal(originalContent, newContent)


[<Fact>]
let ``Ensure rewrite page files in complex fsproj works correctly`` () =
    let originalContent =
        """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Helper1.fs" />
    <Compile Include="src\Util1.fs" />
    <Compile Include="src\Helper2.fs" />
    <Compile Include="src\Helper3.fs" />
    <Compile Include="src/Module1/Module1.fs" />
    <Compile Include="src/Module1/ComponentEditor1.fs" />
    <Compile Include="src\Helper4.fs" />
    <Compile Include="src\Helper5.fs" />
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Compile Include="src\Pages\Page.fs" />
    <Compile Include="src/Pages/PageA/Layout.fs" />
    <Compile Include="src/Pages/PageA/Page.fs" />
    <Compile Include="src\Pages\PageC\Page.fs" />
    <Content Include="src\Pages\PageC\route.json" />
    <Compile Include="src\Pages\PageD\Page.fs" />
    <Compile Include="src\Pages\PageAM\Page.fs" />
    <Compile Include="src/Pages/PageB/_Id1/Layout.fs" />
    <Compile Include="src\Pages\PageB\_Id1\Page.fs" />
    <Content Include="src\Pages\PageB\_Id1\route.json" />
    <Compile Include="src\Pages\PageE\Page.fs" />
    <Compile Include="src\Pages\PageF\Page.fs" />
    <Compile Include="src\Pages\PageF\Section123\Page.fs" />
    <Compile Include="src\Pages\PageAP\Section6.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Layout.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection12.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection13.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection14.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection1.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\SubSection15.fs" />
    <Compile Include="src/Pages/PageAP\_Id2/Section7/SubSection2.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section7\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section8\SubSection3.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section8\SubSection4.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section8\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section5\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section122\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section25\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section24\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection5.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection6.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection7.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection8.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection9.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection10.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\SubSection11.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAO\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section23\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section22\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section116.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section117.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section16.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section13.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section14.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section15.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section19\Section12.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section19\SubSection16.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section19\SubSection17.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section19\SubSection18.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section19\SubSection19.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Section17.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\PageAQ\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section20\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section21\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section19\Page.fs" />
    <Content Include="src\Pages\PageAP\_Id2\Section19\route.json" />
    <Compile Include="src/Pages/PageAP/_Id2/Section2/Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section1\Section18.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section1\Section117.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section1\Section10\Page.fs" />
    <Compile Include="src\Pages\PageAP\_Id2\Section1\Section11\Page.fs" />
    <Content Include="src\Pages\PageAP\_Id2\route.json" />
    <Compile Include="src\Pages\PageAP\_Id2\Section3\Page.fs" />
      <Compile Include="src\Pages\PageAP\_Id2\Section4\Page.fs"/>
      <Compile Include="src\Pages\PageAP\_Id2\Section9\Page.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\Layout.fs" />
    <Content Include="src\Pages\PageAL\_Id3\route.json" />
    <Compile Include="src\Pages\PageAL\_Id3\Section26\Page.fs" />
    <Content Include="src\Pages\PageAL\_Id3\Section27\route.json" />
    <Compile Include="src\Pages\PageAL\_Id3\Section27\Layout.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\Section27\Page.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\Section28\Page.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\SubSection1\Page.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\Section29\Page.fs" />
    <Compile Include="src\Pages\PageAL\_Id3\Section30\Page.fs" />
    <Compile Include="src/Pages/PageAL/_Id3/Page.fs" />
    <Compile Include="src\Pages\PageG\Util1.fs" />
    <Compile Include="src\Pages\PageG\Section119.fs" />
    <Compile Include="src\Pages\PageG\Section120.fs" />
    <Compile Include="src\Pages\PageG\Page.fs" />
    <Content Include="src\Pages\PageG\route.json" />
    <Compile Include="src\Pages\PageH\Page.fs" />
    <Compile Include="src\Pages\PageI\Page.fs" />
    <Compile Include="src\Pages\PageJ\_Id4\_Type1\Layout.fs" />
    <Compile Include="src\Pages\PageJ\_Id4\_Type1\Page.fs" />
    <Content Include="src\Pages\PageJ\_Id4\_Type1\route.json" />
    <Content Include="src\Pages\PageJ\_Id4\route.json" />
    <Compile Include="src\Pages\PageAK\_Id5\Util1.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section31.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section32.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section33.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section34.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section110.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section35.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\_Param1\Section36\_Id6\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\_Param1\Section36\_Id6\Page.fs" />
    <Content Include="src\Pages\PageAK\_Id5\_Param1\Section36\_Id6\route.json" />
    <Content Include="src\Pages\PageAK\_Id5\_Param1\route.json" />
    <Compile Include="src\Pages\PageAK\_Id5\Section37\Section38.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section37\Section39.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section40.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\Util1.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\Section42.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\Section43\Section118.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\Section43\Section111.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\Section44.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section45\Section45.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section45\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section124\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section36\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section46\Section47.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section46\Section48.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section46\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section49\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section50\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section125\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section51\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section52\Section53.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section52\Section47.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section52\Page.fs" />
    <Content Include="src\Pages\PageAK\_Id5\Section41\_Type1\route.json" />
    <Compile Include="src\Pages\PageAK\_Id5\Section41\_Type1\Section29\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section115.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Util1\Section55.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Util1\Section56.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Util1\Section124.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section125\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section36\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section124\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section49\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section50\Page.fs" />
      <Compile Include="src\Pages\PageAK\_Id5\Section54\Section29\Page.fs"/>
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section45\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section58.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section55\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section59.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section60.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section61.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section62.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section63.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section64.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section115\Section65.fs" />
    <Compile Include="src/Pages/PageAK/_Id5/Section54/Section57/Section45/Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section121\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section34\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section66\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section49\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section54\Section57\Section29\Page.fs"/>
    <Compile Include="src/Pages/PageAK/_Id5/Section54/Section57/Section124/Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Section49\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Section50\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Section124\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Section51\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section67\Section29\Page.fs" />
    <Compile Include="src/Pages/PageAK/_Id5/Section67/Section45/Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section68\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section68\Page.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section69\Layout.fs" />
    <Compile Include="src\Pages\PageAK\_Id5\Section69\Page.fs" />
    <Content Include="src\Pages\PageAK\_Id5\route.json" />
    <Compile Include="src\Pages\PageK\Util1.fs" />
    <Compile Include="src\Pages\PageK\Layout.fs" />
    <Compile Include="src\Pages\PageK\Section91\Page.fs" />
    <Compile Include="src/Pages\PageK\Section118\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section114.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Layout.fs" />
    <Content Include="src\Pages\PageK\_Id5\route.json" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section71.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section72.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section73.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section74.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section112.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section75.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section76.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section77.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section78.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section25.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section79.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section80.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section81.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section82.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section83.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section84.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section85.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section86.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section87.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Section88.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section70\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section113\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section29\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section89\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section90\Page.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section36\_Id6\Layout.fs" />
    <Compile Include="src\Pages\PageK\_Id5\Section36\_Id6\Page.fs" />
    <Content Include="src\Pages\PageK\_Id5\Section36\_Id6\route.json" />
    <Compile Include="src\Pages\PageAI\_Id7\Layout.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Section36\Page.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Util1.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Section32.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Section33.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Section34.fs" />
    <Compile Include="src\Pages\PageAI\_Id7\Page.fs" />
    <Content Include="src\Pages\PageAI\_Id7\route.json" />
    <Compile Include="src\Pages\PageL\Page.fs" />
    <Content Include="src\Pages\PageL\route.json" />
    <Compile Include="src\Pages\PageAJ\Page.fs" />
    <Compile Include="src\Pages\PageM\Section92\Page.fs" />
    <Compile Include="src\Pages\PageM\Section93\Page.fs" />
    <Compile Include="src\Pages\PageM\_Id8\Section94\Layout.fs" />
    <Compile Include="src\Pages\PageM\_Id8\Section94\Page.fs" />
    <Compile Include="src\Pages\PageM\_Id8\Section95\Layout.fs" />
    <Compile Include="src\Pages\PageM\_Id8\Section95\Page.fs" />
    <Content Include="src\Pages\PageM\_Id8\route.json" />
    <Compile Include="src\Pages\PageM\Section96\Page.fs" />
    <Compile Include="src\Pages\PageAO\Section96\Page.fs" />
    <Compile Include="src\Pages\PageAO\Section97\Section127\Page.fs" />
    <Compile Include="src\Pages\PageAO\Section98\Page.fs" />
    <Compile Include="src\Pages\PageAO\Section99\Page.fs" />
    <Compile Include="src/Pages/PageAO/Section100/Page.fs" />
    <Compile Include="src\Pages\PageN\Page.fs" />
    <Compile Include="src\Pages\Section116\Page.fs" />
    <Compile Include="src\Pages\PageO\_Param2\Page.fs" />
    <Content Include="src\Pages\PageO\_Param2\route.json" />
    <Compile Include="src\Pages\PageP\Page.fs" />
    <Compile Include="src\Pages\PageQ\Page.fs" />
    <Compile Include="src\Pages\PageR\Page.fs" />
    <Compile Include="src\Pages\PageS\Page.fs" />
    <Compile Include="src\Pages\PageT\Page.fs" />
    <Compile Include="src\Pages\PageU\Page.fs" />
    <Compile Include="src\Pages\PageV\_Param2\Page.fs" />
    <Content Include="src\Pages\PageV\_Param2\route.json" />
    <Compile Include="src\Pages\PageW\Layout.fs" />
    <Compile Include="src\Pages\PageW\Page.fs" />
    <Compile Include="src\Pages\PageY\Page.fs" />
    <Compile Include="src\Pages\PageX\_Param3\Page.fs" />
    <Compile Include="src\Pages\PageZ\Page.fs" />
    <Compile Include="src\Pages\PageAA\Page.fs" />
    <Compile Include="src\Pages\PageAB\Page.fs" />
    <Compile Include="src/Pages/PageAC/Page.fs" />
    <Compile Include="src\Pages\PageAH\Util1.fs" />
    <Compile Include="src\Pages\PageAH\Section101\Util1.fs" />
    <Compile Include="src\Pages\PageAH\Section101\Page.fs" />
    <Compile Include="src\Pages\PageAH\Section101\_Id9\Layout.fs" />
    <Compile Include="src\Pages\PageAH\Section101\_Id9\Page.fs" />
    <Compile Include="src\Pages\PageAH\Section102\Page.fs" />
    <Compile Include="src\Pages\PageAD\Page.fs" />
    <Compile Include="src\Pages\PageAE\Page.fs" />
    <Compile Include="src\Pages\PageAN\Section103\Page.fs" />
      <Compile Include="src\Pages\PageAN\Section104\Page.fs"/>
      <Compile Include="src\Pages\PageAN\Section105\Page.fs"/>
    <Compile Include="src\Pages\PageAF\Page.fs" />
    <Compile Include="src\Pages\PageAG\Section106\Page.fs" />
    <Compile Include="src\Pages\PageAG\Section109\Section107.fs" />
    <Compile Include="src\Pages\PageAG\Section109\Section108.fs" />
    <Compile Include="src\Pages\PageAG\Section109\Page.fs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Fable.Browser.Url" />
    <PackageReference Include="Fable.Elmish.UrlParser" />
    <PackageReference Include="Fable.Browser.Blob" />
    <PackageReference Include="Fable.Browser.Dom" />
    <PackageReference Include="Fable.Core" />
    <PackageReference Include="Fable.Date" />
    <PackageReference Include="Fable.Elmish.Browser" />
    <PackageReference Include="Fable.Elmish.HMR" />
    <PackageReference Include="Fable.Elmish.React" />
    <PackageReference Include="Fable.Fetch" />
    <PackageReference Include="Fable.Promise" />
    <PackageReference Include="Feliz" />
    <PackageReference Include="Thoth.Fetch" />
    <PackageReference Include="Thoth.Json" />
    <PackageReference Include="Fable.Mocha" />
  </ItemGroup>
</Project>
"""

    let newContent = ensurePageFilesLast originalContent
    Assert.Equal(originalContent, newContent)


[<Fact>]
let ``Ensure page 'Compile Include' order is changed if initially incorrect`` () =
    let originalContent =
        """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\Page.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
  </ItemGroup>
</Project>
"""

    let expectedContent =
        """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <Compile Include="src/Pages/Layout.fs" />
    <Compile Include="src\Pages\NotFound.fs" />
    <Compile Include="src\Pages\Page.fs" />
  </ItemGroup>
</Project>
"""

    let newContent = ensurePageFilesLast originalContent
    Assert.Equal(expectedContent, newContent)
