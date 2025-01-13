module ElmishLand.TemplateEngine

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml
open ElmishLand.Effect
open ElmishLand.Log
open ElmishLand.Settings
open HandlebarsDotNet
open ElmishLand.Base
open Microsoft.FSharp.Collections
open Orsak
open ElmishLand.Resource

let reservedWords = [|
    "abstract"
    "and"
    "as"
    "assert"
    "base"
    "begin"
    "class"
    "default"
    "delegate"
    "do"
    "done"
    "downcast"
    "downto"
    "elif"
    "else"
    "end"
    "exception"
    "extern"
    "false"
    "finally"
    "fixed"
    "for"
    "fun"
    "function"
    "global"
    "if"
    "in"
    "inherit"
    "inline"
    "interface"
    "internal"
    "lazy"
    "let"
    "match"
    "member"
    "module"
    "mutable"
    "namespace"
    "new"
    "not"
    "null"
    "of"
    "open"
    "or"
    "override"
    "private"
    "public"
    "rec"
    "return"
    "select"
    "static"
    "struct"
    "then"
    "to"
    "true"
    "try"
    "type"
    "upcast"
    "use"
    "val"
    "void"
    "when"
    "while"
    "with"
    "yield"
    "const"
    "asr"
    "land"
    "lor"
    "lsl"
    "lsr"
    "lxor"
    "mod"
    "sig"
    "break"
    "checked"
    "component"
    "const"
    "constraint"
    "continue"
    "event"
    "external"
    "include"
    "mixin"
    "parallel"
    "process"
    "protected"
    "pure"
    "sealed"
    "tailcall"
    "trait"
    "virtual"
|]

let handlebars model (src: string) =
    let handlebars = Handlebars.Create()
    handlebars.Configuration.ThrowOnUnresolvedBindingExpression <- true
    handlebars.Configuration.NoEscape <- true

    try
        handlebars.Compile(src).Invoke(model)
    with ex ->
        raise (Exception($"Handlebars compilation failed.\n%s{src}\n%A{model}", ex))

type Route = {
    Name: string
    RouteName: string
    LayoutName: string
    LayoutModuleName: string
    MsgName: string
    ModuleName: string
    RecordDefinition: string
    RecordConstructor: string
    RecordPattern: string
    UrlUsage: string
    UrlPattern: string
    UrlPatternWhen: string
}

type Layout = {
    Name: string
    MsgName: string
    ModuleName: string
}

type TemplateData = {
    ViewModule: string
    ViewType: string
    RootModule: string
    Routes: Route array
    Layouts: Layout array
    RouteParamModules: string list
} with

    member this.ViewTypeIsReact = this.ViewType = "ReactElement"

let getSortedPageFiles absoluteProjectDir =
    let pageFilesDir =
        absoluteProjectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts [ "src"; "Pages" ]
        |> FilePath.asString

    if not (Directory.Exists pageFilesDir) then
        Error AppError.PagesDirectoryMissing
    else
        Directory.GetFiles(pageFilesDir, "Page.fs", EnumerationOptions(RecurseSubdirectories = true))
        |> Array.map FilePath.fromString
        |> Array.sortByDescending (fun x ->
            if x |> FilePath.endsWithParts [ "src"; "Pages"; "Page.fs" ] then
                0
            else
                x |> FilePath.parts |> Array.length)
        |> Ok

let getSortedLayoutFiles absoluteProjectDir =
    let layoutFilesDir =
        absoluteProjectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts [ "src"; "Pages" ]
        |> FilePath.asString

    if not (Directory.Exists layoutFilesDir) then
        Ok Array.empty
    else
        Directory.GetFiles(layoutFilesDir, "Layout.fs", EnumerationOptions(RecurseSubdirectories = true))
        |> Array.map FilePath.fromString
        |> Array.sortByDescending (fun x ->
            if x |> FilePath.endsWithParts [ "src"; "Pages"; "Page.fs" ] then
                0
            else
                x |> FilePath.parts |> Array.length)
        |> Ok

let wrapWithTicksIfNeeded (s: string) =
    if Regex.IsMatch(s, "^[0-9a-zA-Z_]+$") && not (Array.contains s reservedWords) then
        s
    else
        $"``%s{s}``"

let toPascalCase (s: string) = $"%s{s[0..0].ToUpper()}%s{s[1..]}"
let toCamelCase (s: string) = $"%s{s[0..0].ToLower()}%s{s[1..]}"

let camelToKebabCase (s: string) =
    s.ToCharArray()
    |> Array.map (fun c ->
        if Char.IsUpper c then
            $"-%c{Char.ToLowerInvariant c}"
        else
            $"%c{c}")
    |> String.concat ""

