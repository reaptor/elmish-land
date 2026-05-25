module tests.BaseTests

open System
open ElmishLand.Base
open Xunit

[<Fact>]
let ``fromString parses a stable version`` () =
    Assert.Equal(Some(DotnetSdkVersion(Version(10, 0, 300))), DotnetSdkVersion.fromString "10.0.300")

[<Fact>]
let ``fromString parses a preview version`` () =
    // Regression test for https://github.com/reaptor/elmish-land/issues/40 — System.Version
    // can't parse the "-preview..." suffix, which made preview SDKs fail the minimum check.
    Assert.Equal(
        Some(DotnetSdkVersion(Version(10, 0, 300))),
        DotnetSdkVersion.fromString "10.0.300-preview.0.26177.108"
    )

[<Fact>]
let ``fromString accepts a preview version above the minimum`` () =
    match DotnetSdkVersion.fromString "10.0.300-preview.0.26177.108" with
    | Some v -> Assert.True(DotnetSdkVersion.value v >= DotnetSdkVersion.value minimumRequiredDotnetSdk)
    | None -> Assert.True(false, "Expected preview version to parse")

[<Fact>]
let ``fromString parses an rc version`` () =
    Assert.Equal(Some(DotnetSdkVersion(Version(8, 0, 100))), DotnetSdkVersion.fromString "8.0.100-rc.1.23463.5")

[<Fact>]
let ``fromString parses a below-minimum version which compares less than the minimum`` () =
    match DotnetSdkVersion.fromString "6.0.100" with
    | Some v -> Assert.True(DotnetSdkVersion.value v < DotnetSdkVersion.value minimumRequiredDotnetSdk)
    | None -> Assert.True(false, "Expected version to parse")

[<Fact>]
let ``fromString returns None for garbage`` () =
    Assert.Equal(None, DotnetSdkVersion.fromString "not-a-version")
