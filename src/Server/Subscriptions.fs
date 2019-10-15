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

    let addSubscription feedId userId (version, events) =
        let existing = getSubscription events feedId
        match existing with
        | Some _ -> failwithf "Subscription for feed  already exists"
        | None -> ok (version, Subscribed { FeedId = feedId
                                            UserId = userId
                                            Timestamp = DateTime.Now })

module SubscriptionCompositions =
    open Subscriptions
    open Saturn
    open Castos.Auth
    open Castos.Http
    open Castos.FeedCompositions

    type AddSubscribeRendition = {
        FeedId: FeedId
    }

    let private subscriptionStreamId userId =
        StreamId (sprintf "sub-%A" userId)

    let private storeSubscriptionEvent eventStore (version, event) =
        let streamId event = subscriptionStreamId (userId event)
        eventStore.SaveEvents (streamId event) version [event]

    let private subscriptionEvents eventStore userId =
        eventStore.GetEvents (subscriptionStreamId userId)

    let private feedExists eventStore feedId =
        getFeedComposition eventStore (feedId.ToString())

    let addSubscriptionComposition eventStore rendition user =
        let result = feedExists eventStore rendition.FeedId
        match result with
        | Success _ -> let result = subscriptionEvents eventStore user.Id
                                    >>= (addSubscription rendition.FeedId (user.Id))
                                    >>= storeSubscriptionEvent eventStore
                       match result with
                       | Success _ -> ok ("added subscription")
                       | Failure m -> fail m
        | Failure m -> fail m

    let getSubscriptionsComposition eventStore user =
        let result = subscriptionEvents eventStore user.Id
        match result with
        | Success (_ , evs) -> ok (getSubscriptions evs)
        | Failure m -> fail m

    let subscriptionsRouter eventStore = router {
        pipe_through authorize
        get "" (processAuthorizedAsync getSubscriptionsComposition eventStore)
        post "" (processDataAuthorizedAsync addSubscriptionComposition eventStore)
    }