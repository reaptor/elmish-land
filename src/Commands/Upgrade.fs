module ElmishLand.Upgrade

open System.IO
open System.Text.RegularExpressions
open System.Threading
open ElmishLand.Effect
open ElmishLand.Resources
open Orsak
open ElmishLand.Base
open ElmishLand.DotNetCli
open ElmishLand.Process
open ElmishLand.AppError
open ElmishLand.Generate
open ElmishLand.PackageVersions

let successMessage () =
    let header = getCommandHeader "upgraded your project!"

    let content =
        """Run the following command to start the development server:

dotnet elmish-land server"""

    getFormattedCommandOutput header content

let private updateNpmDependencyVersion (name: string) (version: string) (json: string) =
    let escapedName = Regex.Escape name
    let pattern = $"\"%s{escapedName}\"\\s*:\\s*\"[^\"]*\""
    let replacement = $"\"%s{name}\": \"^%s{version}\""

    if Regex.IsMatch(json, pattern) then
        Regex.Replace(json, pattern, replacement)
    else
        json

let private updateDotnetToolVersion (name: string) (version: string) (json: string) =
    let escapedName = Regex.Escape name
    // Match the "name": { ... "version": "..." ... } block and replace just the version string.
    let pattern =
        $"(\"%s{escapedName}\"\\s*:\\s*\\{{[^}}]*?\"version\"\\s*:\\s*\")[^\"]*"

    let replacement = $"${{1}}%s{version}"

    if Regex.IsMatch(json, pattern) then
        Regex.Replace(json, pattern, replacement)
    else
        json

let private updatePackageVersionEntry (name: string) (version: string) (xml: string) =
    let escapedName = Regex.Escape name
    // Match a PackageVersion element for this Include name, regardless of attribute order or
    // whitespace, and replace just the Version attribute value.
    let updatePattern =
        $"(<PackageVersion\\b[^/>]*\\bInclude=\"%s{escapedName}\"[^/>]*\\bVersion=\")[^\"]*"

    if Regex.IsMatch(xml, updatePattern) then
        Regex.Replace(xml, updatePattern, $"${{1}}%s{version}")
    else
        // Insert a new entry before the last </ItemGroup>. If there is none, add one before
        // </Project>. Preserves any existing user content otherwise.
        let entry =
            $"        <PackageVersion Include=\"%s{name}\" Version=\"%s{version}\" />\n    "

        let itemGroupClose = "</ItemGroup>"
        let idx = xml.LastIndexOf(itemGroupClose)

        if idx >= 0 then
            xml.Insert(idx, entry)
        else
            let projectClose = "</Project>"
            let pIdx = xml.LastIndexOf(projectClose)

            if pIdx >= 0 then
                let block =
                    $"    <ItemGroup>\n        <PackageVersion Include=\"%s{name}\" Version=\"%s{version}\" />\n    </ItemGroup>\n"

                xml.Insert(pIdx, block)
            else
                xml

let private updateGlobalJsonSdkVersion (version: string) (json: string) =
    let pattern = "(\"sdk\"\\s*:\\s*\\{[^}]*?\"version\"\\s*:\\s*\")[^\"]*"

    if Regex.IsMatch(json, pattern) then
        Regex.Replace(json, pattern, $"${{1}}%s{version}")
    else
        json

