module ElmishLand.Resources

open System.IO
open System.Reflection
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.TemplateEngine
open Microsoft.FSharp.Reflection

type AddLayout_template = {
    ViewModule: string
    ViewType: string
    RootModule: string
    Layout: Layout
}

type AddPage_template = {
    ViewModule: string
    ViewType: string
    ScaffoldTextElement: string
    RootModule: string
    Route: Route
}

type global_json_template = { DotNetSdkVersion: string }

type Directory_Packages_props_template = { PackageVersions: string }

type Project_fsproj_template = {
    DotNetVersion: string
    ProjectName: string
}

type package_json_template = {
    ProjectName: string
    Dependencies: string
    DevDependencies: string
}

type index_html_template = { Title: string }

type NotFound_template = {
    ScaffoldTextElement: string
    RootModule: string
}

type Shared_template = | Shared_template of TemplateData

type Base_fsproj_template = {
    DotNetVersion: string
    PackageReferences: string
    ProjectReferences: string list
}

type App_fsproj_template = {
    DotNetVersion: string
    ProjectReferences: string list
    UseRouterPathMode: bool
}

type Routes_template = | Routes_template of TemplateData

type Command_fs_template = | Command_fs_template of TemplateData

type Page_fs_template = | Page_fs_template of TemplateData

type Layout_fs_template = | Layout_fs_template of TemplateData

type App_template = | App_template of TemplateData

type settings_json = Settings_json

type ``elmish-land_json`` = { RouteMode: string }

type vite_config_js = Vite_config_js

let private getResourceName<'Resource> () =
    typeof<'Resource>.Name.Replace("_", ".")

let getResource<'Resource> (resource: 'Resource) =
    let assembly = Assembly.GetExecutingAssembly()

    let resourceName = getResourceName<'Resource> ()
    use stream = assembly.GetManifestResourceStream(resourceName)

    if stream = null then
        failwith $"'%s{resourceName}' not found in assembly"

    use reader = new StreamReader(stream)
    let fileContents = reader.ReadToEnd()
    let resourceType = typeof<'Resource>

    if FSharpType.IsUnion(resourceType) then
        let unionCases = FSharpType.GetUnionCases(resourceType)

        if unionCases.Length > 0 then
            let fields = unionCases[0].GetFields()

            if fields.Length = 0 then
                fileContents
            else
                let resource' = fields[0].GetValue(resource)
                handlebars resource' fileContents
        else
            fileContents
    else
        handlebars resource fileContents

let writeResource<'Resource> (log: ILog) (workingDir: FilePath) overwrite dst (resource: 'Resource) =
    let dstPath = workingDir |> FilePath.appendParts dst

    if overwrite || not (File.Exists(FilePath.asString dstPath)) then
        let dstDir = FilePath.directoryPath dstPath |> FilePath.asString

        if not (Directory.Exists dstDir) then
            Directory.CreateDirectory(dstDir) |> ignore

        let resourceName = getResourceName<'Resource> ()

        log.Debug("Writing resource '{}' to '{}'", resourceName, FilePath.asString dstPath)

        let fileContents = getResource<'Resource> (resource)
        File.WriteAllText(FilePath.asString dstPath, fileContents)

let generateFiles log workingDir (templateData: TemplateData) =
    writeResource<Routes_template>
        log
        workingDir
        true
        [ ".elmish-land"; "Base"; "Routes.fs" ]
        (Routes_template templateData)

    writeResource<Command_fs_template>
        log
        workingDir
        true
        [ ".elmish-land"; "Base"; "Command.fs" ]
        (Command_fs_template templateData)

    writeResource<Page_fs_template>
        log
        workingDir
        true
        [ ".elmish-land"; "Base"; "Page.fs" ]
        (Page_fs_template templateData)

    writeResource<Layout_fs_template>
        log
        workingDir
        true
        [ ".elmish-land"; "Base"; "Layout.fs" ]
        (Layout_fs_template templateData)

    writeResource<App_template> log workingDir true [ ".elmish-land"; "App"; "App.fs" ] (App_template templateData)
