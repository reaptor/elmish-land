module ElmishLand.TemplateEngine

open System
open System.IO
open HandlebarsDotNet
open ElmishLand.Base

module String =
    let replace (oldValue: string) (newValue: string) (s: string) = s.Replace(oldValue, newValue)
    let split (separator: string) (s: string) = s.Split(separator)

let handlebars (src: string) model =
    let handlebars = Handlebars.Create()
    handlebars.Configuration.ThrowOnUnresolvedBindingExpression <- true
    handlebars.Compile(src).Invoke(model)

type Route = {
    Name: string
    Args: string
    Url: string
    UrlSegments: string array
    Query: string
}

type RouteData = { Routes: Route array }

let getSortedPageFiles projectDir =
    Directory.GetFiles(projectDir, "page.fs", EnumerationOptions(RecurseSubdirectories = true))
    |> Array.sortBy (fun x ->
        if x.EndsWith(Path.Combine("src", "Pages", "Page.fs")) then
            ""
        else
            x)

let fileToRoute projectDir (file: string) =
    let route =
        file[0 .. file.Length - 9]
        |> String.replace (Path.Combine(projectDir, "src")) ""
        |> String.replace "\\" "/"

    route[1..]
    |> String.split "/"
    |> Array.fold
        (fun (parts, args) part ->
            if part.StartsWith("{") && part.EndsWith("}") then
                $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts,
                $"%s{part[1..1].ToLower()}%s{part[2 .. part.Length - 2]}" :: args
            else
                $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts, args)
        ([], [])
    |> fun (parts, args) ->
        let duName = String.concat "_" (List.rev parts)

        let url = if route = "" then "/" else route

        let name =
            if duName.Contains("{") then $"``%s{duName}``"
            else if duName = "" then "Home"
            else duName

        let args =
            let argString =
                args
                |> List.rev
                |> List.map (fun arg -> $"%s{arg}: string")
                |> String.concat " * "

            if argString.Length = 0 then "" else $"%s{argString}"

        {
            Name = name
            Args = args
            Url = url
            UrlSegments = [| "urlSegments" |]
            Query = "query"
        }
    // (if route = "" then "/" else route),
    // (
    //     if duName.Contains("{") then $"``%s{duName}``"
    //     else if duName = "" then "Home"
    //     else duName
    // ),
    // List.rev args
    |> fun x ->
        printfn "%A" x
        x




let getRouteData projectDir =
    let pageFiles = getSortedPageFiles projectDir

    {
        Routes = pageFiles |> Array.map (fileToRoute projectDir)
    }

let processTemplate (routeData: RouteData) (src: string) = handlebars src routeData
