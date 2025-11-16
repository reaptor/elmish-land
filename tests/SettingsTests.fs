module tests.SettingsTests

open System.IO
open System.Text
open ElmishLand.Base
open ElmishLand.Generate
open Runner
open TestProjectGeneration
open Xunit

type AppConfig() =
    static member Generate(?renderMethod, ?renderTargetElementId, ?routeMode) =
        $$"""{
      "$schema": "https://elmish.land/schemas/v1.1/elmish-land.schema.json",
      "program": {
        "renderMethod": "%%s{{defaultArg renderMethod "synchronous"}}",
        "renderTargetElementId": "%%s{{defaultArg renderTargetElementId "app"}}",
        "routeMode": "%%s{{defaultArg routeMode "hash"}}"
      },
      "view": {
        "module": "Feliz",
        "type": "ReactElement",
        "textElement": "Html.text"
      },
      "projectReferences": []
    }
    """

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

            File.WriteAllText(
                filepath,
                AppConfig.Generate(renderMethod = "batched", renderTargetElementId = "myElement")
            )

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result

            let appFsContent =
                File.ReadAllText(Path.Combine(workingDir, ".elmish-land", "App", "App.fs"))

            Expects.containsSubstring ("""|> Program.withReactBatched "myElement" """.TrimEnd()) appFsContent
        })

[<Fact>]
let ``Default routeMode is "hash"`` () =
    withNewProject (fun projectDir _ ->
        task {
            let workingDir = AbsoluteProjectDir.asString projectDir

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result
            Expects.routeModeIsHash workingDir
        })

[<Fact>]
let ``Missing routeMode uses "hash"`` () =
    withNewProject (fun projectDir _ ->
        task {
            let workingDir = AbsoluteProjectDir.asString projectDir

            let filepath = Path.Combine(workingDir, "elmish-land.json")
            File.WriteAllText(filepath, "{}")

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result
            Expects.routeModeIsHash workingDir
        })

[<Fact>]
let ``Changing routeMode to "path" generates correct App.fs, Routes.fs and Command.fs`` () =
    withNewProject (fun projectDir _ ->
        task {
            let workingDir = AbsoluteProjectDir.asString projectDir

            let filepath = Path.Combine(workingDir, "elmish-land.json")
            File.WriteAllText(filepath, AppConfig.Generate(routeMode = "path"))

            let! result, logs =
                generate (AbsoluteProjectDir.asFilePath projectDir) projectDir dotnetSdkVersion
                |> runEff

            Expects.ok logs result
            Expects.routeModeIsPath workingDir
        })
