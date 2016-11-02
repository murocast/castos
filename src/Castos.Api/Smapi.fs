namespace Castos

open Smapi
open Smapi.Respond
open FSharp.Data

module Smapi =
    type getMetadataRequest = XmlProvider<"Samples/GetMetadataRequest.xml">

    let extractSmapiMethod (m:string) =
        m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let getRootCollections =
        [ MediaCollection { Id = "library"
                            ItemType = Collection
                            Title = "Library"
                            CanPlay = false }
          MediaCollection { Id = "current"
                            ItemType = Collection
                            Title = "Current"
                            CanPlay = false }
          MediaCollection { Id = "recent"
                            ItemType = Collection
                            Title = "Recent"
                            CanPlay = false }]

    let processGetMetadata s =
        let req = getMetadataRequest.Parse s
        let items = match req.Body.GetMetadata.Id with
                    | "root" -> getRootCollections
                    | _ -> failwith "unknown id"

        let response = getMetadataResponse items
        Success(response)

    let processSmapiMethod a form =
        match extractSmapiMethod a with
        | "getMetadata" -> processGetMetadata form
        | _ -> Failure(sprintf "Method not implemented %s" a)