let upgradeFiles workingDirectory (absoluteProjectDir: AbsoluteProjectDir) (dotnetSdkVersion: DotnetSdkVersion) =
    eff {
        let! log = Log().Get()

        let elmishLandJsonPath =
            absoluteProjectDir
            |> AbsoluteProjectDir.asFilePath
            |> FilePath.appendParts [ "elmish-land.json" ]

        if not (FilePath.exists elmishLandJsonPath) then
            return! Error AppError.ElmishLandProjectMissing
        else
            log.Debug("Upgrading project: {}", AbsoluteProjectDir.asString absoluteProjectDir)
            log.Debug("Using .NET SDK: {}", dotnetSdkVersion)

            let! resolvedNugetDependencies = resolveNugetDependencies nugetDependencies
            let! resolvedNpmDependencies = resolveNpmDependencies npmDependencies
            let! resolvedNpmDevDependencies = resolveNpmDependencies npmDevDependencies
            let! resolvedDotnetTools = resolveNugetDependencies (getDotnetToolDependencies ())

            let globalJsonPath = workingDirectory |> FilePath.appendParts [ "global.json" ]

            if FilePath.exists globalJsonPath then
                let updated =
                    updateGlobalJsonSdkVersion
                        (DotnetSdkVersion.asString dotnetSdkVersion)
                        (FilePath.readAllText globalJsonPath)

                File.WriteAllText(FilePath.asString globalJsonPath, updated)
            else
                writeResource<global_json_template> log workingDirectory false [ "global.json" ] {
                    DotNetSdkVersion = (DotnetSdkVersion.asString dotnetSdkVersion)
                }

            let packagesPropsPath =
                workingDirectory |> FilePath.appendParts [ "Directory.Packages.props" ]

            if FilePath.exists packagesPropsPath then
                let updated =
                    (FilePath.readAllText packagesPropsPath, resolvedNugetDependencies)
                    ||> List.fold (fun acc (name, ver) -> updatePackageVersionEntry name ver acc)

                File.WriteAllText(FilePath.asString packagesPropsPath, updated)
            else
                writeResource<Directory_Packages_props_template>
                    log
                    workingDirectory
                    false
                    [ "Directory.Packages.props" ]
                    {
                        PackageVersions = getNugetPackageVersions resolvedNugetDependencies
                    }

            let packageJsonPath =
                absoluteProjectDir
                |> AbsoluteProjectDir.asFilePath
                |> FilePath.appendParts [ "package.json" ]

            if FilePath.exists packageJsonPath then
                let json =
                    (FilePath.readAllText packageJsonPath, resolvedNpmDependencies)
                    ||> List.fold (fun acc (name, ver) -> updateNpmDependencyVersion name ver acc)

                let json =
                    (json, resolvedNpmDevDependencies)
                    ||> List.fold (fun acc (name, ver) -> updateNpmDependencyVersion name ver acc)

                File.WriteAllText(FilePath.asString packageJsonPath, json)

            let dotnetToolsJsonPaths = [
                absoluteProjectDir
                |> AbsoluteProjectDir.asFilePath
                |> FilePath.appendParts [ ".config"; "dotnet-tools.json" ]
                absoluteProjectDir
                |> AbsoluteProjectDir.asFilePath
                |> FilePath.appendParts [ "dotnet-tools.json" ]
            ]

            for path in dotnetToolsJsonPaths do
                if FilePath.exists path then
                    let json =
                        (FilePath.readAllText path, resolvedDotnetTools)
                        ||> List.fold (fun acc (name, ver) -> updateDotnetToolVersion name ver acc)

                    File.WriteAllText(FilePath.asString path, json)
    }

let upgradeCliCommands _workingDirectory (absoluteProjectDir: AbsoluteProjectDir) =
    let isVerbose = System.Environment.CommandLine.Contains("--verbose")

    eff {
        let projectName = ProjectName.fromAbsoluteProjectDir absoluteProjectDir

        do!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "dotnet"
                [|
                    "restore"
                    $".elmish-land/App/ElmishLand.%s{ProjectName.asString projectName}.App.fsproj"
                |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string * string>

        do!
            runProcess
                isVerbose
                (AbsoluteProjectDir.asFilePath absoluteProjectDir)
                "npm"
                [| "install" |]
                CancellationToken.None
                ignore
            |> Effect.map ignore<string * string>
    }

let upgrade workingDirectory (absoluteProjectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getLatestInstalledDotnetSdkVersion workingDirectory

        do!
            withSpinner "Upgrading your project..." (fun _ ->
                eff {
                    do! upgradeFiles workingDirectory absoluteProjectDir dotnetSdkVersion
                    do! generate workingDirectory absoluteProjectDir dotnetSdkVersion
                    do! upgradeCliCommands workingDirectory absoluteProjectDir
                })

        log.Info(successMessage ())
    }
