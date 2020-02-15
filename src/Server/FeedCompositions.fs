namespace Castos

open Giraffe
open Saturn

open Castos
open Castos.Http
open FeedSource

open Microsoft.FSharp.Reflection

open CosmoStore

module FeedCompositions =
    open Castos

    let private feedStreamId event = sprintf "feed-%A" (feedId event)

    let rec getEventsFromEventRead (lastVersion:int64) (events:EventRead<'a, 'b> list) =
        match events with
        | e :: rest -> let (events, version) = getEventsFromEventRead lastVersion rest
                       ((e.Data :: events), (System.Math.Max(version,lastVersion))) //TODO: Tail recursion??
        | [ ] -> [ ], lastVersion

    let getAllEventsFromStreamById (store:CosmoStore.EventStore<'a, 'b>) streamId =
        AllEvents
        |> store.GetEvents (streamId)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> getEventsFromEventRead 0L

    let rec getAllEventsFromStreams (store:CosmoStore.EventStore<'a, 'b>) (streams:Stream<'b> list) =
        match streams with
        | s::rest -> let (evs, version) = getAllEventsFromStreamById store s.Id
                     evs @ getAllEventsFromStreams store rest
        | [] -> []

    let private allEventsFromStreamsStartsWith (store:CosmoStore.EventStore<'a, 'b>) startsWith =
        StreamsReadFilter.StartsWith(startsWith)
        |> store.GetStreams
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> getAllEventsFromStreams store

    let private allFeedsEvents eventStore =
        allEventsFromStreamsStartsWith eventStore "feed-"

    let private allFeedAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-feeds"))

    let private allEpisodeAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-feeds"))

    let private getFeedStreamId id =
        sprintf "feed-%A" id

    let private getUnionCaseName (x:'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let private createEvent event =
        ({ Id = (System.Guid.NewGuid())
           CorrelationId = None
           CausationId = None
           Name = getUnionCaseName event
           Data = event
           Metadata = None })

    let private appendEvent store stream event =
        store.AppendEvent stream Any event
        |> Async.AwaitTask
        |> Async.RunSynchronously

    //TODO: Version
    let storeEvent eventStore getStreamId ev =
        createEvent (ev)
        |> appendEvent eventStore (getStreamId ev)
        |> ignore

    let private storeFeedEvent eventStore ev =
        storeEvent eventStore feedStreamId ev

    let getCategoriesComposition eventStore =
        let events = allFeedsEvents eventStore
        ok (getCategories events)

    let addEpisodeComposition eventStore feedId rendition =
        let (events, version) = getAllEventsFromStreamById eventStore (getFeedStreamId feedId)
        let added = addEpisode feedId rendition events

        //TODO: Version
        storeFeedEvent eventStore added

        ok ("added episode")

    let addFeedComposition eventStore rendition =
        let event = addFeed rendition
        let read = createEvent event
                   |> appendEvent eventStore (feedStreamId event)

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
        let events = allFeedsEvents eventStore
        ok (getFeedsOfCategory category events)

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