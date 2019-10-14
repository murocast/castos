namespace Castos

open Smapi
open Smapi.Respond
open Smapi.GetLastUpdate

open FSharp.Data
open System.Text.RegularExpressions

open FeedCompositions

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

    let (|Feed|_|) str =
        let feedPattern = "__feed_(.*)"
        let m = Regex.Match(str, feedPattern)
        if (m.Success) then
          Some (System.Guid.Parse(m.Groups.[1].Value))
        else
          None

    let (|MediaMetadataId|_|) str =
        let idPattern = "([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})___(\\d+)"
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
                                  |> List.sort
                                  |> List.map (fun (category) -> MediaCollection { Id = "__category_" + category
                                                                                   ItemType = Collection
                                                                                   Title = category
                                                                                   CanPlay = false })
                                  |> Seq.ofList
        | _ -> failwith("bla")

    let getPodcastsOfCategory eventStore c =
        let result = getFeedsOfCategoryComposition eventStore c
        match result with
        | Success (feeds) -> feeds
                                     |> List.sortBy (fun s -> s.Name)
                                     |> List.map (fun p -> MediaCollection { Id = "__feed_" + string p.Id
                                                                             ItemType = Collection
                                                                             Title = p.Name
                                                                             CanPlay = false })
                                     |> Seq.ofList
        | _ -> failwith("bla")

    let getPodcast podcasts pname =
        podcasts
        |> List.ofSeq
        |> List.pick (fun p -> if (p.Name = pname) then Some p else None)

    let getEpisodesOfFeed eventStore id =
        let result = getEpisodesOfFeedComposition eventStore (string id)
        match result with
        | Success (episodes) -> episodes
                                |> List.sortByDescending (fun e -> e.ReleaseDate)
                                |> List.map (fun e -> MediaMetadata { Id = sprintf "%A___%i" e.FeedId e.Id
                                                                      ItemType = Track
                                                                      Title = e.Title
                                                                      MimeType = "audio/mp3"
                                                                      ItemMetadata = TrackMetadata { Artist = "Artist"
                                                                                                     Duration = e.Length
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
                    | Feed s -> getEpisodesOfFeed eventStore s
                    | CurrentId
                    | RecentId
                    | _ -> failwithf "unknown id %s" id

        let response = getMetadataResponse items
        ok response

    let processGetMediaMetadata eventStore (s:GetMediaMetadataRequest.Envelope) =
        let id = s.Body.GetMediaMetadata.Id
        let e = match id with
                      | MediaMetadataId (feedId, podcastId) -> getEpisodeOfFeedComposition eventStore feedId podcastId
                      | _ -> failwithf "Wrong Id: %s" id
        let metadata = { Id = id
                         ItemType = Track
                         Title = e.Title
                         MimeType = "audio/mp3"
                         ItemMetadata = TrackMetadata { Artist = "Artist"
                                                        Duration = e.Length
                                                        CanResume = true  }}
        let response = Smapi.Respond.getMediaMetadataRepnose metadata
        ok response

    let lastPlayEpisodeStopped ls =
        let filteredLs = List.filter (fun x -> match x with
                                               | PlaySecondsReported _ -> true
                                               | PlayEpisodeStopped _ -> true
                                               | _ -> false) ls
        match List.isEmpty filteredLs with
        | true -> None
        | false -> Some (List.reduce (fun _ i -> i) filteredLs)

    let processGetMediaURI eventstore s =
        let req = GetMediaURIRequest.Parse s
        let id = req.Body.GetMediaUri.Id
        let episode = match id with
                      | MediaMetadataId (feedId, episodeId) -> getEpisodeOfFeedComposition eventstore feedId episodeId
                      | _ -> failwithf "Wrong Id: %s" id

        let path = episode.MediaUrl

        let (|IsNeededPlaySecondsReported|_|) episodeId event =
            match event with
            | Some (PlaySecondsReported data) -> if data.Id = episodeId then Some (data.Position) else None
            | _ -> None

        let (|IsNeededPlayEpisodeStopped|_|) episodeId event =
            match event with
            | Some (PlayEpisodeStopped data) -> if data.Id = episodeId then Some (data.Position) else None
            | _ -> None

        let position = match feedEvents eventstore (string episode.FeedId) with
                       | Success (_ , events) -> match lastPlayEpisodeStopped events with
                                                 | IsNeededPlaySecondsReported episode.Id position -> Some position
                                                 | IsNeededPlayEpisodeStopped episode.Id position -> Some position
                                                 | _ -> None
                       | Failure (e:Error) -> failwithf "Get Events for position faild. Error: %s for Episode %i in Feed %O" (string e) episode.Id episode.FeedId

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

        let (episodeId, feedId) = match id with
                                          | MediaMetadataId (feedId, episodeId) -> (episodeId, feedId)
                                          | _ -> failwithf "unknown Id for play seconds reported: %s" id
        let streamId = (getFeedStreamId (string feedId))
        let version = match eventstore.GetEvents streamId with
                      | Success (version, _) -> version
                      | _ -> StreamVersion 0

        let ev = PlaySecondsReported { Id = episodeId
                                       FeedId = FeedId feedId
                                       Position = position }
        match eventstore.SaveEvents streamId version [ev] with
        | Success _ -> ()
        | Failure (error:Error) -> failwith (string error)

    let processGetExtendedMetadata s =
        let req = GetExtendedMetadataRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadataText s =
        let req = GetExtendedMetadataTextRequest.Parse s
        failwith "TODO"