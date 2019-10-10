namespace Castos

module Subscriptions =
    open System

    type Subscription = {
            UserId : UserId
            FeedId : FeedId
            Subscribed : DateTime
            Unsubscribed : DateTime option
            Paused: bool
        }

    type AddSubscribeRendition = {
        UserId: UserId
        FeedId: FeedId
        Timestamp: DateTime
    }

    let private apply state event =
        match event with
        | Subscribed data -> { UserId = data.UserId
                               FeedId = data.FeedId
                               Subscribed = data.Timestamp
                               Unsubscribed = None
                               Paused = false } :: state
        | Unsubscribed data -> state
                               |> List.map (fun s -> match s.FeedId = data.FeedId with
                                                     | true -> { s with Unsubscribed = Some data.Timestamp }
                                                     | _ -> s)
        | _ -> failwith "unkown event"

    let private evolve state events =
        events
        |> List.fold apply state

    let userId = function
    | Subscribed s -> s.UserId
    | Unsubscribed u -> u.UserId
    | _ -> failwith "unknown subscription event"

    let getSubscriptions events =
        evolve [] events

    let getSubscription events feedId =
        getSubscriptions events
        |> List.tryFind (fun s -> s.FeedId = feedId)

    let addSubscription rendition (version, events) =
        let existing = getSubscription events rendition.FeedId
        match existing with
        | Some _ -> fail "Subscription for feed  already exists"
        | None -> ok (version, Subscribed { FeedId = rendition.FeedId
                                            UserId = rendition.UserId
                                            Timestamp = rendition.Timestamp })

module SubscriptionCompositions =
    open Subscriptions
    open Giraffe
    open Saturn
    open Castos.Auth
    open Castos.Http

    let private subscriptionStreamId userId =
        StreamId (sprintf "sub-%A" userId)

    let private storeSubscriptionEvent eventStore (version, event) =
        let streamId event = subscriptionStreamId (userId event)
        eventStore.SaveEvents (streamId event) version [event]

    let private subscriptionEvents eventStore userId =
        eventStore.GetEvents (subscriptionStreamId userId)

    let addSubscriptionComposition eventStore rendition =
        let result = subscriptionEvents eventStore rendition.UserId
                        >>= (addSubscription rendition)
                        >>= storeSubscriptionEvent eventStore
        match result with
        | Success _ -> ok ("added subscription")
        | Failure m -> fail m

    let subscriptionsRouter eventStore = router {
        pipe_through authorize
        post "" (processDataAsync addSubscriptionComposition eventStore)
    }