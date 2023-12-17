module ElmishLand.Log

open System
open System.Text
open Orsak

let formatMessage (message: string) args =
    args
    |> Array.fold
        (fun (sb: StringBuilder) arg ->
            let i = sb.ToString().IndexOf("{}")
            sb.Replace("{}", (if arg = null then "null" else $"%A{arg}"), i, 2))
        (StringBuilder(message))
    |> _.ToString()

type ILog =
    abstract member Debug: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Info: message: string * [<ParamArray>] args: obj array -> unit
    abstract member Error: message: string * [<ParamArray>] args: obj array -> unit

type ILogProvider =
    abstract member GetLogger: unit -> ILog

module Effect =
    let getLogger () =
        Effect.Create(fun (provider: #ILogProvider) -> provider.GetLogger())
