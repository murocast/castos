namespace Castos

open Smapi
open Smapi.Respond
open Smapi.GetLastUpdate

open FSharp.Data
open System.Text.RegularExpressions

open FeedCompositions
open SubscriptionCompositions

type SmapiMethod =
    | GetMetadata of string*(UserId option)
    | GetMediaMetadata of string*(UserId option)
    | GetMediaURI of string*(UserId option)
    | GetLastUpdate of string*(UserId option)
    | GetExtendedMetadata of string*(UserId option)
    | GetExtendedMetadataText of string*(UserId option)
    | ReportPlayStatus of string*(UserId option)
    | ReportPlaySeconds of string*(UserId option)
    | SetPlayedSeconds of string*(UserId option)
    | GetAppLink of string*(UserId option)
    | GetDeviceAuthToken of string*(UserId option)

module Smapi =
    [<Literal>]
    let RootId = "root"
    [<Literal>]
    let LibraryId = "library"
    [<Literal>]
    let CurrentId = "current"
    [<Literal>]
    let RecentId = "recent"

    type Header = XmlProvider<"Samples/Header.xml">
    type GetMetadataRequest = XmlProvider<"Samples/GetMetadataRequest.xml">
    type GetMediaMetadataRequest = XmlProvider<"Samples/GetMediaMetadataRequest.xml">
    type GetMediaURIRequest = XmlProvider<"Samples/GetMediaURIRequest.xml">
    type GetLastUpdateRequest = XmlProvider<"Samples/GetLastUpdateRequest.xml">
    type GetExtendedMetadataRequest = XmlProvider<"Samples/getExtendedMetadataRequest.xml">
    type GetExtendedMetadataTextRequest = XmlProvider<"Samples/GetExtendedMetadataTextRequest.xml">
    type ReportPlaySecondsRequest = XmlProvider<"Samples/ReportPlaySecondsRequest.xml">
    type ReportPlayStatusRequest = XmlProvider<"Samples/ReportPlayStatusRequest.xml">
    type SetPlayedSecondsRequest = XmlProvider<"Samples/SetPlayedSecondsRequest.xml">
    type GetAppLinkRequest = XmlProvider<"Samples/GetAppLink.xml">
    type GetDeviceAuthTokenRequest = XmlProvider<"Samples/GetDeviceAuthToken.xml">

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
        let idPattern = "([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})___([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})"
        let m = Regex.Match(str, idPattern)
        if(m.Success) then Some ((System.Guid.Parse(m.Groups.[1].Value)), System.Guid.Parse(m.Groups.[2].Value)) else None

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

    let private getPlayEpisodeStreamId userId feedId episodeId =
            sprintf "PlayEpisode__%A__%A__%A" userId feedId episodeId

    let getUserFromHeader (db:Database.DatabaseConnection) xml =
        let header = Header.Parse xml
        try
            let loginToken = header.Header.Credentials.LoginToken
            let token = loginToken.Token
            let householdId = loginToken.HouseholdId
            let existingToken = db.GetAuthToken token householdId
            match existingToken with
            | None -> None
            | Some t -> Some t.UserId
        with
            | _ -> None

    let getCategories eventStore userId =
        let result = SubscriptionCompositions.getSubscriptionsCategoriesComposition eventStore userId
        match result with
        | Success (categories) -> categories
                                  |> List.sort
                                  |> List.map (fun (category) -> MediaCollection { Id = "__category_" + category
                                                                                   ItemType = Collection
                                                                                   Title = category
                                                                                   CanPlay = false })
                                  |> Seq.ofList
        | _ -> failwith("bla")

    let getFeedIdId g =
        sprintf "__feed_%A" g

    let getPodcastsOfCategory eventStore userId c =
        let result = getPodcastsOfCategoriesForUser eventStore userId c
        match result with
        | Success (feeds) -> feeds
                                     |> List.sortBy (fun s -> s.Name)
                                     |> List.map (fun p -> MediaCollection { Id = getFeedIdId p.Id
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
                                |> List.map (fun e -> MediaMetadata { Id = sprintf "%A___%A" e.FeedId e.Id
                                                                      ItemType = Track
                                                                      Title = e.Title
                                                                      MimeType = "audio/mp3"
                                                                      ItemMetadata = TrackMetadata { Artist = "Artist"
                                                                                                     Duration = e.Length
                                                                                                     CanResume = true }})
                                |> List.truncate 100
                                |> Seq.ofList
        | _ -> failwith("bla")

    let processGetMetadata eventStore userId (s:GetMetadataRequest.Envelope) =
        let id = s.Body.GetMetadata.Id
        let items = match id with
                    | RootId -> getRootCollections
                    | LibraryId -> getCategories eventStore userId
                    | Category c -> getPodcastsOfCategory eventStore userId c
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

    let processGetMediaURI eventstore s userId =
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

        let (events, _) = getAllEventsFromStreamById eventstore (getPlayEpisodeStreamId userId episode.FeedId episode.Id)
        let position = match lastPlayEpisodeStopped events with
                       | IsNeededPlaySecondsReported episode.Id position -> Some position
                       | IsNeededPlayEpisodeStopped episode.Id position -> Some position
                       | _ -> None

        let response = Smapi.Respond.getMediaUriResponse path id position
        ok response

    let processGetLastUpdate s =
        let result = { AutoRefreshEnabled = false
                       Catalog = (string 4321)
                       Favorites = (string 4321)
                       PollIntervall = 500 }
        ok (toLastUpdateXml result)

    let processReportPlaySecondsRequest eventstore s u =
        let req = ReportPlaySecondsRequest.Parse s
        let id = req.Body.ReportPlaySeconds.Id
        let position = req.Body.ReportPlaySeconds.OffsetMillis

        //FeedId is SubscriptionId? Which user?
        let (episodeId, feedId) =
            match id with
            | MediaMetadataId (feedId, episodeId) -> (episodeId, feedId)
            | _ -> failwithf "unknown Id for play seconds reported: %s" id

        let streamId = getPlayEpisodeStreamId u feedId episodeId
        let ev = PlaySecondsReported { Id = episodeId
                                       FeedId = feedId
                                       Position = position }
        storeEvent eventstore (fun _ -> streamId) ev

    let processGetAppLink baseUrl (db:Database.DatabaseConnection) s =
        let req = GetAppLinkRequest.Parse s
        let houseHoldId = req.Body.GetAppLink.HouseholdId
        let id = System.Guid.NewGuid()
        let request = { Id = id
                        HouseholdId = houseHoldId
                        LinkCode = (System.Guid.NewGuid())
                        UserId = None
                        Created = System.DateTime.Now
                        Used = None } : Database.AuthRequest

        db.AddAuthRequest request

        let loginFormUrl = (sprintf "%s/?linkcode=%A&householdid=%s" baseUrl request.LinkCode request.HouseholdId)
        let response = Smapi.Respond.getAppLinkResponse loginFormUrl (string request.LinkCode)
        ok response

    let linkcodeFault() =
        Smapi.Respond.getFault "Client.NOT_LINKED_RETRY" "Access token not found, retry" "Retry token request." "5"

    let processGetDeviceAuthTokenRequest (db:Database.DatabaseConnection) s =
        let req = GetDeviceAuthTokenRequest.Parse s
        let householdId = req.Body.GetDeviceAuthToken.HouseholdId
        let linkCode = req.Body.GetDeviceAuthToken.LinkCode

        let found = db.GetAuthRequestByLinkToken linkCode householdId
        match found with
        | None -> ok (linkcodeFault())
        | Some r ->
            match r.UserId with
            | None -> ok (linkcodeFault())
            | Some u ->
                    let updatedReq = { r with Used = Some System.DateTime.Now }
                    db.UpdateAuthRequest updatedReq |> ignore

                    let authToken = { Id = System.Guid.NewGuid()
                                      HouseholdId = householdId
                                      Token = string (System.Guid.NewGuid())
                                      PrivateKey = "no refresh yet"
                                      UserId = r.UserId |> Option.get
                                      Created = System.DateTime.Now } : Database.AuthToken
                    db.AddAuthToken authToken

                    let response = Smapi.Respond.processGetDeviceAuthTokenResponse authToken.Token authToken.PrivateKey
                    ok response

    let processGetExtendedMetadata s =
        let req = GetExtendedMetadataRequest.Parse s
        failwith "TODO"

    let processGetExtendedMetadataText s =
        let req = GetExtendedMetadataTextRequest.Parse s
        failwith "TODO"
