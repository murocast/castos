namespace Castos

module Smapi =
    let extractSmapiMethod (m:string) =
        m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let processSmapiMethod a =
        match extractSmapiMethod a with
        | "getMetadata" -> Success(a)
        | _ -> Failure(sprintf "Method not implemented %s" a)

