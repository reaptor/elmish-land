module UpgradeTests

open System.IO
open ElmishLand.Base
open ElmishLand.Upgrade
open Xunit
open Runner
open Orsak
open TestProjectGeneration

[<Fact>]
let ``upgradeFelizSource renames React.fragment to React.Fragment`` () =
    let input = "let x = React.fragment [ ]"
    let actual, count = upgradeFelizSource input
    Assert.Equal("let x = React.Fragment [ ]", actual)
    Assert.Equal(1, count)

[<Fact>]
let ``upgradeFelizSource renames all PascalCase components`` () =
    let input =
        """
let a = React.fragment []
let b = React.keyedFragment []
let c = React.imported "x"
let d = React.dynamicImported "x"
let e = React.strictMode []
let f = React.suspense []
"""

    let actual, count = upgradeFelizSource input

    Assert.Contains("React.Fragment", actual)
    Assert.Contains("React.KeyedFragment", actual)
    Assert.Contains("React.Imported", actual)
    Assert.Contains("React.DynamicImported", actual)
    Assert.Contains("React.StrictMode", actual)
    Assert.Contains("React.Suspense", actual)
    Assert.DoesNotContain("React.fragment ", actual)
    Assert.DoesNotContain("React.keyedFragment", actual)
    Assert.DoesNotContain("React.suspense", actual)
    Assert.Equal(6, count)

[<Fact>]
let ``upgradeFelizSource moves disposable helpers into FsReact namespace`` () =
    let input =
        """
let a = React.createDisposable (fun () -> ())
let b = React.useDisposable (fun () -> ())
let c = React.useCancellationToken ()
"""

    let expected =
        """
let a = FsReact.createDisposable (fun () -> ())
let b = FsReact.useDisposable (fun () -> ())
let c = FsReact.useCancellationToken ()
"""

    let actual, count = upgradeFelizSource input
    Assert.Equal(expected, actual)
    Assert.Equal(3, count)

[<Fact>]
let ``upgradeFelizSource does not rename React.fragmentExtra or other identifiers`` () =
    let input = "let x = React.fragmentExtra []"
    let actual, count = upgradeFelizSource input
    Assert.Equal(input, actual)
    Assert.Equal(0, count)

[<Fact>]
let ``upgradeFelizSource leaves already-upgraded code unchanged`` () =
    let input =
        """
let a = React.Fragment []
let b = FsReact.createDisposable (fun () -> ())
"""

    let actual, count = upgradeFelizSource input
    Assert.Equal(input, actual)
    Assert.Equal(0, count)

[<Fact>]
let ``detectManualMigrations flags React.memo usage`` () =
    let input =
        """let MemoFunction = React.memo<{|text: string|}> (fun props -> Html.div [])
let X = React.memoRenderer (MemoFunction, {| text = "x" |})"""

    let issues = detectManualMigrations input
    let memoIssue = issues |> List.tryFind (fun i -> i.Pattern = "React.memo")
    Assert.True(memoIssue.IsSome)
    Assert.Equal(1, memoIssue.Value.Line)
    Assert.Equal("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactmemo", memoIssue.Value.DocsUrl)

[<Fact>]
let ``detectManualMigrations does not flag React.memoRenderer alone`` () =
    let input = "let X = React.memoRenderer (M, ())"
    let issues = detectManualMigrations input
    Assert.Empty(issues)

[<Fact>]
let ``detectManualMigrations flags React.lazy' usage`` () =
    let input = "let LazyHello = React.lazy'(fun () -> promise { return! ... })"
    let issues = detectManualMigrations input
    let lazyIssue = issues |> List.tryFind (fun i -> i.Pattern = "React.lazy'")
    Assert.True(lazyIssue.IsSome)
    Assert.Equal("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactlazy", lazyIssue.Value.DocsUrl)

[<Fact>]
let ``detectManualMigrations flags React.contextProvider and contextConsumer`` () =
    let input =
        """let p = React.contextProvider (Ctx, value, children)
let c = React.contextConsumer (Ctx, render)"""

    let issues = detectManualMigrations input
    Assert.Equal(2, issues.Length)
    Assert.True(issues |> List.forall (fun i -> i.Pattern = "React.context*"))

    Assert.True(
        issues
        |> List.forall (fun i -> i.DocsUrl = "https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactcontext")
    )

