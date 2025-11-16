module ElmishLand.Settings

open System
open System.IO
open ElmishLand.Base
open ElmishLand.Effect
open Orsak
open Thoth.Json.Net
open ElmishLand.AppError

type RoutePathParameter = {
    Module: string
    Type: string
    Parse: string option
    Format: string option
}

type RouteQueryParameter = {
    Name: string
    Module: string
    Type: string
    Parse: string option
    Format: string option
    Required: bool
}

type LayoutName = | LayoutName of string

module LayoutName =
    let asString (LayoutName x) = x

type RouteParameters = | RouteParameters of List<string * (RoutePathParameter option * RouteQueryParameter list)>

module RouteParameters =
    let value (RouteParameters x) = x

type RenderMethod =
    /// Uses `requestAnimationFrame` to batch updates to prevent drops in frame rate.
    /// NOTE: This may have unexpected effects in React controlled inputs, see https://github.com/elmish/react/issues/12
    | Batched
    /// New renders are triggered immediately after an update.
    | Synchronous

module RenderMethod =
    let tryParse ok err =
        function
        | "batched" -> ok Batched
        | "synchronous" -> ok Synchronous
        | other ->
            err
                $"program:renderMethod '%s{other}' is not supported. Allowed values are: batched, synchronous and hydrate"

    let asElmishReactFunction =
        function
        | Batched -> "withReactBatched"
        | Synchronous -> "withReactSynchronous"

type ViewSettings = {
    Module: string
    Type: string
    TextElement: string
}

type RouteMode =
    | Path
    | Hash

module RouteMode =
    let tryParse ok err =
        function
        | "path" -> ok Path
        | "hash" -> ok Hash
        | other -> err $"program:pathMode '%s{other}' is not supported. Allowed values are: path and hash"

    let asAppConfigString =
        function
        | Path -> "path"
        | Hash -> "hash"

type ProgramSettings = {
    RenderMethod: RenderMethod
    RenderTargetElementId: string
    RouteMode: RouteMode
}

type Settings = {
    View: ViewSettings
    ProjectReferences: string list
    RouteSettings: RouteParameters
    ServerCommand: (string * string array) option
    Program: ProgramSettings
}

let elmishLandSettingsDecoder pageSettings =
    Decode.object (fun get -> {
        View =
            get.Optional.Field
                "view"
                (Decode.object (fun get -> {
                    Module = get.Optional.Field "module" Decode.string |> Option.defaultValue "Feliz"
                    Type = get.Optional.Field "type" Decode.string |> Option.defaultValue "ReactElement"
                    TextElement =
                        get.Optional.Field "textElement" Decode.string
                        |> Option.defaultValue "Html.text"
                }))
            |> Option.defaultWith (fun () -> {
                Module = "Feliz"
                Type =
                    get.Optional.Field "viewType" Decode.string
                    |> Option.defaultValue "ReactElement"
                TextElement = "Html.text"
            })
        ProjectReferences =
            get.Optional.Field "projectReferences" (Decode.list Decode.string)
            |> Option.defaultValue []
            |> List.map (fun x -> $"../../%s{x}")
        RouteSettings = RouteParameters(pageSettings)
        ServerCommand =
            let decodeServerCommand =
                Decode.string
                |> Decode.andThen (fun serverCommand ->
                    match serverCommand.Split(" ", StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
                    | [] -> Decode.fail "Exe command is missing"
                    | [ command ] -> Decode.succeed (command, [||])
                    | command :: args -> Decode.succeed (command, List.toArray args))

            get.Optional.Field "serverCommand" decodeServerCommand
        Program =
            get.Optional.Field
                "program"
                (Decode.object (fun get ->
                    let decodeRenderMethod =
                        Decode.string
                        |> Decode.andThen (RenderMethod.tryParse Decode.succeed Decode.fail)

                    let decodePathMode =
                        Decode.string |> Decode.andThen (RouteMode.tryParse Decode.succeed Decode.fail)

                    {
                        RenderMethod =
                            get.Optional.Field "renderMethod" decodeRenderMethod
                            |> Option.defaultValue Synchronous
                        RenderTargetElementId =
                            get.Optional.Field "renderTargetElementId" Decode.string
                            |> Option.defaultValue "app"
                        RouteMode =
                            get.Optional.Field "routeMode" decodePathMode
                            |> Option.defaultValue RouteMode.Hash
                    }))
            |> Option.defaultWith (fun () -> {
                RenderMethod = Synchronous
                RenderTargetElementId = "app"
                RouteMode = Hash
            })
    })

let getSettings absoluteProjectDir =
    eff {
        let settingsPath =
            FilePath.appendParts [ "elmish-land.json" ] (AbsoluteProjectDir.asFilePath absoluteProjectDir)

        let settingsPathExists = FilePath.exists settingsPath

        do!
            if settingsPathExists then
                Ok()
            else
                Error ElmishLandProjectMissing

        let paramsDecoder =
            Decode.object (fun get ->
                get.Optional.Field
                    "pathParameter"
                    (Decode.object (fun get -> {
                        Module = get.Required.Field "module" Decode.string
                        Type = get.Required.Field "type" Decode.string
                        Parse = get.Optional.Field "parse" Decode.string
                        Format = get.Optional.Field "format" Decode.string
                    })),
                get.Optional.Field
                    "queryParameters"
                    (Decode.list (
                        Decode.object (fun get -> {
                            Name = get.Required.Field "name" Decode.string
                            Module = get.Required.Field "module" Decode.string
                            Type = get.Optional.Field "type" Decode.string |> Option.defaultValue "string"
                            Parse = get.Optional.Field "parse" Decode.string
                            Format = get.Optional.Field "format" Decode.string
                            Required = get.Optional.Field "required" Decode.bool |> Option.defaultValue false
                        })
                    ))
                |> Option.toList
                |> List.collect id)

        let routeJson = "route.json"

        let! pageSettings =
            FilePath.getFilesRecursive (AbsoluteProjectDir.asFilePath absoluteProjectDir) routeJson
            |> Array.map (fun file ->
                FilePath.readAllText file
                |> Decode.fromString paramsDecoder
                |> Result.mapError InvalidSettings
                |> Result.map (fun x ->
                    file
                    |> fun (FilePath s) ->
                        s
                            .Replace($"%s{AbsoluteProjectDir.asString absoluteProjectDir}/src/Pages/", "")
                            .Replace($"/%s{routeJson}", "")
                            .Replace($"%s{routeJson}", "")
                        |> fun s -> $"/%s{s}"
                    , x))
            |> Array.toList
            |> Result.sequence



        return!
            FilePath.readAllText settingsPath
            |> Decode.fromString (elmishLandSettingsDecoder pageSettings)
            |> Result.mapError InvalidSettings
    }
