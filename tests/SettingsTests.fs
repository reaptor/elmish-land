module tests.SettingsTests

open System.IO
open System.Text
open ElmishLand.Base
open ElmishLand.Generate
open Runner
open TestProjectGeneration
open Xunit

[<Fact>]
let ``Default renderMethod is written to App.fs`` () =
    withNewProject (fun projectDir _ ->
        task {
            let workingDir = AbsoluteProjectDir.asString projectDir

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result

            let appFsContent =
                File.ReadAllText(Path.Combine(workingDir, ".elmish-land", "App", "App.fs"))

            Expects.containsSubstring ("""|> Program.withReactSynchronous "app" """.TrimEnd()) appFsContent
        })

[<Fact>]
let ``Changing renderMethod to batched writes it to App.fs`` () =
    withNewProject (fun projectDir _ ->
        task {
            let workingDir = AbsoluteProjectDir.asString projectDir

            let filepath = Path.Combine(workingDir, "elmish-land.json")
            let settingsContent = File.ReadAllText(filepath) |> StringBuilder

            settingsContent.Replace(""""renderMethod": "synchronous",""", $""""renderMethod": "batched",""")
            |> ignore

            settingsContent.Replace(
                """"renderTargetElementId": "app" """.TrimEnd(),
                """"renderTargetElementId": "myElement","""
            )
            |> ignore

            File.WriteAllText(filepath, settingsContent.ToString())

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result

            let appFsContent =
                File.ReadAllText(Path.Combine(workingDir, ".elmish-land", "App", "App.fs"))

            Expects.containsSubstring ("""|> Program.withReactBatched "myElement" """.TrimEnd()) appFsContent
        })
