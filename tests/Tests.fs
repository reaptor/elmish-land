module Tests

open System

let getFolder () =
    $"""Proj_%s{Guid.NewGuid().ToString().Replace("-", "")}"""

// [<Fact>]
// let ``Init, generates project`` () =
//     task {
//         let cts = new CancellationTokenSource()
//         cts.CancelAfter(TimeSpan.FromSeconds 30L)
//         let rootFolder = getFolder ()
//
//         try
//             let! result, logs =
//                 ElmishLand.Program.run [| "init"; "--project-dir"; rootFolder; "--verbose" |]
//                 |> runEff
//
//             Expects.ok logs result
//
//             Expects.equalsIgnoringWhitespace logs $"%s{ElmishLand.Init.successMessage ()}\n" (logs.Info.ToString())
//         finally
//             if Directory.Exists(rootFolder) then
//                 Directory.Delete(rootFolder, true)
//     }
//
// [<Fact>]
// let ``Build, builds project`` () =
//     task {
//         let cts = new CancellationTokenSource()
//         cts.CancelAfter(TimeSpan.FromSeconds 30L)
//         let folder = getFolder ()
//
//         try
//             let! _ = ElmishLand.Program.run [| "init"; "--project-dir"; folder |] |> runEff
//
//             let! result, logs =
//                 ElmishLand.Program.run [| "build"; "--project-dir"; folder; "--verbose" |]
//                 |> runEff
//
//             Expects.ok logs result
//             Expects.equalsIgnoringWhitespace logs $"%s{ElmishLand.Build.successMessage}\n" (logs.Info.ToString())
//         finally
//             if Directory.Exists(folder) then
//                 Directory.Delete(folder, true)
//     }
