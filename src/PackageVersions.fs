module ElmishLand.PackageVersions

open System
open ElmishLand.AppError
open ElmishLand.Effect
open Orsak
open Thoth.Json.Net

let selectLatestMatchingMajor (major: int) (versions: string seq) : string option =
    versions
    |> Seq.choose (fun v ->
        if String.IsNullOrWhiteSpace v || v.Contains('-') then
            None
        else
            match Version.TryParse v with
            | true, parsed when parsed.Major = major -> Some(parsed, v)
            | _ -> None)
    |> Seq.toList
    |> function
        | [] -> None
        | matches -> matches |> List.maxBy fst |> snd |> Some

let private nugetVersionsDecoder: Decoder<string list> =
    Decode.field "versions" (Decode.list Decode.string)

let private npmVersionsDecoder: Decoder<string list> =
    Decode.field "versions" (Decode.keyValuePairs (Decode.succeed ()))
    |> Decode.map (List.map fst)

let private decodeOrError name decoder json =
    match Decode.fromString decoder json with
    | Ok versions -> Ok versions
    | Error msg -> Error(PackageVersionResolutionFailed $"Failed to parse versions for %s{name}: %s{msg}")

let private cache =
    System.Collections.Concurrent.ConcurrentDictionary<string, string>()

let private resolveLatest registry (name: string) (major: int) (url: string) (decoder: Decoder<string list>) =
    eff {
        let key = $"%s{registry}::%s{name}::%d{major}"

        match cache.TryGetValue(key) with
        | true, cached -> return cached
        | _ ->
            let! json = Http.getString url
            let! versions = decodeOrError name decoder json

            let! resolved =
                match selectLatestMatchingMajor major versions with
                | Some version -> Ok version
                | None ->
                    Error(
                        PackageVersionResolutionFailed
                            $"No %s{registry} version of %s{name} matching major %d{major} was found"
                    )

            cache.TryAdd(key, resolved) |> ignore
            return resolved
    }

let resolveLatestNugetVersion (name: string) (major: int) =
    let url =
        $"https://api.nuget.org/v3-flatcontainer/%s{name.ToLowerInvariant()}/index.json"

    resolveLatest "NuGet" name major url nugetVersionsDecoder

let resolveLatestNpmVersion (name: string) (major: int) =
    let url = $"https://registry.npmjs.org/%s{name}"
    resolveLatest "npm" name major url npmVersionsDecoder

let private resolveAll (resolver: string -> int -> Effect<IEffectEnv, string, AppError>) (deps: (string * int) seq) =
    let initial: Effect<IEffectEnv, (string * string) list, AppError> =
        eff { return [] }

    deps
    |> Seq.fold
        (fun (state: Effect<IEffectEnv, (string * string) list, AppError>) (name, major) ->
            eff {
                let! resolved = state
                let! version = resolver name major
                return (name, version) :: resolved
            })
        initial
    |> Effect.map List.rev

let resolveNugetDependencies deps =
    resolveAll resolveLatestNugetVersion deps

let resolveNpmDependencies deps = resolveAll resolveLatestNpmVersion deps
