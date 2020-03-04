namespace Castos

open Thoth.Json.Net

[<AutoOpen>]
module Json =
    let inline unjson<'T> json =
            let a = Decode.Auto.fromString<'T>(json, isCamelCase=true)
            a

    let inline mkjson a =
            let json = Encode.Auto.toString(4, a)
            json