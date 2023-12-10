module ElmishLand.AppError

open System
open System.IO
open ElmishLand.Base

type AppError =
    | ProcessError of string
    | FsProjValidationError of string list
    | DotnetSdkNotFound
    | NodeNotFound

let private printError (text: string) =
    Console.ForegroundColor <- ConsoleColor.Red
    Console.Error.WriteLine(indent text)
    Console.ResetColor()

let handleAppResult (projectDir: AbsoluteProjectDir) onSuccess =
    function
    | Ok _ ->
        onSuccess ()
        0
    | Error e ->
        Console.WriteLine(indent welcomeTitle.Value)

        match e with
        | ProcessError(error) ->
            printError error
            -1
        | FsProjValidationError errors ->
            for error in errors do
                printError error

            -1
        | DotnetSdkNotFound ->
            printError
                $"""You need to install .NET Core SDK version %s{DotnetSdkVersion.asString minimumRequiredDotnetSdk} or above
https://dotnet.microsoft.com/
"""

            -1
        | NodeNotFound ->
            printError
                $"""You need to install Node.js version %s{minimumRequiredNode.ToString()} or above
https://nodejs.org/
"""

            -1
