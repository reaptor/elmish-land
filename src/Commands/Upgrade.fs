module ElmishLand.Upgrade

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Effect
open ElmishLand.Resources
open Orsak
open ElmishLand.Base
open ElmishLand.DotNetCli
open ElmishLand.AppError
open ElmishLand.Generate
open ElmishLand.PackageVersions

let successMessage () =
    let header = getCommandHeader "upgraded your project!"

    let content =
        """Your project files have been updated. To finish the upgrade, run:

1. dotnet restore
2. npm install
3. dotnet elmish-land server"""

    getFormattedCommandOutput header content

// === Source-level transformations from Feliz 2 to Feliz 3 ===

/// Whole-identifier renames. The matcher uses identifier boundaries on both sides,
/// so it won't touch a longer name that starts with the old one (e.g. React.fragmentExtra)
/// and re-running the upgrade on already-upgraded code is a no-op.
let felizCodeReplacements = [
    // Components moved to PascalCase
    "React.fragment", "React.Fragment"
    "React.keyedFragment", "React.KeyedFragment"
    "React.imported", "React.Imported"
    "React.dynamicImported", "React.DynamicImported"
    "React.strictMode", "React.StrictMode"
    "React.suspense", "React.Suspense"
    // Helpers relocated to the FsReact namespace
    "React.createDisposable", "FsReact.createDisposable"
    "React.useDisposable", "FsReact.useDisposable"
    "React.useCancellationToken", "FsReact.useCancellationToken"
]

let private wholeIdentRegex (oldName: string) =
    Regex("(?<![A-Za-z0-9_])" + Regex.Escape(oldName) + "(?![A-Za-z0-9_'])")

/// Apply the Feliz 2 → 3 source replacements to a file's contents.
/// Returns the new contents and the number of replacements made.
let upgradeFelizSource (content: string) : string * int =
    let mutable text = content
    let mutable changes = 0

    for oldName, newName in felizCodeReplacements do
        let rx = wholeIdentRegex oldName
        let matches = rx.Matches(text)

        if matches.Count > 0 then
            changes <- changes + matches.Count
            text <- rx.Replace(text, newName)

    text, changes

type ManualMigration = {
    Line: int
    Pattern: string
    Message: string
    DocsUrl: string
}

let private memoNeedsReview = Regex(@"\bReact\.memo\b(?!Renderer)")
let private lazyNeedsReview = Regex(@"\bReact\.lazy'")

let private contextNeedsReview =
    Regex(@"\bReact\.(contextProvider|contextConsumer)\b")

let felizUpgradeDocsBase = "https://fable-hub.github.io/Feliz/api-docs/Upgrade"

let felizWritingBindingsDocsUrl =
    "https://fable-hub.github.io/Feliz/api-docs/guides/writing-bindings"

/// Find code patterns that require manual migration after Feliz 2 → 3.
let detectManualMigrations (content: string) : ManualMigration list = [
    let lines = content.Replace("\r\n", "\n").Split('\n')

    for i, line in Array.indexed lines do
        if memoNeedsReview.IsMatch(line) then
            yield {
                Line = i + 1
                Pattern = "React.memo"
                Message = "React.memo now requires explicit React.memoRenderer call sites at usage points"
                DocsUrl = $"%s{felizUpgradeDocsBase}#reactmemo"
            }

        if lazyNeedsReview.IsMatch(line) then
            yield {
                Line = i + 1
                Pattern = "React.lazy'"
                Message = "React.lazy' now requires explicit React.lazyRender call sites at usage points"
                DocsUrl = $"%s{felizUpgradeDocsBase}#reactlazy"
            }

        if contextNeedsReview.IsMatch(line) then
            yield {
                Line = i + 1
                Pattern = "React.context*"
                Message = "React.context API redesigned - use ContextName.Provider(...) / ContextName.Consumer(...)"
                DocsUrl = $"%s{felizUpgradeDocsBase}#reactcontext"
            }
]

let private isUserSourceFile (relPath: string) =
    let p = relPath.Replace("\\", "/")

    let isExcludedDir (dir: string) =
        p.StartsWith(dir + "/") || p.Contains("/" + dir + "/")

    p.EndsWith(".fs", StringComparison.OrdinalIgnoreCase)
    && not (isExcludedDir ".elmish-land")
    && not (isExcludedDir "bin")
    && not (isExcludedDir "obj")
    && not (isExcludedDir "node_modules")
    && not (isExcludedDir "fable_modules")

