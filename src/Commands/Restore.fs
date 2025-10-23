module ElmishLand.Restore

open System.IO
open ElmishLand.Effect
open Orsak
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Generate
open ElmishLand.Base

let successMessage () =
    let header = getCommandHeader "restored your project!"

    let content =
        """Run the following command to start the development server:

dotnet elmish-land server"""

    getFormattedCommandOutput header content

let restore workingDirectory absoluteProjectDir (promptAccept: AutoUpdateCode) =
    eff {
        let! log = Log().Get()

        do!
            withSpinner "Restoring your project..." (fun _ ->
                eff {
                    let! dotnetSdkVersion = getDotnetSdkVersion workingDirectory
                    log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

                    let settingsFiles =
                        Directory.EnumerateFiles(
                            AbsoluteProjectDir.asString absoluteProjectDir,
                            "elmish-land.json",
                            SearchOption.AllDirectories
                        )

                    if Seq.isEmpty settingsFiles then
                        return! Error AppError.ElmishLandProjectMissing
                    else
                        for settingsFile in settingsFiles do
                            let subAbsoluteProjectDir =
                                settingsFile
                                |> FilePath.fromString
                                |> FilePath.directoryPath
                                |> AbsoluteProjectDir

                            do! generate workingDirectory subAbsoluteProjectDir dotnetSdkVersion
                            do! validate subAbsoluteProjectDir promptAccept

                })

        log.Info(successMessage ())
    }
