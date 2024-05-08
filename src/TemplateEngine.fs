module ElmishLand.TemplateEngine

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml
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
    ViewType: string
    RootModule: string
    Routes: Route array
    Layouts: Layout array
} with

    member this.ViewTypeIsReact = this.ViewType = "Feliz.ReactElement"

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
            if x |> FilePath.endsWithParts [ "src"; "Pages"; "Home"; "Page.fs" ] then
                0
            else
                x |> FilePath.parts |> Array.length)
        |> Ok

let getSortedLayoutFiles absoluteProjectDir =
    let layoutFilesDir =
        absoluteProjectDir
        |> AbsoluteProjectDir.asFilePath
        |> FilePath.appendParts [ "src"; "Layouts" ]
        |> FilePath.asString

    if not (Directory.Exists layoutFilesDir) then
        Ok Array.empty
    else
        Directory.GetFiles(layoutFilesDir, "Layout.fs", EnumerationOptions(RecurseSubdirectories = true))
        |> Array.map FilePath.fromString
        |> Array.sortByDescending (fun x ->
            if x |> FilePath.endsWithParts [ "src"; "Pages"; "Home"; "Page.fs" ] then
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

let fileToRoute projectName absoluteProjectDir (RouteParameters routeParameters) (FilePath file) =
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
        let args = List.rev args

        let name =
            parts
            |> List.rev
            |> List.map toPascalCase
            |> String.concat "_"
            |> fun name -> if name = "" then "Home" else name

        let moduleNamePart =
            parts
            |> List.rev
            |> List.map (toPascalCase >> wrapWithTicksIfNeeded)
            |> String.concat "."
            |> fun name -> if name = "" then "Home" else name

        let queryParameters =
            routeParameters
            |> List.tryPick (fun (route', (_, queryParameters)) ->
                if route.StartsWith(route') then
                    Some queryParameters
                else
                    None)
            |> Option.toList
            |> List.collect id

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
            routeParameters
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
            if route = "/Home" then "/" else route
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
                         let name = $"%s{wrapWithTicksIfNeeded qp.Name |> toCamelCase |> camelToKebabCase}"

                         if qp.Required then
                             $"\"%s{name}\", %s{format} %s{name}"
                         else
                             $"match %s{name} with Some x -> \"%s{name}\", %s{format} x | None -> ()")
                     |> String.concat ";"
                     |> fun x -> if String.IsNullOrEmpty x then [||] else [| $"[ %s{x} ]" |])
            |> String.concat ", "

        let urlPattern =
            if route = "/Home" then "/" else route
            |> String.split "/"
            |> Array.map (fun x -> x.TrimStart('_') |> toCamelCase |> wrapWithTicksIfNeeded)
            |> String.concat "; "
            |> fun pattern ->
                if pattern.Length > 0 then
                    $"[ %s{pattern}; Query q ]"
                else
                    "[ Query q ]"

        let urlPatternWhen =
            (if route = "/Home" then "/" else route)
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

        {
            Name = wrapWithTicksIfNeeded name
            RouteName = wrapWithTicksIfNeeded $"%s{name}Route"
            MsgName = wrapWithTicksIfNeeded $"%s{name}Msg"
            ModuleName = $"%s{projectName |> ProjectName.asString}.Pages.%s{moduleNamePart}.Page"
            RecordDefinition = recordDefinition
            RecordConstructor = recordConstructor
            RecordPattern = recordPattern
            UrlUsage = urlUsage
            UrlPattern = urlPattern
            UrlPatternWhen = urlPatternWhen
        }

let fileToLayout projectName absoluteProjectDir (FilePath file) =
    let layout =
        file[0 .. file.Length - "Layout.fs".Length - 2]
        |> String.replace
            (absoluteProjectDir
             |> AbsoluteProjectDir.asFilePath
             |> FilePath.appendParts [ "src"; "Layouts" ]
             |> FilePath.asString)
            ""
        |> String.replace "\\" "/"

    layout[1..]
    |> String.split "/"
    |> Array.fold
        (fun (parts) part ->
            if part.StartsWith("_") then
                (toPascalCase part).TrimStart('_') :: parts
            else
                toPascalCase part :: parts)
        ([])
    |> fun parts ->
        let name = parts |> List.rev |> List.map toPascalCase |> String.concat ""

        {
            Name = wrapWithTicksIfNeeded name
            MsgName = wrapWithTicksIfNeeded $"%s{name}Msg"
            ModuleName =
                $"%s{projectName |> ProjectName.asString |> wrapWithTicksIfNeeded}.Layouts.%s{wrapWithTicksIfNeeded name}.Layout"
        }

let getTemplateData projectName absoluteProjectDir =
    eff {
        let! pageFiles = getSortedPageFiles absoluteProjectDir
        let! layoutFiles = getSortedLayoutFiles absoluteProjectDir
        let! settings = getSettings absoluteProjectDir

        return {
            ViewType = settings.ViewType
            RootModule = projectName |> ProjectName.asString |> wrapWithTicksIfNeeded
            Routes =
                pageFiles
                |> Array.map (fun filePath ->
                    fileToRoute projectName absoluteProjectDir settings.RouteSettings filePath)
            Layouts = layoutFiles |> Array.map (fileToLayout projectName absoluteProjectDir)
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
