module tests.SharedModuleTests

open System.IO
open ElmishLand.DotNetCli
open ElmishLand.Generate
open Runner
open Xunit
open Orsak
open ElmishLand.Base
open TestProjectGeneration

[<Fact>]
let ``Init function in Shared can handle promises`` () =
    withNewProject (fun absoluteProjectDir _ ->
        task {
            let folder = AbsoluteProjectDir.asString absoluteProjectDir

            let moduleName = Path.GetFileName(folder)

            let sharedFileContent =
                (sprintf
                    """
module %s.Shared

open System
open ElmishLand

type SharedModel = { stuff: int }

type SharedMsg =
    | BlahReceived of string

let init () =
    { stuff = 0 }
    , Command.ofPromise (fun _ -> promise { return "blah" }) () BlahReceived

let update (msg: SharedMsg) (model: SharedModel) =
    match msg with
    | BlahReceived _s ->
        model, Command.none

let subscriptions _model : (string list * ((SharedMsg -> unit) -> IDisposable)) list = []

"""
                    moduleName)

            File.WriteAllText(Path.Combine(folder, "src", "Shared.fs"), sharedFileContent)

            do!
                eff {
                    let! dotnetSdkVersion = getDotnetSdkVersion (FilePath.fromString folder)
                    do! generate (FilePath.fromString folder) absoluteProjectDir dotnetSdkVersion
                    let appProjDir = (FilePath.fromString (Path.Combine(folder, ".elmish-land", "App")))
                    do! expectProjectTypeChecks (AbsoluteProjectDir.create appProjDir [])
                }
                |> Expects.effectOk runEff
        })
