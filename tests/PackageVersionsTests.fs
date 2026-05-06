module tests.PackageVersionsTests

open ElmishLand.PackageVersions
open Xunit

[<Fact>]
let ``selectLatestMatchingMajor picks the highest version with the given major`` () =
    let versions = [ "4.0.0"; "5.0.0"; "5.1.0"; "5.2.3"; "6.0.0"; "6.1.0" ]
    Assert.Equal(Some "5.2.3", selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor returns None when no version matches`` () =
    let versions = [ "1.0.0"; "2.0.0"; "3.0.0" ]
    Assert.Equal(None, selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor ignores prerelease versions`` () =
    let versions = [ "5.0.0"; "5.1.0"; "5.2.0-preview1"; "5.2.0-rc1"; "6.0.0-preview1" ]

    Assert.Equal(Some "5.1.0", selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor handles unsorted version lists`` () =
    let versions = [ "5.10.0"; "5.2.0"; "5.1.5"; "5.9.1" ]
    Assert.Equal(Some "5.10.0", selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor compares numerically not lexically`` () =
    // Lexical compare would pick "5.9.0" because "9" > "10", but numeric compare picks "5.10.0"
    let versions = [ "5.9.0"; "5.10.0" ]
    Assert.Equal(Some "5.10.0", selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor handles two-segment versions`` () =
    let versions = [ "5.0"; "5.1"; "5.2" ]
    Assert.Equal(Some "5.2", selectLatestMatchingMajor 5 versions)

[<Fact>]
let ``selectLatestMatchingMajor skips garbage entries`` () =
    let versions = [ "not-a-version"; "5.0.0"; ""; "5.1.0" ]
    Assert.Equal(Some "5.1.0", selectLatestMatchingMajor 5 versions)
