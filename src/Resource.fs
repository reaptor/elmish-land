module ElmishLand.Resource

open System.IO
open System.Reflection
open ElmishLand.Base
open ElmishLand.Log
open Orsak

let writeResource (projectDir: AbsoluteProjectDir) overwrite (resourceName: string) dst replace =
    eff {
        let! log = Log().Get()
        let! fs = getFileSystem ()
        let dstPath = AbsoluteProjectDir.asFilePath projectDir |> FilePath.appendParts dst

        if overwrite || not (fs.FileExists(dstPath)) then
            let dstDir = FilePath.directoryPath dstPath

            fs.EnsureDirectory(dstDir)

            let assembly = Assembly.GetExecutingAssembly()

            log.Debug("Writing resource '{}' to '{}'", resourceName, FilePath.asString dstPath)

            use stream = assembly.GetManifestResourceStream(resourceName)

            if stream = null then
                failwith $"'%s{resourceName}' not found in assembly"

            use reader = new StreamReader(stream)
            let fileContents = reader.ReadToEnd()
            fs.WriteAllText(dstPath, fileContents)

            match replace with
            | Some f ->
                fs.ReadAllText(dstPath)
                |> f
                |> (fun x -> fs.WriteAllText(dstPath, x))
            | None -> ()
    }
