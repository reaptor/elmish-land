module ElmishLand.AppError

open System
open ElmishLand.Base
open ElmishLand.Log

type AppError =
    | ProcessError of string
    | FsProjValidationError of string list
    | DotnetSdkNotFound
    | NodeNotFound

let handleAppResult (projectDir: AbsoluteProjectDir) onSuccess =
    let log = Log()

    function
    | Ok _ ->
        onSuccess ()
        0
    | Error e ->
        log.Info(welcomeTitle.Value)

        match e with
        | ProcessError(error) ->
            log.Error error
            -1
        | FsProjValidationError errors ->
            for error in errors do
                log.Error error

            -1
        | DotnetSdkNotFound ->
            log.Error
                $"""You need to install .NET Core SDK version %s{DotnetSdkVersion.asString minimumRequiredDotnetSdk} or above
https://dotnet.microsoft.com/
"""

            -1
        | NodeNotFound ->
            log.Error
                $"""You need to install Node.js version %s{minimumRequiredNode.ToString()} or above
https://nodejs.org/
"""

            -1
