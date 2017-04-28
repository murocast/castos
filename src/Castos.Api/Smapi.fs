namespace Castos

open Smapi
open Smapi.Respond
open Smapi.GetLastUpdate

open FSharp.Data
open System.Text.RegularExpressions

open Podcasts

type SmapiMethod =
    | GetMetadata of string
    | GetMediaMetadata of string
    | GetMediaURI of string
    | GetLastUpdate of string
    | GetExtendedMetadata of string
    | GetExtendedMetadataText of string
    | ReportPlayStatus of string
    | ReportPlaySeconds of string
    | SetPlayedSeconds of string

module Smapi =
    [<Literal>]
    let RootId = "root"
    [<Literal>]
    let LibraryId = "library"
    [<Literal>]
    let CurrentId = "current"
    [<Literal>]
    let RecentId = "recent"

    type GetMetadataRequest = XmlProvider<"Samples/GetMetadataRequest.xml">
    type GetMediaMetadataRequest = XmlProvider<"Samples/GetMediaMetadataRequest.xml">
    type GetMediaURIRequest = XmlProvider<"Samples/GetMediaURIRequest.xml">
    type GetLastUpdateRequest = XmlProvider<"Samples/GetLastUpdateRequest.xml">
    type GetExtendedMetadataRequest = XmlProvider<"Samples/getExtendedMetadataRequest.xml">
    type GetExtendedMetadataTextRequest = XmlProvider<"Samples/GetExtendedMetadataTextRequest.xml">
    type ReportPlaySecondsRequest = XmlProvider<"Samples/ReportPlaySecondsRequest.xml">
    type ReportPlayStatusRequest = XmlProvider<"Samples/ReportPlayStatusRequest.xml">
    type SetPlayedSecondsRequest = XmlProvider<"Samples/SetPlayedSecondsRequest.xml">
    
    let extractSmapiMethod (m:string) =
        m.Trim('"').[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let (|Category|_|) str =
        let categoryPattern = "__category_(.*)"
        let m = Regex.Match(str, categoryPattern)
        if (m.Success) then Some m.Groups.[1].Value else None

    let (|Podcast|_|) str =
        let podcastPattern = "__podcast_(.*)"
        let m = Regex.Match(str, podcastPattern)
        if (m.Success) then Some m.Groups.[1].Value else None

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
        |> Seq.ofList
    let getCategories podcasts =
        podcasts
        |> Seq.groupBy (fun x -> x.Category)
        |> Seq.map (fun (category, _) -> MediaCollection { Id = "__category_" + category
                                                           ItemType = Collection
                                                           Title = category
                                                           CanPlay = false })

    let getPodcastsOfCategory podcasts c =
        podcasts
        |> Seq.where (fun p -> p.Category = c)
        |> Seq.map (fun p -> MediaCollection { Id = "__podcast_" + p.Name
                                               ItemType = Collection
                                               Title = p.Name
                                               CanPlay = false })

    let getPodcast podcasts pname =
        podcasts
        |> List.ofSeq
        |> List.pick (fun p -> if (p.Name = pname) then Some p else None)

    let getEpisodesOfPodcast podcasts pname =
        let podcast = getPodcast podcasts pname
        podcast.Episodes
        |> List.sortByDescending (fun e -> e.Name)
        |> List.map (fun e -> MediaMetadata { Id = e.Id
                                              ItemType = Track
                                              Title = e.Name
                                              MimeType = "audio/mp3"
                                              ItemMetadata = TrackMetadata { Artist = "Artist"
                                                                             Duration = int e.Length.TotalSeconds
                                                                             CanResume = true }})
        |> List.truncate 100
        |> Seq.ofList

    let processGetMetadata podcasts (s:GetMetadataRequest.Envelope) =
        let id = s.Body.GetMetadata.Id
        let items = match id with
                    | RootId -> getRootCollections
                    | LibraryId -> getCategories podcasts
                    | Category c -> getPodcastsOfCategory podcasts c
                    | Podcast p -> getEpisodesOfPodcast podcasts p
                    | CurrentId
                    | RecentId
                    | _ -> failwithf "unknown id %s" id

        let response = getMetadataResponse items
        ok response

    let processGetMediaMetadata podcasts (s:GetMediaMetadataRequest.Envelope) =
        let id = s.Body.GetMediaMetadata.Id
        let splitted = id.Split([|"___"|], System.StringSplitOptions.RemoveEmptyEntries)
        let podcast = getPodcast podcasts splitted.[1]
        let e = podcast.Episodes
                      |> List.pick (fun e -> if (e.Id = id) then Some e else None)
        let metadata = { Id = id
                         ItemType = Track
                         Title = e.Name
                         MimeType = "audio/mp3"
                         ItemMetadata = TrackMetadata { Artist = "Artist"
                                                        Duration = int e.Length.TotalSeconds
                                                        CanResume = true  }}
        let response = Smapi.Respond.getMediaMetadataRepnose metadata
        ok response

    let processGetMediaURI s httpBasePath=
        let req = GetMediaURIRequest.Parse s
        let id = req.Body.GetMediaUri.Id        
        let path = httpBasePath + id
        let encodedPath = path.Replace(" ", "%20")
        let response = Smapi.Respond.getMediaUriResponse encodedPath
        ok response

    let processGetLastUpdate s =
        let result = { AutoRefreshEnabled = false
                       Catalog = (string 4321)
                       Favorites = (string 4321)
                       PollIntervall = 500 }
        ok (toLastUpdateXml result)

    let processReportPlaySecondsRequest eventstore s =
        let req = ReportPlaySecondsRequest.Parse s
        let id = req.Body.ReportPlaySeconds.Id
        let position = req.Body.ReportPlaySeconds.OffsetMillis

        let ev = PlaySecondsReported { Id = id
                                       Position = position }

        let version = match eventstore.GetEvents (StreamId id) with
                      | Success (version, _) -> version
                      | _ -> StreamVersion 0
        eventstore.SaveEvents (StreamId id) version [ev]

    let processGetExtendedMetadata s =
        let req = GetExtendedMetadataRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadataText s =
        let req = GetExtendedMetadataTextRequest.Parse s
        failwith "TODO"