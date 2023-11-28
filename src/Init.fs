module ElmishLand.Init

open System
open System.IO
open System.Reflection
open ElmishLand.Base
open ElmishLand.TemplateEngine

let init (projectName: string) =
    let cp src dst replace =
        let templatesDir =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "src", "templates")

        let dstPath =
            Path.Combine(Environment.CurrentDirectory, projectName, Path.Combine(List.toArray dst))

        let dstDir = Path.GetDirectoryName(dstPath)

        if not (Directory.Exists dstDir) then
            Directory.CreateDirectory(dstDir) |> ignore

        File.Copy(Path.Combine(templatesDir, Path.Combine(List.toArray src)), dstPath)

        match replace with
        | Some f -> File.ReadAllText(dstPath) |> f |> (fun x -> File.WriteAllText(dstPath, x))
        | None -> ()

    try
        let dstPath = Path.Combine(Environment.CurrentDirectory, projectName)

        if Directory.Exists(dstPath) then
            Directory.Delete(dstPath, true)

        let cpSame fileName replace = cp fileName fileName replace
        cp [ "PROJECT_NAME.fsproj" ] [ $"%s{projectName}.fsproj" ] None
        cpSame [ "global.json" ] None
        cpSame [ "index.html" ] None
        cpSame [ "package.json" ] (Some(fun x -> x.Replace("{{PROJECT_NAME}}", projectName)))
        cpSame [ "package-lock.json" ] None
        cp [ "dotnet-tools.json" ] [ ".config"; "dotnet-tools.json" ] None
        // cp [ "Routes.fs" ] [ "src"; "Routes.fs" ] None
        cp [ "Shared.fs" ] [ "src"; "Shared.fs" ] None
        // cp [ "App.template" ] [ "src"; "App.fs" ] None
        // cp [ "Page.template" ] [ "src"; "Pages"; "Page.fs" ] None

        let routeData = {
            Routes = [|
                {
                    Name = "Home"
                    Args = ""
                    Url = "/"
                    UrlSegments = [| "/" |]
                    Query = ""
                }
            |]
        }

        cp [ "Routes.template" ] [ "src"; "Routes.fs" ] (Some(processTemplate routeData))

        printfn
            $"""
        %s{appTitle} (v%s{version}) created a new project in ./%s{projectName}
        ⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺⎺
        Here are some next steps:

        cd %s{projectName}
        elmish-land server
        """
    with :? IOException as ex ->
        printfn $"%s{ex.Message}"
