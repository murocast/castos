namespace Castos

open Giraffe
open Saturn

open Castos
open Castos.Http
open FeedSource

module FeedCompositions =
    open Castos

    let private allFeedsEvents eventStore =
        eventStore.GetEvents (StreamId("$ce-feed"))

    let private allFeedAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-feeds"))

    let private allEpisodeAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-feeds"))

    let getFeedStreamId id =
        (StreamId(sprintf "feed-%s" id))

    let feedEvents eventStore id =
        eventStore.GetEvents (getFeedStreamId id)

    let private storeFeedEvent eventStore (version, event) =
        let streamId event = StreamId (sprintf "feed-%A" (feedId event))
        eventStore.SaveEvents (streamId event) version [event]

    let getCategoriesComposition eventStore =
        let result = allFeedsEvents eventStore
        match result with
        | Success (_, events) -> ok (getCategories events)
        | _ -> failwith "bla"

    let addEpisodeComposition eventStore feedId rendition =
        let result = feedEvents eventStore feedId
                        >>= (addEpisode feedId rendition)
                        >>= storeFeedEvent eventStore
        match result with
        | Success _ -> ok ("added episode")
        | Failure m -> fail m

    let addFeedComposition eventStore rendition =

        let result = addFeed rendition
                     |> storeFeedEvent eventStore
        match result with
        | Success _ -> ok ("added feed")
        | Failure m -> fail m

    let deleteFeedComposition eventStore id =
        let result = feedEvents eventStore id
                        >>= deleteFeed
                        >>= storeFeedEvent eventStore
        match result with
        | Success _ -> ok (sprintf "Deleted %s" id)
        | Failure m -> fail m

    let getFeedsComposition eventStore =
        let result = allFeedsEvents eventStore
        match result with
        | Success (_, events) -> ok (getFeeds events)
        | _ -> failwith "bla"

    let getFeedComposition eventStore id =
        let result = feedEvents eventStore id
        match result with
        | Success (_, events) -> ok (getFeed events)
        | _ -> failwith "stream not found"

    let getFeedsOfCategoryComposition eventStore category =
        let result = allFeedsEvents eventStore
        match result with
        | Success (_, events) -> ok (getFeedsOfCategory category events)
        | _ -> failwith "bla"

    let getEpisodesOfFeedComposition eventStore id =
        let result = feedEvents eventStore (string id)
        match result with
        | Success (_, events) -> ok (getEpisodes events)
        | _ -> failwith "bla"

    let getEpisodeOfFeedComposition eventStore feedId episodeId =
        let result = feedEvents eventStore (string feedId)
        match result with
        | Success (_, events) -> (getEpisodes events)
                                 |> List.find (fun x -> x.Id = episodeId)
        | _ -> failwith("bla")

    let feedsRouter eventStore = router {
        get ""  (processAsync getFeedsComposition eventStore)
        post ""  (processDataAsync addFeedComposition eventStore)

        get "/categories" (processAsync getCategoriesComposition eventStore)
        getf "/categories/%s"  (fun category -> processAsync (fun eventStore -> getFeedsOfCategoryComposition eventStore category) eventStore)

        getf "/%s/episodes/%i" (fun (feedId, episodeId) -> text (sprintf "Metadata of Episode %i of feed %A" episodeId feedId))

        getf "/%s/episodes" (fun id -> processAsync (fun eventStore -> getEpisodesOfFeedComposition eventStore id) eventStore)
        postf "/%s/episodes" (fun id -> processDataAsync (fun eventStore -> addEpisodeComposition eventStore id) eventStore )
    }