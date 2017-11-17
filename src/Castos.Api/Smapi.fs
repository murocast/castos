namespace Castos

open Smapi
open Smapi.Respond
open Smapi.GetLastUpdate

open FSharp.Data
open System.Text.RegularExpressions

open Podcasts
open SubscriptionCompositions

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
        if (m.Success) then
          Some (System.Guid.Parse(m.Groups.[1].Value))
        else
          None

    let (|MediaMetadataId|_|) str =
        let idPattern = "(.+)___(d+)"
        let m = Regex.Match(str, idPattern)
        if(m.Success) then Some ((System.Guid.Parse(m.Groups.[1].Value)), System.Int32.Parse(m.Groups.[2].Value)) else None

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
    let getCategories eventStore =
        let result = getCategoriesComposition eventStore
        match result with
        | Success (categories) -> categories
                                  |> List.map (fun (category) -> MediaCollection { Id = "__category_" + category
                                                                                   ItemType = Collection
                                                                                   Title = category
                                                                                   CanPlay = false })
                                  |> Seq.ofList
        | _ -> failwith("bla")

    let getPodcastsOfCategory eventStore c =
        let result = getSubscriptionsOfCategoryComposition eventStore c
        match result with
        | Success (subscriptions) -> subscriptions
                                     |> List.map (fun p -> MediaCollection { Id = "__podcast_" + string p.Id
                                                                             ItemType = Collection
                                                                             Title = p.Name
                                                                             CanPlay = false })
                                     |> Seq.ofList
        | _ -> failwith("bla")

    let getPodcast podcasts pname =
        podcasts
        |> List.ofSeq
        |> List.pick (fun p -> if (p.Name = pname) then Some p else None)

    let getEpisodesOfPodcast eventStore id =
        let result = getEpisodesOfSubscriptionComposition eventStore id
        match result with
        | Success (episodes) -> episodes
                                |> List.sortByDescending (fun e -> e.Id)
                                |> List.map (fun e -> MediaMetadata { Id = sprintf "%A___%i" e.SubscriptionId e.Id
                                                                      ItemType = Track
                                                                      Title = e.Title
                                                                      MimeType = "audio/mp3"
                                                                      ItemMetadata = TrackMetadata { Artist = "Artist"
                                                                                                     Duration = 100 //int e.Length.TotalSeconds
                                                                                                     CanResume = true }})
                                |> List.truncate 100
                                |> Seq.ofList
        | _ -> failwith("bla")

    let processGetMetadata eventStore (s:GetMetadataRequest.Envelope) =
        let id = s.Body.GetMetadata.Id
        let items = match id with
                    | RootId -> getRootCollections
                    | LibraryId -> getCategories eventStore
                    | Category c -> getPodcastsOfCategory eventStore c
                    | Podcast p -> getEpisodesOfPodcast eventStore p
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

    let lastPlayEpisodeStopped ls =
        let filteredLs = List.filter (fun x -> match x with
                                               | PlaySecondsReported data -> true
                                               | PlayEpisodeStopped data -> true
                                               | _ -> false) ls
        match List.isEmpty filteredLs with
        | true -> None
        | false -> Some (List.reduce (fun _ i -> i) filteredLs)

    let processGetMediaURI eventstore s httpBasePath =
        let req = GetMediaURIRequest.Parse s
        let id = req.Body.GetMediaUri.Id
        let episode = match id with
                      | MediaMetadataId (subscriptionId, podcastId) -> getEpisodeOfSubscriptionComposition eventstore subscriptionId podcastId
                      | _ -> failwithf "Wrong Id: %s" id

        let path = episode.MediaUrl
        //let encodedPath = path.Replace(" ", "%20")

        let position = match eventstore.GetEvents (StreamId id) with
                       | Success (_ , events) -> match lastPlayEpisodeStopped events with
                                                 | Some (PlaySecondsReported data) -> Some data.Position
                                                 | Some (PlayEpisodeStopped data) -> Some data.Position
                                                 | _ -> None
                       | _ -> None

        let response = Smapi.Respond.getMediaUriResponse path id position
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