let private enumerateUserSourceFiles (projectRoot: string) =
    if not (Directory.Exists projectRoot) then
        Seq.empty
    else
        Directory.EnumerateFiles(projectRoot, "*.fs", SearchOption.AllDirectories)
        |> Seq.filter (fun full ->
            let rel = Path.GetRelativePath(projectRoot, full).Replace("\\", "/")

            isUserSourceFile rel)

// === File-version updaters (kept from v2's runtime version resolution flow) ===

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

let private removePackageVersionEntry (name: string) (xml: string) =
    let escapedName = Regex.Escape name
    // Match a self-closing <PackageVersion Include="name" ... /> plus the whitespace and
    // newline that precede it, so we don't leave a blank line behind.
    let pattern = $"\\s*<PackageVersion\\b[^/>]*\\bInclude=\"%s{escapedName}\"[^/>]*/>"

    Regex.Replace(xml, pattern, "")

let private removePackageReferenceEntry (name: string) (xml: string) =
    let escapedName = Regex.Escape name

    let pattern =
        $"\\s*<PackageReference\\b[^/>]*\\bInclude=\"%s{escapedName}\"[^/>]*/>"

    Regex.Replace(xml, pattern, "")

let removedNugetDependencies = [ "Feliz.Router" ]

let private isUserProjectFile (relPath: string) =
    let p = relPath.Replace("\\", "/")

    let isExcludedDir (dir: string) =
        p.StartsWith(dir + "/") || p.Contains("/" + dir + "/")

    p.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
    && not (isExcludedDir ".elmish-land")
    && not (isExcludedDir "bin")
    && not (isExcludedDir "obj")
    && not (isExcludedDir "node_modules")
    && not (isExcludedDir "fable_modules")

let private enumerateUserProjectFiles (projectRoot: string) =
    if not (Directory.Exists projectRoot) then
        Seq.empty
    else
        Directory.EnumerateFiles(projectRoot, "*.fsproj", SearchOption.AllDirectories)
        |> Seq.filter (fun full ->
            let rel = Path.GetRelativePath(projectRoot, full).Replace("\\", "/")
            isUserProjectFile rel)

let upgradeUserProjectFiles (workingDirectory: FilePath) =
    eff {
        let! log = Log().Get()
        let projectRoot = FilePath.asString workingDirectory

        for path in enumerateUserProjectFiles projectRoot do
            let original = File.ReadAllText(path)

            let updated =
                (original, removedNugetDependencies)
                ||> List.fold (fun acc name -> removePackageReferenceEntry name acc)

            if updated <> original then
                File.WriteAllText(path, updated)
                let rel = Path.GetRelativePath(projectRoot, path)
                log.Debug("Stripped removed PackageReference entries from {}", rel)
    }

let upgradeRootFiles workingDirectory (resolvedNugetDependencies: (string * string) list) =
    eff {
        let! log = Log().Get()

        let packagesPropsPath =
            workingDirectory |> FilePath.appendParts [ "Directory.Packages.props" ]

        if FilePath.exists packagesPropsPath then
            let updated =
                (FilePath.readAllText packagesPropsPath, resolvedNugetDependencies)
                ||> List.fold (fun acc (name, ver) -> updatePackageVersionEntry name ver acc)

            let updated =
                (updated, removedNugetDependencies)
                ||> List.fold (fun acc name -> removePackageVersionEntry name acc)

            File.WriteAllText(FilePath.asString packagesPropsPath, updated)
        else
            writeResource<Directory_Packages_props_template> log workingDirectory false [ "Directory.Packages.props" ] {
                PackageVersions = getNugetPackageVersions resolvedNugetDependencies
            }
    }

/// Update package.json and dotnet-tools.json under the given directory, if they exist.
/// Called once per candidate directory (working directory and each per-project dir),
/// deduped by the caller — repos vary on whether these files live at the solution root
/// or inside each app dir.
let upgradeProjectFiles
    (projectDir: FilePath)
    (resolvedNpmDependencies: (string * string) list)
    (resolvedNpmDevDependencies: (string * string) list)
    (resolvedDotnetTools: (string * string) list)
    =
    eff {
        let! log = Log().Get()

        log.Debug("Upgrading project files in: {}", FilePath.asString projectDir)

        let packageJsonPath = projectDir |> FilePath.appendParts [ "package.json" ]

        if FilePath.exists packageJsonPath then
            let json =
                (FilePath.readAllText packageJsonPath, resolvedNpmDependencies)
                ||> List.fold (fun acc (name, ver) -> updateNpmDependencyVersion name ver acc)

            let json =
                (json, resolvedNpmDevDependencies)
                ||> List.fold (fun acc (name, ver) -> updateNpmDependencyVersion name ver acc)

            File.WriteAllText(FilePath.asString packageJsonPath, json)

        let dotnetToolsJsonPaths = [
            projectDir |> FilePath.appendParts [ ".config"; "dotnet-tools.json" ]
            projectDir |> FilePath.appendParts [ "dotnet-tools.json" ]
        ]

        for path in dotnetToolsJsonPaths do
            if FilePath.exists path then
                let json =
                    (FilePath.readAllText path, resolvedDotnetTools)
                    ||> List.fold (fun acc (name, ver) -> updateDotnetToolVersion name ver acc)

                File.WriteAllText(FilePath.asString path, json)
    }

