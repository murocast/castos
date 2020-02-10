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

    let rec getEventsFromEventRead (events:EventRead<'a, 'b> list) =
        match events with
        | e :: rest -> e.Data :: getEventsFromEventRead rest
        | [ ] -> [ ]

    let getAllEventsFromStreamById (store:CosmoStore.EventStore<'a, 'b>) streamId =
        AllEvents
        |> store.GetEvents (streamId)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> getEventsFromEventRead

    let rec getAllEventsFromStreams (store:CosmoStore.EventStore<'a, 'b>) (streams:Stream<'b> list) =
        match streams with
        | s::rest -> let evs = getAllEventsFromStreamById store s.Id
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

    let getFeedStreamId id =
        (StreamId(sprintf "feed-%s" id))

    let feedEvents eventStore id =
        eventStore.GetEvents (getFeedStreamId id)

    let private storeFeedEvent eventStore (version, event) =
        let streamId event = StreamId (sprintf "feed-%A" (feedId event))
        eventStore.SaveEvents (streamId event) version [event]

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

    let getCategoriesComposition eventStore =
        let events = allFeedsEvents eventStore
        ok (getCategories events)

    let addEpisodeComposition eventStore feedId rendition =
        let events = getAllEventsFromStreamById eventStore feedId
        let added = addEpisode feedId rendition (0, events) //TODO Version

        createEvent (snd added)
        |> appendEvent eventStore feedId
        |> ignore

        ok ("added episode")

    let addFeedComposition eventStore rendition =
        let (version, event) = addFeed rendition
        let read = createEvent event
                   |> appendEvent eventStore (feedStreamId event)

        ok ("added feed") //TODO: Error Handlin!!
        // match result with
        // | Success _ -> ok ("added feed")
        // | Failure m -> fail m

    let deleteFeedComposition eventStore id =
        let result = feedEvents eventStore id
                        >>= deleteFeed
                        >>= storeFeedEvent eventStore
        match result with
        | Success _ -> ok (sprintf "Deleted %s" id)
        | Failure m -> fail m

    let getFeedsComposition eventStore =
        //let result = allFeedsEvents eventStore

        // match result with
        // | Success (_, events) -> ok (getFeeds events)
        // | _ -> failwith "bla"
        let events = allEventsFromStreamsStartsWith eventStore "feed-"
        ok (getFeeds events)

    let getFeedComposition eventStore id =
        let result = feedEvents eventStore id
        match result with
        | Success (_, events) -> ok (getFeed events)
        | _ -> failwith "stream not found"

    let getFeedsOfCategoryComposition eventStore category =
        let events = allFeedsEvents eventStore
        ok (getFeedsOfCategory category events)

    let getEpisodesOfFeedComposition eventStore id =
        let events = getAllEventsFromStreamById eventStore id
        ok (getEpisodes events)

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

        getf "/%s/episodes/%i" (fun (feedId, episodeId) -> text (sprintf "Metadata of Episode %i of feed %A" episodeId feedId)) //TODO

        getf "/%s/episodes" (fun id -> processAsync (fun eventStore -> getEpisodesOfFeedComposition eventStore id) eventStore )
        postf "/%s/episodes" (fun id -> processDataAsync (fun eventStore -> addEpisodeComposition eventStore id) eventStore )
    }