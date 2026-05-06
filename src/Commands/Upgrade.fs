module ElmishLand.Upgrade

open System
open System.IO
open System.Text.RegularExpressions
open ElmishLand.Effect
open Orsak
open ElmishLand.Log
open ElmishLand.Base
open ElmishLand.AppError

// Source-level renames from Feliz 2 to Feliz 3.
// Each pair is (oldIdentifier, newIdentifier). Replacement is whole-identifier
// (does not match a longer identifier such as React.fragmentExtra).
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

/// Apply automatic Feliz 2 → 3 source replacements to the given file content.
/// Returns the new content and the number of replacements made.
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

/// Update Directory.Packages.props so that any package listed in `nugetDependencies`
/// uses the version pinned by elmish-land. Returns (newContent, changed).
let upgradeProjectPackagesXml (content: string) : string * bool =
    let mutable text = content
    let mutable changed = false

    for name, ver in nugetDependencies do
        let pattern =
            sprintf """<PackageVersion\s+Include="%s"\s+Version="[^"]*"\s*/>""" (Regex.Escape(name))

        let replacement =
            sprintf "<PackageVersion Include=\"%s\" Version=\"%s\" />" name ver

        let rx = Regex(pattern)

        if rx.IsMatch(text) then
            let next = rx.Replace(text, replacement)

            if next <> text then
                changed <- true
                text <- next

    text, changed

let private bumpJsonStringField (name: string) (newValue: string) (text: string) : string * bool =
    let pattern = sprintf @"(""%s""\s*:\s*"")[^""]*("")" (Regex.Escape(name))
    let rx = Regex(pattern)

    if rx.IsMatch(text) then
        let next =
            rx.Replace(text, fun m -> m.Groups.[1].Value + newValue + m.Groups.[2].Value)

        if next <> text then next, true else text, false
    else
        text, false

/// Update package.json so npm dependency versions match what elmish-land currently
/// scaffolds. Returns (newContent, changed).
let upgradePackageJson (content: string) : string * bool =
    let mutable text = content
    let mutable changed = false

    let allDeps = Seq.append (npmDependencies :> seq<_>) (npmDevDependencies :> seq<_>)

    for name, ver in allDeps do
        let next, didChange = bumpJsonStringField name ($"^%s{ver}") text

        if didChange then
            changed <- true
            text <- next

    text, changed

/// Update .config/dotnet-tools.json so the fable tool version matches what
/// elmish-land currently scaffolds. Returns (newContent, changed).
let upgradeDotnetToolsJson (content: string) : string * bool =
    let mutable text = content
    let mutable changed = false

    for name, versionSpec in getDotnetToolDependencies () do
        // versionSpec looks like "--version 5.* --prerelease"; pick the major from "5.*"
        let majorMatch = Regex.Match(versionSpec, @"(\d+)\.")

        if majorMatch.Success then
            let major = majorMatch.Groups.[1].Value

            let isPrerelease =
                versionSpec.Contains("--prerelease") || versionSpec.Contains("-prerelease")

            let placeholder =
                if isPrerelease then
                    $"%s{major}.0.0-prerelease"
                else
                    $"%s{major}.0.0"

            // Replace the version inside this tool's JSON object.
            // Pattern: "name": { ... "version": "..."
            let pattern =
                sprintf @"(""%s""\s*:\s*\{[^}]*?""version""\s*:\s*"")[^""]*("")" (Regex.Escape(name))

            let rx = Regex(pattern, RegexOptions.Singleline)

            if rx.IsMatch(text) then
                let next =
                    rx.Replace(text, fun m -> m.Groups.[1].Value + placeholder + m.Groups.[2].Value)

                if next <> text then
                    changed <- true
                    text <- next

    text, changed

let private isUserSourceFile (relPath: string) =
    let p = relPath.Replace("\\", "/")

    p.EndsWith(".fs", StringComparison.OrdinalIgnoreCase)
    && not (p.Contains("/.elmish-land/"))
    && not (p.StartsWith(".elmish-land/"))
    && not (p.Contains("/bin/"))
    && not (p.Contains("/obj/"))
    && not (p.Contains("/node_modules/"))
    && not (p.Contains("/fable_modules/"))

let private enumerateUserSourceFiles (projectRoot: string) =
    if not (Directory.Exists projectRoot) then
        Seq.empty
    else
        Directory.EnumerateFiles(projectRoot, "*.fs", SearchOption.AllDirectories)
        |> Seq.filter (fun full ->
            let rel = Path.GetRelativePath(projectRoot, full).Replace("\\", "/")

            isUserSourceFile rel)