[<Fact>]
let ``upgrade command prints per-pattern feliz docs URL in manual-migration warnings`` () =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir
            let testPath = Path.Combine(folder, "src", "Pages", "Page.fs")
            let original = File.ReadAllText(testPath)

            let withManualPatterns =
                original
                + "\nlet MemoFn = React.memo<unit> (fun _ -> Html.div [])\n"
                + "let LazyFn = React.lazy'(fun () -> promise { return! Unchecked.defaultof<_> })\n"
                + "let p = React.contextProvider (Ctx, value, children)\n"

            File.WriteAllText(testPath, withManualPatterns)

            let! result, logs = runEff (upgrade (FilePath.fromString folder) absoluteProjectDir AutoAccept)
            let _ = Expects.ok logs result

            let infoOutput = logs.Info.ToString()
            Assert.Contains("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactmemo", infoOutput)
            Assert.Contains("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactlazy", infoOutput)
            Assert.Contains("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactcontext", infoOutput)
        })

[<Fact>]
let ``upgradeProjectPackagesXml bumps Feliz version`` () =
    let input =
        """<Project>
    <ItemGroup>
        <PackageVersion Include="Feliz" Version="2.9.0" />
        <PackageVersion Include="Feliz.Router" Version="4.0.0" />
        <PackageVersion Include="FSharp.Core" Version="10.0.100" />
    </ItemGroup>
</Project>"""

    let actual, changed = upgradeProjectPackagesXml input
    Assert.True(changed)

    let felizDep = nugetDependencies |> Seq.find (fun (n, _) -> n = "Feliz") |> snd

    Assert.Contains($"<PackageVersion Include=\"Feliz\" Version=\"%s{felizDep}\" />", actual)

[<Fact>]
let ``upgradeProjectPackagesXml leaves unrelated content unchanged`` () =
    let input =
        """<Project>
    <ItemGroup>
        <PackageVersion Include="SomeOtherPackage" Version="1.0.0" />
    </ItemGroup>
</Project>"""

    let actual, changed = upgradeProjectPackagesXml input
    Assert.False(changed)
    Assert.Equal(input, actual)

[<Fact>]
let ``upgradePackageJson bumps react and vite versions`` () =
    let input =
        """{
  "name": "demo",
  "dependencies": {
    "react": "^18",
    "react-dom": "^18"
  },
  "devDependencies": {
    "vite": "^5"
  }
}"""

    let actual, changed = upgradePackageJson input
    Assert.True(changed)
    Assert.Contains("\"react\": \"^19\"", actual)
    Assert.Contains("\"react-dom\": \"^19\"", actual)
    Assert.Contains("\"vite\": \"^7\"", actual)

[<Fact>]
let ``upgradeDotnetToolsJson bumps fable to v5 prerelease`` () =
    let input =
        """{
  "version": 1,
  "isRoot": true,
  "tools": {
    "fable": {
      "version": "4.25.0",
      "commands": [ "fable" ]
    }
  }
}"""

    let actual, changed = upgradeDotnetToolsJson input
    Assert.True(changed)
    Assert.Contains("\"fable\"", actual)
    // Should mention the new major version 5 in some form
    Assert.Matches(@"""version""\s*:\s*""5\.[^""]*""", actual)

[<Fact>]
let ``upgrade command rewrites a project's source files`` () =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            // Plant a file with Feliz 2 patterns into the user source tree
            let testPath = Path.Combine(folder, "src", "Pages", "Page.fs")
            let original = File.ReadAllText(testPath)

            let withOldPatterns =
                original
                + "\nlet myFragment = React.fragment []\nuse d = React.createDisposable (fun () -> ())\n"

            File.WriteAllText(testPath, withOldPatterns)

            let! result, logs = runEff (upgrade (FilePath.fromString folder) absoluteProjectDir AutoAccept)

            let _ = Expects.ok logs result

            let updated = File.ReadAllText(testPath)
            Assert.Contains("React.Fragment", updated)
            Assert.Contains("FsReact.createDisposable", updated)
            Assert.DoesNotContain("React.fragment ", updated)
            // No remaining un-prefixed React.createDisposable.
            let stripped = updated.Replace("FsReact.createDisposable", "")
            Assert.DoesNotContain("React.createDisposable", stripped)
        })
