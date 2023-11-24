module ElmishLand.Server

open ElmishLand.Base

let server workingDirectory =
    startProcess workingDirectory "npm" [| "start" |]
