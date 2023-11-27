module ElmishLand.AddPage

open System.IO

// let files (projectDir) =
//     Directory.GetFiles(projectDir, "page.fs", EnumerationOptions(RecurseSubdirectories = true))
//     |> Array.sortBy (fun x -> if x.EndsWith("src\Pages\Page.fs") then "" else x)
//
// let routeData (file: string) =
//     let route =
//         file[0 .. file.Length - 9]
//         |> String.replace rootDir ""
//         |> String.replace "\\" "/"
//
//     route[1..]
//     |> String.split "/"
//     |> Array.fold
//         (fun (parts, args) part ->
//             if part.StartsWith("{") && part.EndsWith("}") then
//                 $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts,
//                 RouteArg $"%s{part[1..1].ToLower()}%s{part[2 .. part.Length - 2]}" :: args
//             else
//                 $"%s{part[0..0].ToUpper()}{part[1..]}" :: parts, args)
//         ([], [])
//     |> fun (parts, args) ->
//         let duName = String.concat "_" (List.rev parts)
//
//         Url(if route = "" then "/" else route),
//         RouteFsharpTypeName(
//             if duName.Contains("{") then $"``%s{duName}``"
//             else if duName = "" then "Home"
//             else duName
//         ),
//         List.rev args