let private rewriteFileIfChanged (log: ILog) (path: string) (transform: string -> string * 'change) =
    let original = File.ReadAllText(path)
    let updated, change = transform original

    if updated <> original then
        File.WriteAllText(path, updated)
        log.Info $"  ✓ Updated %s{path}"

    change

let private findFirst (candidates: string list) = candidates |> List.tryFind File.Exists

let upgrade
    (workingDirectory: FilePath)
    (absoluteProjectDir: AbsoluteProjectDir)
    (promptBehaviour: UserPromptBehaviour)
    =
    eff {
        let! log = Log().Get()

        let projectRoot = AbsoluteProjectDir.asString absoluteProjectDir
        let workingRoot = FilePath.asString workingDirectory

        log.Info(getCommandHeader "is upgrading your project to Feliz 3 / Fable 5...")

        // Confirm before touching files.
        let proceed =
            match promptBehaviour with
            | AutoAccept ->
                log.Info "🤖 Auto-accepting upgrade"
                true
            | AutoDecline ->
                log.Info "🤖 Auto-declining upgrade"
                false
            | Ask ->
                log.Info
                    "\nThis will rewrite source files and bump dependency versions in this project. Continue? [y/N]"

                let response = Console.ReadLine()

                String.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
                || String.Equals(response, "yes", StringComparison.OrdinalIgnoreCase)

        if not proceed then
            log.Info "Upgrade cancelled."
            return ()
        else
            // 1. Source-level transformations + manual-migration detection
            let mutable totalCodeChanges = 0
            let manualNeeded = ResizeArray<string * ManualMigration>()

            for path in enumerateUserSourceFiles projectRoot do
                let original = File.ReadAllText(path)
                let updated, changes = upgradeFelizSource original

                if changes > 0 then
                    File.WriteAllText(path, updated)
                    totalCodeChanges <- totalCodeChanges + changes

                    let rel = Path.GetRelativePath(projectRoot, path)
                    log.Info $"  ✓ Updated %s{rel} (%d{changes} replacement(s))"

                for issue in detectManualMigrations updated do
                    let rel = Path.GetRelativePath(projectRoot, path)
                    manualNeeded.Add(rel, issue)

            // 2. Directory.Packages.props (search project then working dir then parents)
            let packagesPropsCandidates = [
                Path.Combine(projectRoot, "Directory.Packages.props")
                Path.Combine(workingRoot, "Directory.Packages.props")
            ]

            match findFirst packagesPropsCandidates with
            | Some path -> rewriteFileIfChanged log path upgradeProjectPackagesXml |> ignore
            | None ->
                log.Info
                    "  (no Directory.Packages.props found - if you pin packages elsewhere, update Feliz to the latest 3.x manually)"

            // 3. package.json
            let packageJsonPath = Path.Combine(projectRoot, "package.json")

            if File.Exists packageJsonPath then
                rewriteFileIfChanged log packageJsonPath upgradePackageJson |> ignore
            else
                log.Info "  (no package.json found in project directory)"

            // 4. dotnet-tools.json (under .config or repo root, per current init logic)
            let toolsJsonCandidates = [
                Path.Combine(workingRoot, ".config", "dotnet-tools.json")
                Path.Combine(workingRoot, "dotnet-tools.json")
                Path.Combine(projectRoot, ".config", "dotnet-tools.json")
            ]

            match findFirst toolsJsonCandidates with
            | Some path -> rewriteFileIfChanged log path upgradeDotnetToolsJson |> ignore
            | None ->
                log.Info
                    "  (no dotnet-tools.json found - run 'dotnet tool install fable --prerelease --version 5.*' manually)"

            // 5. Summary + manual migration warnings
            log.Info ""
            log.Info(getCommandHeader "upgrade summary")

            log.Info $"  • %d{totalCodeChanges} automatic code replacement(s) applied"
            log.Info $"  • %d{manualNeeded.Count} location(s) need manual review"

            if manualNeeded.Count > 0 then
                log.Info ""
                log.Info "Manual migration required:"

                for rel, issue in manualNeeded do
                    log.Info $"  • %s{rel}:%d{issue.Line} — %s{issue.Message}"
                    log.Info $"      see %s{issue.DocsUrl}"

            log.Info ""
            log.Info "Next steps:"
            log.Info "  1. Run 'dotnet tool restore' to install the new fable version."
            log.Info "  2. Run 'npm install' to install React 19 and the new vite version."
            log.Info "  3. Run 'dotnet elmish-land restore' to regenerate framework files."

            return ()
    }
