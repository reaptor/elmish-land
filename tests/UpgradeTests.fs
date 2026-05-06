module UpgradeTests

open ElmishLand.Upgrade
open Xunit

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
let ``detectManualMigrations flags React.memo usage with deep link`` () =
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
let ``detectManualMigrations flags React.lazy' usage with deep link`` () =
    let input = "let LazyHello = React.lazy'(fun () -> promise { return! ... })"
    let issues = detectManualMigrations input
    let lazyIssue = issues |> List.tryFind (fun i -> i.Pattern = "React.lazy'")
    Assert.True(lazyIssue.IsSome)
    Assert.Equal("https://fable-hub.github.io/Feliz/api-docs/Upgrade#reactlazy", lazyIssue.Value.DocsUrl)

[<Fact>]
let ``detectManualMigrations flags React.contextProvider and contextConsumer with deep link`` () =
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
