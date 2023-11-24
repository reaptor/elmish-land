module ElmishLand.Init

open System
open System.IO
open System.Reflection
open ElmishLand.Base

let init (projectName: string) =
    let projectDir =
        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, projectName))

    let templatesDir =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "templates")

    if not projectDir.Exists then
        projectDir.Create()

    try
        let cp src dst =
            File.Copy(Path.Combine(templatesDir, src), Path.Combine(projectDir.FullName, dst))

        let cpSame fileName = cp fileName fileName
        cp "PROJECT_NAME.fsproj" $"%s{projectName}.fsproj"
        cpSame "global.json"
        cpSame "package.json"
        cpSame "package-lock.json"
        cpSame "index.html"

        let dotnetConfigDir = DirectoryInfo(Path.Combine(projectDir.FullName, ".config"))

        if not dotnetConfigDir.Exists then
            dotnetConfigDir.Create()

        cpSame (Path.Combine(dotnetConfigDir.Name, "dotnet-tools.json"))

        let srcDir = DirectoryInfo(Path.Combine(projectDir.FullName, "src"))

        if not srcDir.Exists then
            srcDir.Create()

        cpSame (Path.Combine(srcDir.Name, "Shared.fs"))
        cpSame (Path.Combine(srcDir.Name, "App.fs"))
        cpSame (Path.Combine(srcDir.Name, "Routes.fs"))
        let pagesDir = DirectoryInfo(Path.Combine(srcDir.FullName, "Pages"))

        if not pagesDir.Exists then
            pagesDir.Create()

        cpSame (Path.Combine(srcDir.Name, pagesDir.Name, "Page.fs"))

        let packageJsonPath = Path.Combine(projectDir.FullName, "package.json")
        File.WriteAllText(packageJsonPath, File.ReadAllText(packageJsonPath).Replace("{{PROJECT_NAME}}", projectName))

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
