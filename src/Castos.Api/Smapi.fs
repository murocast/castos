namespace Castos

module Smapi =
    let extractSmapiMethod (m:string) =
        m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let processGetMetadata s =
        Success(s)

    let processSmapiMethod a form =
        match extractSmapiMethod a with
        | "getMetadata" -> processGetMetadata form
        | _ -> Failure(sprintf "Method not implemented %s" a)

