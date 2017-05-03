namespace Castos

[<AutoOpen>]
module Json =
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp

    let settings = JsonSerializerSettings()
                            |> Serialisation.extend

    let inline unjson<'T> json =
            let a = JsonConvert.DeserializeObject<'T>(json, settings)
            a

    let inline mkjson a =
            let json = JsonConvert.SerializeObject(a, settings)
            json