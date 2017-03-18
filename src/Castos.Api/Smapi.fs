namespace Castos

open Smapi
open Smapi.Respond
open Smapi.GetLastUpdate
open FSharp.Data

open Podcasts

open Chessie.ErrorHandling

type SmapiMethod =
    | GetMetadata of string
    | GetMediaMetadata of string
    | GetMediaURI of string
    | GetLastUpdate of string
    | GetExtendedMetadataRequest of string
    | GetExtendedMetadataRequestText of string

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
        m.Trim('"').[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

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
    let getLibraryCollection podcasts =
        podcasts
        |> Seq.ofList
        |> Seq.groupBy (fun x -> x.Category)
        |> Seq.map (fun x -> MediaCollection { Id = fst x
                                               ItemType = Collection
                                               Title = fst x
                                               CanPlay = false })                                                   
        |> List.ofSeq
    let processGetMetadata (podcasts:Podcast list) (s:getMetadataRequest.Envelope) =
        let items = match s.Body.GetMetadata.Id with
                    | RootId -> getRootCollections
                    | LibraryId -> getLibraryCollection podcasts
                    | CurrentId
                    | RecentId
                    | _ -> failwith "unknown id"

        let response = getMetadataResponse items
        ok response

    let processGetMediaMetadata s =
//        let items = match req.Body.GetMediaMetadata.Id with
//                    | RootId ->
        failwith "TODO"

    let processGetMediaURI s =
        let req = getMediaURIRequest.Parse s
        failwith "TODO"

    let processGetLastUpdate s =
        let result = { AutoRefreshEnabled = false
                       Catalog = (string 4321)
                       Favorites = (string 4321)
                       PollIntervall = 30 }
        ok (toLastUpdateXml result)

    let processGetExtendedMetadata s =
        let req = getExtendedMetadataRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadataText s =
        let req = getExtendedMetadataTextRequest.Parse s
        failwith "TODO"

