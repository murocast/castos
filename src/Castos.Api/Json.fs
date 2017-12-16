namespace Castos

[<AutoOpen>]
module Json =
    open Newtonsoft.Json

    let private converter = Fable.JsonConverter()

    let unjson<'T> json =
            let a = JsonConvert.DeserializeObject(json, typeof<'T>, converter) :?> 'T
            a

    let mkjson a =
            let json = JsonConvert.SerializeObject(a, converter)
            json