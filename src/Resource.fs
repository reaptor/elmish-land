module ElmishLand.Resource

open System.IO
open System.Reflection
open ElmishLand.Base
open ElmishLand.Effect
open ElmishLand.Log
open Orsak

let writeResource (workingDir: FilePath) overwrite (resourceName: string) dst (replace: (string -> string) option) =
    eff {
        let! log = Log().Get()

        let dstPath = workingDir |> FilePath.appendParts dst

        if overwrite || not (File.Exists(FilePath.asString dstPath)) then
            let dstDir = FilePath.directoryPath dstPath |> FilePath.asString

            if not (Directory.Exists dstDir) then
                Directory.CreateDirectory(dstDir) |> ignore

            let assembly = Assembly.GetExecutingAssembly()

            log.Debug("Writing resource '{}' to '{}'", resourceName, FilePath.asString dstPath)

            use stream = assembly.GetManifestResourceStream(resourceName)

            if stream = null then
                failwith $"'%s{resourceName}' not found in assembly"

            use reader = new StreamReader(stream)
            let fileContents = reader.ReadToEnd()
            File.WriteAllText(FilePath.asString dstPath, fileContents)

            match replace with
            | Some f ->
                File.ReadAllText(FilePath.asString dstPath)
                |> f
                |> (fun x -> File.WriteAllText(FilePath.asString dstPath, x))
            | None -> ()
    }
