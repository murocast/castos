namespace Castos

[<AutoOpen>]
module Json =
    open Newtonsoft.Json
    let settings =
        let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
        fableJsonSettings.Converters.Add(Fable.JsonConverter())
        fableJsonSettings

    let inline unjson<'T> json =
            let a = JsonConvert.DeserializeObject<'T>(json, settings)
            a

    let inline mkjson a =
            let json = JsonConvert.SerializeObject(a, settings)
            json