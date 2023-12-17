module Expects

open Xunit

let ok (x: Result<'a, 'e>) : 'a =
    match x with
    | Ok x -> x
    | Error e ->
        Assert.Fail $"%A{e}"
        Unchecked.defaultof<'a>

let equals expected actual = Assert.Equal(expected, actual)
