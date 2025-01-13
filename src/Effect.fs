module ElmishLand.Effect

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open ElmishLand.Base
open Orsak

type ILog =
    abstract member Debug: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Info: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Error: message: string * [<ParamArray>] args: obj array -> unit

type ILogProvider =
    abstract member GetLogger: string * string * int -> ILog

type Log
    (
        [<CallerMemberName; Optional; DefaultParameterValue("")>] memberName: string,
        [<CallerFilePath; Optional; DefaultParameterValue("")>] path: string,
        [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line: int
    ) =
    member _.Get() =
        Effect.Create(fun (provider: #ILogProvider) -> provider.GetLogger(memberName, path, line))

type IFileSystem =
    abstract member FilePathExists: FilePath -> bool

module FileSystem =
    let filePathExists filePath =
        Effect.Create(fun (provider: #IFileSystem) -> provider.FilePathExists(filePath))

type IEffectEnv =
    inherit ILogProvider
    inherit IFileSystem
