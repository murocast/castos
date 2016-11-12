namespace Castos

open Smapi
open Smapi.Respond
open FSharp.Data

module Smapi =
    [<Literal>]
    let RootId = "root"
    [<Literal>]
    let LibraryId = "library"
    [<Literal>]
    let CurrentId = "current"
    [<Literal>]
    let RecentId = "recent"

    type getMetadataRequest = XmlProvider<"Samples/GetMetadataRequest.xml">
    type getMediaMetadataRequest = XmlProvider<"Samples/GetMediaMetadataRequest.xml">
    type getMediaURIRequest = XmlProvider<"Samples/GetMediaURIRequest.xml">
    type getLastUpdateRequest = XmlProvider<"Samples/GetLastUpdateRequest.xml">
    type getExtendedMetadataRequest = XmlProvider<"Samples/getExtendedMetadataRequest.xml">
    type getExtendedMetadataTextRequest = XmlProvider<"Samples/GetExtendedMetadataTextRequest.xml">

    let extractSmapiMethod (m:string) =
        m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let getRootCollections =
        [ MediaCollection { Id = LibraryId
                            ItemType = Collection
                            Title = "Library"
                            CanPlay = false }
          MediaCollection { Id = CurrentId
                            ItemType = Collection
                            Title = "Current"
                            CanPlay = false }
          MediaCollection { Id = RecentId
                            ItemType = Collection
                            Title = "Recent"
                            CanPlay = false }]

    let processGetMetadata s =
        let req = getMetadataRequest.Parse s
        let items = match req.Body.GetMetadata.Id with
                    | RootId -> getRootCollections
                    | LibraryId
                    | CurrentId
                    | RecentId
                    | _ -> failwith "unknown id"

        let response = getMetadataResponse items
        Success(response)

    let processGetMediaMetadata s =
        let req = getMediaMetadataRequest.Parse s
//        let items = match req.Body.GetMediaMetadata.Id with
//                    | RootId ->
        failwith "TODO"

    let processGetMediaURI s =
        let req = getMediaURIRequest.Parse s
        failwith "TODO"

    let processGetLastUpdate s =
        let req = getLastUpdateRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadata s =
        let req = getExtendedMetadataRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadataTextRequest s =
        let req = getExtendedMetadataTextRequest.Parse s
        failwith "TODO"

    let processSmapiMethod a form =
        match extractSmapiMethod a with
        | "getMetadata" -> processGetMetadata form
        | "getMediaMetadata" -> processGetMediaMetadata form
        | "getMediaURI" -> processGetMediaURI form
        | "getLastUpdate" -> processGetLastUpdate form
        | "getExtendedMetadataRequest" -> processGetExtendedMetadata form
        | "getExtendedMetadataRequestText" -> processGetExtendedMetadataText form
        | _ -> Failure(sprintf "Method not implemented %s" a)

