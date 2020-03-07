namespace Castos

open Thoth.Json.Net

[<AutoOpen>]
module Json =
    let inline unjson<'T> json =
            let a = Decode.Auto.unsafeFromString<'T>(json, caseStrategy=CamelCase)
            a

    let inline mkjson a =
            let json = Encode.Auto.toString(4, a, caseStrategy=CamelCase)
            json