namespace Castos

type Episode = {
    Id: EpisodeId
    Guid: string
    FeedId: FeedId
    Url: string
    MediaUrl: string
    Title: string
    Length: int
    ReleaseDate: System.DateTime
}

type Feed = {
    Id: FeedId
    Url: string
    Name: string
    Category: string
    Active: bool
    Episodes: Episode list
}

type FeedListItemRendition = {
    Id: FeedId
    Url: string
    Name: string
    Category: string
    EpidsodesAmount: int
}

type AddFeedRendition = {
    Name: string
    Url: string
    Category: string
}

type AddEpisodeRendition = {
    Title: string
    Guid: string
    Url: string
    ReleaseDate: System.DateTime
    MediaUrl: string
    Length: int
}

module FeedSource =
    let private initialFeedState =
        { Id = FeedId System.Guid.Empty
          Url = ""
          Name = ""
          Category = ""
          Active = false
          Episodes = [] }

    let private apply state event =
        match event with
        | FeedAdded ev -> { state with Id = ev.Id
                                       Url = ev.Url
                                       Name = ev.Name
                                       Category = ev.Category
                                       Active = true }
        | FeedDeleted _ -> { state with Active = false }
        | EpisodeAdded ev ->    let newEpisode = { Id = ev.Id
                                                   FeedId = ev.FeedId
                                                   Guid = ev.Guid
                                                   Url = ev.Url
                                                   MediaUrl = ev.MediaUrl
                                                   Title = ev.Title
                                                   ReleaseDate = ev.ReleaseDate
                                                   Length = ev.Length }
                                { state with Episodes = newEpisode :: state.Episodes }
        | PlaySecondsReported  _ -> state
        | PlayEpisodeStopped _ -> state
        | _ -> failwith("Unknown event")

    let private evolve state events =
        events
        |> List.fold apply state

    let feedId =
        function
        | FeedAdded data -> data.Id
        | FeedDeleted data -> data.Id
        | EpisodeAdded data -> data.FeedId
        | PlaySecondsReported  data -> data.FeedId
        | PlayEpisodeStopped data -> data.FeedId
        | _ -> failwith("Unknown event")

    let feedRendition (feed:Feed) =
        { Id = feed.Id
          Url = feed.Url
          Name = feed.Name
          Category = feed.Category
          EpidsodesAmount = List.length feed.Episodes}

    let getFeeds events =
        events
        |> List.groupBy feedId
        |> List.map ((fun ev -> evolve initialFeedState (snd ev)))
        |> List.filter (fun s  -> s.Active)
        |> List.map feedRendition

    let getFeed events =
        events
        |> evolve initialFeedState

    let addFeed rendition =
        FeedAdded { Id = FeedId (System.Guid.NewGuid())
                    Name = rendition.Name
                    Category = rendition.Category
                    Url = rendition.Url }

    let deleteFeed events =
        let state = getFeed events
        match state.Active with
        | true -> Some (FeedDeleted { Id = state.Id })
        | _ -> None

    let addEpisode (feedId:string) rendition events =
        let state = getFeed events
        let lastEpisodeId = match List.length state.Episodes with
                            | 0 -> 0
                            | _ -> (List.maxBy (fun (e:Episode) -> e.Id) state.Episodes).Id
        EpisodeAdded { Id = lastEpisodeId + 1
                       FeedId = FeedId (System.Guid.Parse(feedId))
                       Guid = rendition.Guid
                       Url = rendition.Url
                       MediaUrl = rendition.MediaUrl
                       Title = rendition.Title
                       Length = rendition.Length
                       ReleaseDate = rendition.ReleaseDate }

    let getCategories (feeds:FeedListItemRendition list) =
        feeds
        |> List.map (fun s -> s.Category)
        |> List.distinct
        |> List.filter (System.String.IsNullOrWhiteSpace >> not)

    let getFeedsOfCategory category (feeds:FeedListItemRendition list) =
        feeds
        |> List.filter (fun s -> s.Category = category)

    let getEpisodes events =
        let feed = getFeed events
        feed.Episodes
