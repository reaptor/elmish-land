module Tests

open System
open Xunit

let getFolder () =
    $"""Proj_%s{Guid.NewGuid().ToString().Replace("-", "")}"""


[<Fact>]
let ``detectLayoutMismatch works with any page and layout combination`` () =
    task {
        // Test 1: Products page with Products.Layout.Msg
        let productError = "./src/Pages/Products/Page.fs(10,20): (10,30) error FSHARP: The type 'Layout.Msg' does not match the type 'Products.Layout.Msg' (code 1)"
        let result1 = ElmishLand.FableOutput.processOutput productError productError false
        Assert.True(result1.Errors |> List.length >= 1, "Should detect Products layout mismatch")
        let error1 = result1.Errors |> List.head
        Assert.Contains("Products", error1)
        Assert.Contains("Layout.Msg", error1)
        Assert.Contains("Products.Layout.Msg", error1)
        Assert.True(result1.LayoutMismatches |> List.length >= 1)
        let mismatch1 = result1.LayoutMismatches |> List.head
        Assert.Equal("Layout.Msg", mismatch1.WrongLayout)
        Assert.Equal("Products.Layout.Msg", mismatch1.CorrectLayout)

        // Test 2: Users page with Users.Layout.Msg
        let userError = "./src/Pages/Users/Settings/Page.fs(15,10): (15,20) error FSHARP: The type 'Layout.Msg' does not match the type 'Users.Settings.Layout.Msg' (code 1)"
        let result2 = ElmishLand.FableOutput.processOutput userError userError false
        Assert.True(result2.Errors |> List.length >= 1, "Should detect Users.Settings layout mismatch")
        let error2 = result2.Errors |> List.head
        Assert.Contains("Users/Settings", error2)
        Assert.Contains("Layout.Msg", error2)
        Assert.Contains("Users.Settings.Layout.Msg", error2)
        let mismatch2 = result2.LayoutMismatches |> List.head
        Assert.Equal("Layout.Msg", mismatch2.WrongLayout)
        Assert.Equal("Users.Settings.Layout.Msg", mismatch2.CorrectLayout)

        // Test 3: Admin.Dashboard page with Admin.Dashboard.Layout.Msg
        let adminError = "./src/Pages/Admin/Dashboard/Page.fs(20,5): (20,15) error FSHARP: The type 'Pages.Layout.Msg' does not match the type 'Admin.Dashboard.Layout.Msg' (code 1)"
        let result3 = ElmishLand.FableOutput.processOutput adminError adminError false
        Assert.True(result3.Errors |> List.length >= 1, "Should detect Admin.Dashboard layout mismatch")
        let error3 = result3.Errors |> List.head
        Assert.Contains("Admin/Dashboard", error3)
        Assert.Contains("Pages.Layout.Msg", error3)
        Assert.Contains("Admin.Dashboard.Layout.Msg", error3)
        let mismatch3 = result3.LayoutMismatches |> List.head
        Assert.Equal("Pages.Layout.Msg", mismatch3.WrongLayout)
        Assert.Equal("Admin.Dashboard.Layout.Msg", mismatch3.CorrectLayout)

        // Test 4: Generated App.fs error for Store.Products page
        let appError = "./.elmish-land/App/App.fs(250,20): (250,30) error FSHARP: The type 'Layout.Msg' does not match the type 'Store.Products.Layout.Msg' (code 1)"
        let result4 = ElmishLand.FableOutput.processOutput appError appError false
        Assert.True(result4.Errors |> List.length >= 1, "Should detect Store.Products layout mismatch in App.fs")
        let error4 = result4.Errors |> List.head
        Assert.Contains("Store/Products", error4)
        Assert.Contains("Pages.Layout.Msg", error4)
        Assert.Contains("Store.Products.Layout.Msg", error4)
        let mismatch4 = result4.LayoutMismatches |> List.head
        Assert.Equal("Pages.Layout.Msg", mismatch4.WrongLayout)
        Assert.Equal("Store.Products.Layout.Msg", mismatch4.CorrectLayout)

        // Test 5: Deep nesting - Admin.Users.Settings
        let deepError = "./src/Pages/Admin/Users/Settings/Page.fs(30,10): (30,20) error FSHARP: The type 'Layout.Msg' does not match the type 'Admin.Users.Settings.Layout.Msg' (code 1)"
        let result5 = ElmishLand.FableOutput.processOutput deepError deepError false
        Assert.True(result5.Errors |> List.length >= 1, "Should detect deeply nested layout mismatch")
        let error5 = result5.Errors |> List.head
        Assert.Contains("Admin/Users/Settings", error5)
        Assert.Contains("Layout.Msg", error5)
        Assert.Contains("Admin.Users.Settings.Layout.Msg", error5)
        let mismatch5 = result5.LayoutMismatches |> List.head
        Assert.Equal("Layout.Msg", mismatch5.WrongLayout)
        Assert.Equal("Admin.Users.Settings.Layout.Msg", mismatch5.CorrectLayout)
    }

[<Fact>]
let ``processOutput for page with incorrect layout, reports correct error message`` () =
    let buildOutput = """
  Started Fable compilation...

  Fable compilation finished in 4486ms
  ./.elmish-land/App/App.fs(156,118): (156,136) error FSHARP: Type mismatch. Expecting a    'Pages.Layout.Msg -> LayoutMsg'    but given a    'Pages.Hello.Layout.Msg -> LayoutMsg'    The type 'Pages.Layout.Msg' does not match the type 'Pages.Hello.Layout.Msg' (code 1)
  ./.elmish-land/App/App.fs(163,38): (163,48) error FSHARP: The type 'Pages.Hello.Layout.Msg' does not match the type 'Pages.Layout.Msg' (code 1)
  ./.elmish-land/App/App.fs(167,88): (167,98) error FSHARP: The type 'Pages.Hello.Layout.Msg' does not match the type 'Pages.Layout.Msg' (code 1)
  ./.elmish-land/App/App.fs(172,73): (172,82) error FSHARP: The type 'Pages.Layout.Msg' does not match the type 'Pages.Hello.Layout.Msg' (code 1)
  Compilation failed
"""

    let result = ElmishLand.FableOutput.processOutput buildOutput buildOutput false

    match result.Errors with
    | [ "Page src/Pages/Hello/Page.fs has wrong layout reference. It uses 'Pages.Layout.Msg' but should use 'Pages.Hello.Layout.Msg'" ] -> ()
    | other -> failwithf "Expected correct error message. Got %A" other
