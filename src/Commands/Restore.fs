module ElmishLand.Restore

open System.IO
open ElmishLand.Effect
open Orsak
open ElmishLand.Log
open ElmishLand.DotNetCli
open ElmishLand.FsProj
open ElmishLand.Generate
open ElmishLand.Base
open ElmishLand.Validation

let successMessage () =
    let header = getCommandHeader "restored your project!"

    let content =
        """Run the following command to start the development server:

dotnet elmish-land server"""

    getFormattedCommandOutput header content

let restore absoluteProjectDir =
    let stopSpinner = createSpinner "Restoring your project..."

    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getDotnetSdkVersion ()
        log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

        let settingsFiles =
            Directory.EnumerateFiles(
                AbsoluteProjectDir.asString absoluteProjectDir,
                "elmish-land.json",
                SearchOption.AllDirectories
            )

        if Seq.isEmpty settingsFiles then
            stopSpinner ()
            return! Error AppError.ElmishLandProjectMissing
        else
            for settingsFile in settingsFiles do
                let subAbsoluteProjectDir =
                    settingsFile
                    |> FilePath.fromString
                    |> FilePath.directoryPath
                    |> AbsoluteProjectDir

                do! generate subAbsoluteProjectDir dotnetSdkVersion
                do! validate subAbsoluteProjectDir
                
                // Validate page and layout function signatures
                let! pageErrors = validatePageFiles subAbsoluteProjectDir
                let! layoutErrors = validateLayoutFiles subAbsoluteProjectDir
                let allErrors = List.append pageErrors layoutErrors
                
                if not (List.isEmpty allErrors) then
                    let errorMessage = formatValidationErrors allErrors
                    return! Error (AppError.ValidationError errorMessage)

            stopSpinner ()
            log.Info(successMessage ())
    }
