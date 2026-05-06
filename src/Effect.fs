module ElmishLand.Effect

open System
open System.Runtime.CompilerServices
open System.Threading.Tasks
open System.Runtime.InteropServices
open Orsak
open AppError

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

type IHttp =
    abstract member GetAsync: url: string -> Task<Result<string, AppError>>

module Http =
    let getString(url: string) =
        Effect.Create(fun (provider: #IHttp) -> provider.GetAsync(url))

type IEffectEnv =
    inherit ILogProvider
    inherit IHttp
