module ElmishLand.Settings

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



type Settings = {
    View: {|
        Module: string
        Type: string
        TextElement: string
    |}
    ProjectReferences: string list
    DefaultLayoutTemplate: string option
    DefaultPageTemplate: string option
    RouteSettings: RouteParameters
}

let getSettings absoluteProjectDir =
    eff {
        let settingsPath =
            FilePath.appendParts [ "elmish-land.json" ] (AbsoluteProjectDir.asFilePath absoluteProjectDir)

        let! fs = FileSystem.get ()
        let settingsPathExists = fs.FilePathExists(settingsPath, false)

        do!
            if settingsPathExists then
                Ok()
            else
                Error ElmishLandProjectMissing

        let trimLeadingSpaces (s: string) =
            s.Split('\n')
            |> Array.fold
                (fun (state: string list, trimCount) s ->
                    if state.Length = 0 && s.Trim().Length = 0 then
                        state, trimCount
                    else
                        let trimCount =
                            match trimCount with
                            | Some trimCount' -> trimCount'
                            | None -> s.Length - s.TrimStart().Length

                        $"    %s{s[trimCount..]}" :: state, Some trimCount)
                ([], None)
            |> fun (xs, _) -> List.rev xs
            |> String.concat "\n"

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
            fs.GetFilesRecursive(AbsoluteProjectDir.asFilePath absoluteProjectDir, routeJson)
            |> Array.map (fun file ->
                fs.ReadAllText(file)
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

        let decoder =
            Decode.object (fun get -> {
                View =
                    get.Optional.Field
                        "view"
                        (Decode.object (fun get -> {|
                            Module = get.Optional.Field "module" Decode.string |> Option.defaultValue "Feliz"
                            Type = get.Optional.Field "type" Decode.string |> Option.defaultValue "ReactElement"
                            TextElement =
                                get.Optional.Field "textElement" Decode.string
                                |> Option.defaultValue "Html.text"
                        |}))
                    |> Option.defaultWith (fun () -> {|
                        Module = "Feliz"
                        Type =
                            get.Optional.Field "viewType" Decode.string
                            |> Option.defaultValue "ReactElement"
                        TextElement = "Html.text"
                    |})
                ProjectReferences =
                    get.Optional.Field "projectReferences" (Decode.list Decode.string)
                    |> Option.defaultValue []
                    |> List.map (fun x -> $"../../%s{x}")
                DefaultLayoutTemplate =
                    get.Optional.Field "defaultLayoutTemplate" Decode.string
                    |> Option.map trimLeadingSpaces
                DefaultPageTemplate =
                    get.Optional.Field "defaultPageTemplate" Decode.string
                    |> Option.map trimLeadingSpaces
                RouteSettings = RouteParameters(pageSettings)
            })

        return!
            fs.ReadAllText(settingsPath)
            |> Decode.fromString decoder
            |> Result.mapError InvalidSettings
    }