let routeParamTypes =
    Map [
        "Guid", ("parseGuid", "formatGuid")
        "int", ("parseInt", "formatInt")
        "int64", ("parseInt64", "formatInt64")
        "bool", ("parseBool", "formatBool")
        "float", ("parseFloat", "formatFloat")
        "Decimal", ("parseDecimal", "formatDecimal")
    ]

let fileToLayout projectName absoluteProjectDir (FilePath file) =
    let layout =
        file[0 .. file.Length - "Layout.fs".Length - 2]
        |> String.replace
            (absoluteProjectDir
             |> AbsoluteProjectDir.asFilePath
             |> FilePath.appendParts [ "src"; "Pages" ]
             |> FilePath.asString)
            ""
        |> String.replace "\\" "/"

    layout[1..]
    |> String.split "/"
    |> Array.fold
        (fun parts part ->
            if part.StartsWith("_") then
                (toPascalCase part).TrimStart('_') :: parts
            else
                toPascalCase part :: parts)
        []
    |> fun parts ->
        let name = parts |> List.rev |> List.map toPascalCase |> String.concat "_"
        let name = if String.IsNullOrEmpty name then "Main" else name

        let moduleNamePart =
            if parts.Length = 0 then
                ""
            else
                parts
                |> List.rev
                |> List.map (toPascalCase >> wrapWithTicksIfNeeded)
                |> String.concat "."
                |> fun x -> $".%s{x}"

        {
            Name = wrapWithTicksIfNeeded name
            MsgName = wrapWithTicksIfNeeded $"%s{name}Msg"
            ModuleName = $"%s{projectName |> ProjectName.asString}.Pages%s{moduleNamePart}.Layout"
        }