/// Apply the Feliz 2 → 3 source-level renames across every user .fs file under
/// the working directory, and collect any patterns that require manual migration.
/// Skips generated and tooling directories. Walking from the working directory
/// (rather than per elmish-land app dir) ensures shared libraries and sibling
/// projects in the same repository are also rewritten.
let upgradeUserSourceFiles (workingDirectory: FilePath) =
    eff {
        let! log = Log().Get()

        let projectRoot = FilePath.asString workingDirectory
        let mutable totalCodeChanges = 0
        let manualNeeded = ResizeArray<string * ManualMigration>()

        for path in enumerateUserSourceFiles projectRoot do
            let original = File.ReadAllText(path)
            let updated, changes = upgradeFelizSource original

            if changes > 0 then
                File.WriteAllText(path, updated)
                totalCodeChanges <- totalCodeChanges + changes

                let rel = Path.GetRelativePath(projectRoot, path)
                log.Debug("Updated {} ({} replacement(s))", rel, changes)

            for issue in detectManualMigrations updated do
                let rel = Path.GetRelativePath(projectRoot, path)
                manualNeeded.Add(rel, issue)

        log.Info $"  • %d{totalCodeChanges} automatic code replacement(s) applied"
        log.Info $"  • %d{manualNeeded.Count} location(s) need manual review"

        if manualNeeded.Count > 0 then
            log.Info ""
            log.Info "Manual migration required:"

            for rel, issue in manualNeeded do
                log.Info $"  • %s{rel}:%d{issue.Line} — %s{issue.Message}"
                log.Info $"      see %s{issue.DocsUrl}"

        log.Info "For information on breaking changes in Feliz 3:"
        log.Info $"  • Upgrade to v3: %s{felizUpgradeDocsBase}"
        log.Info $"  • Writing Bindings: %s{felizWritingBindingsDocsUrl}"
    }

let upgrade workingDirectory (absoluteProjectDir: AbsoluteProjectDir) =
    eff {
        let! log = Log().Get()

        let! dotnetSdkVersion = getLatestInstalledDotnetSdkVersion workingDirectory

        let settingsFiles =
            Directory.EnumerateFiles(
                AbsoluteProjectDir.asString absoluteProjectDir,
                "elmish-land.json",
                SearchOption.AllDirectories
            )
            |> Seq.toList

        if List.isEmpty settingsFiles then
            return! Error AppError.ElmishLandProjectMissing
        else
            let! resolvedNugetDependencies = resolveNugetDependencies nugetDependencies
            let! resolvedNpmDependencies = resolveNpmDependencies npmDependencies
            let! resolvedNpmDevDependencies = resolveNpmDependencies npmDevDependencies
            let! resolvedDotnetTools = resolveNugetDependencies (getDotnetToolDependencies ())

            do!
                withSpinner "Upgrading your project..." (fun _ ->
                    eff {
                        do! upgradeRootFiles workingDirectory resolvedNugetDependencies
                        do! upgradeUserProjectFiles workingDirectory
                        do! upgradeUserSourceFiles workingDirectory

                        let subAbsoluteProjectDirs =
                            settingsFiles
                            |> List.map (fun settingsFile ->
                                settingsFile
                                |> FilePath.fromString
                                |> FilePath.directoryPath
                                |> AbsoluteProjectDir)

                        let projectFileDirs =
                            workingDirectory
                            :: (subAbsoluteProjectDirs |> List.map AbsoluteProjectDir.asFilePath)
                            |> List.distinctBy FilePath.asString

                        for projectDir in projectFileDirs do
                            do!
                                upgradeProjectFiles
                                    projectDir
                                    resolvedNpmDependencies
                                    resolvedNpmDevDependencies
                                    resolvedDotnetTools

                        for subAbsoluteProjectDir in subAbsoluteProjectDirs do
                            do! generate workingDirectory subAbsoluteProjectDir dotnetSdkVersion
                    })

            log.Info(successMessage ())
    }
