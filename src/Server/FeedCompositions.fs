namespace Castos

open Giraffe
open Saturn

open Castos
open Castos.Http
open FeedSource

open CosmoStore

module FeedCompositions =

    let private feedStreamId event = sprintf "feed-%A" (feedId event)

    let rec getAllEventsFromStreams store streams =
        match streams with
        | s::rest -> let (evs, version) = getAllEventsFromStreamById store s.Id
                     evs @ getAllEventsFromStreams store rest
        | [] -> []

    let private allEventsFromStreamsStartsWith store startsWith =
        StreamsReadFilter.StartsWith(startsWith)
        |> store.GetStreams
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> getAllEventsFromStreams store

    let private allFeedsEvents eventStore =
        allEventsFromStreamsStartsWith eventStore "feed-"

    let private getFeedStreamId id =
        sprintf "feed-%O" id

    let private storeFeedEvent eventStore ev =
        storeEvent eventStore feedStreamId ev

    let getCategoriesComposition eventStore =
        let events = allFeedsEvents eventStore
        let feeds = getFeeds events
        ok (getCategories feeds)

    let getCategoriesOfFeedsComposition eventStore feedIds =
        let events = allFeedsEvents eventStore
        let feeds = getFeeds events
                    |> List.filter (fun f -> List.contains (f.Id) feedIds)
        ok (getCategories feeds)

    let addEpisodeComposition eventStore feedId rendition =
        let (_, version) = getAllEventsFromStreamById eventStore (getFeedStreamId feedId)
        let added = addEpisode feedId rendition

        //TODO: Version
        storeFeedEvent eventStore added

        ok ("added episode")

    let addFeedComposition eventStore rendition =
        let event = addFeed rendition
        storeEvent eventStore feedStreamId event

        ok ("added feed")

    let deleteFeedComposition eventStore id =
        let (events, version) = getAllEventsFromStreamById eventStore (getFeedStreamId id)

        let deleted = deleteFeed events
        match deleted with
        | Some ev ->
            storeFeedEvent eventStore ev
        | None -> ()

        ok (sprintf "Deleted %s" id)

    let getFeedsComposition eventStore =
        let events = allEventsFromStreamsStartsWith eventStore "feed-"
        ok (getFeeds events)

    let getFeedComposition eventStore id =
        let (events, _) = getAllEventsFromStreamById eventStore (getFeedStreamId id)
        ok (getFeed events)

    let getFeedsOfCategoryComposition eventStore category =
        let feeds = allFeedsEvents eventStore
                    |> getFeeds
        ok (getFeedsOfCategory category feeds)

    let getEpisodesOfFeedComposition eventStore id =
        let (events, _) = getAllEventsFromStreamById eventStore (getFeedStreamId id)
        ok (getEpisodes events)

    let getEpisodeOfFeedComposition eventStore feedId episodeId =
        let (events, _) = getAllEventsFromStreamById eventStore (getFeedStreamId feedId)

        (getEpisodes events)
        |> List.find (fun x -> x.Id = episodeId)

    let feedsRouter eventStore = router {
        get ""  (processAsync getFeedsComposition eventStore)
        post ""  (processDataAsync addFeedComposition eventStore)

        get "/categories" (processAsync getCategoriesComposition eventStore)
        getf "/categories/%s"  (fun category -> processAsync (fun eventStore -> getFeedsOfCategoryComposition eventStore category) eventStore)

        getf "/%s/episodes/%i" (fun (feedId, episodeId) -> text (sprintf "Metadata of Episode %i of feed %A" episodeId feedId)) //TODO

        getf "/%s/episodes" (fun id -> processAsync (fun eventStore -> getEpisodesOfFeedComposition eventStore id) eventStore )
        postf "/%s/episodes" (fun id -> processDataAsync (fun eventStore -> addEpisodeComposition eventStore id) eventStore )
    }