let fileToRoute projectName absoluteProjectDir (RouteParameters pageSettings) (FilePath file) =
    let route =
        file[0 .. file.Length - "Page.fs".Length - 2]
        |> String.replace
            (absoluteProjectDir
             |> AbsoluteProjectDir.asFilePath
             |> FilePath.appendParts [ "src"; "Pages" ]
             |> FilePath.asString)
            ""
        |> String.replace "\\" "/"

    route[1..]
    |> String.split "/"
    |> Array.fold
        (fun (parts, args) part ->
            if part.StartsWith("_") then
                (toPascalCase part).TrimStart('_') :: parts, (toCamelCase part).TrimStart('_') :: args
            else
                toPascalCase part :: parts, args)
        ([], [])
    |> fun (parts, args) ->
        eff {
            let args = List.rev args

            let name = parts |> List.rev |> List.map toPascalCase |> String.concat "_"
            let name = if String.IsNullOrEmpty name then "Home" else name

            let moduleNamePart =
                if parts.Length = 0 then
                    ""
                else
                    parts
                    |> List.rev
                    |> List.map (toPascalCase >> wrapWithTicksIfNeeded)
                    |> String.concat "."
                    |> fun x -> $".%s{x}"

            let! layout =
                let rec findLayoutRecurse (dir: string) =
                    eff {
                        let dirInfo = DirectoryInfo(dir)
                        let y = DirectoryInfo(AbsoluteProjectDir.asString absoluteProjectDir).FullName

                        if dirInfo.FullName = y then
                            return! Error AppError.MissingMainLayout
                        else
                            let layoutFile = FilePath.fromString <| Path.Combine(dirInfo.FullName, "Layout.fs")

                            let! layoutExists = FileSystem.filePathExists layoutFile

                            if layoutExists then
                                return! Ok <| fileToLayout projectName absoluteProjectDir layoutFile
                            else
                                return! findLayoutRecurse dirInfo.Parent.FullName
                    }

                findLayoutRecurse (Path.GetDirectoryName(file))

            let queryParameters =
                pageSettings
                |> List.sortByDescending (fun (path, _) -> path.Split("/")) // Ensure the most deep folders are sorted first
                |> List.fold
                    (fun (state: RouteQueryParameter list) (route', (_, queryParameters)) ->
                        // if route' = "/" ensures that root level route.json file are included when getting query parameters.
                        if route' = "/" || route.StartsWith(route') then
                            (queryParameters
                             |> List.filter (fun x ->
                                 // Ensures that we only take query params from the deepest route.json file.
                                 // Eg. deeper files will override previous.
                                 state |> List.exists (fun y -> y.Name <> x.Name) |> not))
                            @ state
                        else
                            state)
                    []

            let recordPattern =
                let argString =
                    args
                    |> List.map (fun arg ->
                        $"%s{arg |> toPascalCase |> wrapWithTicksIfNeeded} = %s{arg |> toCamelCase |> wrapWithTicksIfNeeded}")
                    |> fun x ->
                        List.append
                            x
                            (queryParameters
                             |> List.map (fun qp ->
                                 $"%s{qp.Name |> toPascalCase |> wrapWithTicksIfNeeded} = %s{qp.Name |> toCamelCase |> wrapWithTicksIfNeeded}"))
                    |> String.concat "; "

                if argString.Length = 0 then
                    "()"
                else
                    $"{{ %s{argString} }}"

            let pathParameters =
                pageSettings
                |> List.choose (fun (route', (pathParameter, _)) ->
                    pathParameter
                    |> Option.bind (fun pathParameter' ->
                        if route.StartsWith(route') then
                            route'.Split("/")
                            |> Array.tryLast
                            |> Option.map (fun pathParamName ->
                                pathParamName[1..] (* Remove leading underscore *) , pathParameter')
                        else
                            None))
                |> Map

            let getPathParameter arg =
                pathParameters
                |> Map.tryFind arg
                |> Option.defaultValue {
                    Module = "System"
                    Type = "string"
                    Parse = None
                    Format = None
                }

            let getPathParamParser (parameter: RoutePathParameter) =
                parameter.Parse
                |> Option.orElseWith (fun () -> routeParamTypes |> Map.tryFind parameter.Type |> Option.map fst)

            let getPathParamFormatter (parameter: RoutePathParameter) =
                parameter.Format
                |> Option.orElseWith (fun () -> routeParamTypes |> Map.tryFind parameter.Type |> Option.map snd)

            let getQueryParamParser (parameter: RouteQueryParameter) =
                parameter.Parse
                |> Option.orElseWith (fun () -> routeParamTypes |> Map.tryFind parameter.Type |> Option.map fst)

            let getQueryParamFormatter (parameter: RouteQueryParameter) =
                parameter.Format
                |> Option.orElseWith (fun () -> routeParamTypes |> Map.tryFind parameter.Type |> Option.map snd)

            let recordDefinition =
                args
                |> List.map (fun arg ->
                    let parameter = getPathParameter arg
                    $"%s{arg |> toPascalCase |> wrapWithTicksIfNeeded}: %s{parameter.Type}")
                |> fun xs ->
                    List.append
                        xs
                        (queryParameters
                         |> List.map (fun x ->
                             let optional = if x.Required then "" else "option"
                             $"%s{x.Name |> toPascalCase |> wrapWithTicksIfNeeded}: %s{x.Type} %s{optional}"))
                |> String.concat "; "
                |> fun x ->
                    if String.IsNullOrWhiteSpace x then
                        "unit"
                    else
                        $"{{ %s{x} }}"

            let recordConstructor =
                let argString =
                    args
                    |> List.map (fun arg ->
                        let parameter = getPathParameter arg
                        let arg = arg |> toCamelCase |> wrapWithTicksIfNeeded

                        let value =
                            getPathParamParser parameter
                            |> Option.map (fun x -> $"(%s{x} %s{arg}).Value")
                            |> Option.defaultValue arg

                        $"%s{arg |> toPascalCase |> wrapWithTicksIfNeeded} = %s{value}")
                    |> fun xs ->
                        List.append
                            xs
                            (queryParameters
                             |> List.map (fun x ->
                                 let parser = x |> getQueryParamParser |> Option.defaultValue "Some"
                                 let getter = if x.Required then "getQuery" else "tryGetQuery"
                                 $"%s{x.Name |> toPascalCase |> wrapWithTicksIfNeeded} = %s{getter} \"%s{x.Name}\" %s{parser} q"))
                    |> String.concat "; "

                if argString.Length = 0 then
                    "()"
                else
                    $"{{ %s{argString}; }}"

            let urlUsage =
                route
                |> String.split "/"
                |> Array.map (fun x ->
                    if x.StartsWith("_") then
                        let arg = x.TrimStart('_')

                        let parameter = getPathParameter arg
                        let formatnFn = getPathParamFormatter parameter |> Option.defaultValue ""

                        $"%s{formatnFn} %s{arg |> toCamelCase |> wrapWithTicksIfNeeded}"
                    else
                        $"\"%s{wrapWithTicksIfNeeded x |> toCamelCase |> camelToKebabCase}\"")
                |> fun xs -> if xs.Length = 0 then [| "\"\"" |] else xs
                |> fun x ->
                    Array.append
                        x
                        (queryParameters
                         |> List.toArray
                         |> Array.map (fun qp ->
                             let format = getQueryParamFormatter qp |> Option.defaultValue ""
                             let name = $"%s{wrapWithTicksIfNeeded qp.Name |> toCamelCase}"

                             if qp.Required then
                                 $"\"%s{name}\", %s{format} %s{name}"
                             else
                                 $"[ match %s{name} with Some x -> \"%s{name}\", %s{format} x | None -> () ]")
                         |> String.concat "@"
                         |> fun x -> if String.IsNullOrEmpty x then [||] else [| x |])
                |> String.concat ", "

            let urlPattern =
                route
                |> String.split "/"
                |> Array.map (fun x -> x.TrimStart('_') |> toCamelCase |> wrapWithTicksIfNeeded)
                |> String.concat "; "
                |> fun pattern ->
                    if pattern.Length > 0 then
                        $"[ %s{pattern}; Query q ]"
                    else
                        "[ Query q ]"

            let urlPatternWhen =
                route
                |> String.split "/"
                |> Array.choose (fun arg ->
                    if arg.StartsWith "_" then
                        let arg = arg.TrimStart('_')
                        let parameter = getPathParameter arg

                        getPathParamParser parameter
                        |> Option.map (fun p -> $"(%s{p} %s{arg |> toCamelCase |> wrapWithTicksIfNeeded}).IsSome")
                    else
                        Some
                            $"eq %s{arg |> toCamelCase |> wrapWithTicksIfNeeded} \"%s{arg |> toCamelCase |> camelToKebabCase}\"")
                |> fun xs ->
                    Array.append
                        xs
                        (queryParameters
                         |> List.toArray
                         |> Array.choose (fun arg ->
                             if arg.Required then
                                 let parser = arg.Parse |> Option.defaultValue "Some"
                                 Some $"containsQuery \"%s{arg.Name}\" %s{parser} q"
                             else
                                 None)

                        )

                |> String.concat " && "

            return {
                Name = wrapWithTicksIfNeeded name
                RouteName = wrapWithTicksIfNeeded $"%s{name}Route"
                LayoutName = layout.Name
                LayoutModuleName = layout.ModuleName
                MsgName = wrapWithTicksIfNeeded $"%s{name}Msg"
                ModuleName = $"%s{projectName |> ProjectName.asString}.Pages%s{moduleNamePart}.Page"
                RecordDefinition = recordDefinition
                RecordConstructor = recordConstructor
                RecordPattern = recordPattern
                UrlUsage = urlUsage
                UrlPattern = urlPattern
                UrlPatternWhen = urlPatternWhen
            }
        }

let getTemplateData projectName absoluteProjectDir =
    eff {
        let! pageFiles = getSortedPageFiles absoluteProjectDir
        let! layoutFiles = getSortedLayoutFiles absoluteProjectDir
        let! settings = getSettings absoluteProjectDir

        // Ignore modules that already exists in the template file.
        // We don't wan't those types to shadowing the users own types.
        let shouldIgnoreModule moduleName =
            [ "System"; "Feliz.Router" ] |> List.contains moduleName |> not

        let routeParamModules =
            (RouteParameters.value settings.RouteSettings)
            |> List.collect (fun (_, (maybeRouteParam, queryParams)) -> [
                match maybeRouteParam with
                | Some routeParam when shouldIgnoreModule routeParam.Module -> routeParam.Module
                | _ -> ()
                yield!
                    queryParams
                    |> List.choose (fun x -> if shouldIgnoreModule x.Module then Some x.Module else None)
            ])
            |> List.distinct

        let! routes =
            pageFiles
            |> Array.fold
                (fun state filePath ->
                    eff {
                        let! routes = state
                        let! route = fileToRoute projectName absoluteProjectDir settings.RouteSettings filePath
                        return route :: routes
                    })
                (eff { return! Ok [] })
            |> Effect.map List.toArray

        return {
            ViewModule = settings.View.Module
            ViewType = settings.View.Type
            RootModule = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded
            Routes = routes
            Layouts = layoutFiles |> Array.map (fileToLayout projectName absoluteProjectDir)
            RouteParamModules = routeParamModules
        }
    }

let generateFiles workingDir (templateData: TemplateData) =
    let writeResource = writeResource workingDir true

    eff {
        do! writeResource "Routes.template" [ ".elmish-land"; "Base"; "Routes.fs" ] (Some(handlebars templateData))
        do! writeResource "Command.fs.template" [ ".elmish-land"; "Base"; "Command.fs" ] (Some(handlebars templateData))
        do! writeResource "Page.fs.template" [ ".elmish-land"; "Base"; "Page.fs" ] (Some(handlebars templateData))
        do! writeResource "Layout.fs.template" [ ".elmish-land"; "Base"; "Layout.fs" ] (Some(handlebars templateData))
        do! writeResource "App.template" [ ".elmish-land"; "App"; "App.fs" ] (Some(handlebars templateData))
    }
