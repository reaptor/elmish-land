#!/usr/bin/env dotnet fsi

#load "../TestUtils.fsx"
open TestUtils

printStep "Testing nested layout detection fix for issue #21..."

// Clean up and create test directory
cleanupAndCreateTestDir "TestProject"

printStep "Running elmish-land init..."
runElmishLandCommand "init --verbose" |> ignore

printStep "Adding nested layout /PageWithNestedLayout..."
runElmishLandCommand "add layout /PageWithNestedLayout --verbose" |> ignore

printStep "Adding layout compile include to project file..."
insertIntoFirstItemGroup "TestProject.fsproj" "src/Pages/PageWithNestedLayout/Layout.fs"

printStep "Adding page /PageWithNestedLayout..."
runElmishLandCommand "add page /PageWithNestedLayout --verbose" |> ignore

printStep "Adding page compile include to project file..."
insertIntoFirstItemGroup "TestProject.fsproj" "src/Pages/PageWithNestedLayout/Page.fs"

printStep "Building project to verify it compiles..."
runElmishLandCommand "build --verbose" |> ignore

printStep "Verifying main page uses correct layout message..."
verifyFileContains "src/Pages/Page.fs" "| LayoutMsg of Layout.Msg" "Main page correctly references Layout.Msg"

printStep "Verifying nested page uses correct layout message..."

verifyFileContains
    "src/Pages/PageWithNestedLayout/Page.fs"
    "| LayoutMsg of PageWithNestedLayout.Layout.Msg"
    "Nested page correctly references PageWithNestedLayout.Layout.Msg"

printStep "Verifying layout files were created..."
let expectedLayoutFiles = [ "src/Pages/Layout.fs"; "src/Pages/PageWithNestedLayout/Layout.fs" ]

verifyFilesExist expectedLayoutFiles "Layout"

printSuccess "All tests passed! Issue #21 nested layout detection fix works correctly."
