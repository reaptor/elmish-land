module ElmishLand.Generate

open System.Text
open System.Text.Json
open Orsak
open ElmishLand.Base
open ElmishLand.Log
open ElmishLand.TemplateEngine
open ElmishLand.Resource

let settingsArrayToHtmlElements (name: string) close (arr: JsonElement array) =
    arr
    |> Array.fold
        (fun xs elem ->
            let sb = StringBuilder()
            sb.Append($"<%s{name} ") |> ignore

            for x in elem.EnumerateObject() do
                sb.Append($"""%s{x.Name}="%s{x.Value.GetString()}" """) |> ignore

            sb.Remove(sb.Length - 1, 1) |> ignore

            if close then
                sb.Append($"></%s{name}>") |> ignore
            else
                sb.Append(">") |> ignore

            sb.ToString() :: xs)
        []

let generate (projectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let settings = Settings.load projectDir
        log.Debug("Using settings: {}", settings)

        let writeResource = writeResource projectDir true

        do!
            writeResource
                "index.html.handlebars"
                [ ".elmish-land"; "index.html" ]
                (Some(
                    handlebars {|
                        Lang = settings.App.Html.Lang
                        Meta = settingsArrayToHtmlElements "meta" false settings.App.Html.Meta
                        Title = settings.App.Html.Title
                        Link = settingsArrayToHtmlElements "link" true settings.App.Html.Link
                        Script = settingsArrayToHtmlElements "script" true settings.App.Html.Script
                    |}
                ))

        let routeData = getRouteData projectDir
        do! generateRoutesAndApp projectDir routeData
    }
