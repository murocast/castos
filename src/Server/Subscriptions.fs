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

    let addSubscription feedId userId events =
        let existing = getSubscription events feedId
        match existing with
        | Some _ -> failwithf "Subscription for feed  already exists"
        | None -> Subscribed { FeedId = feedId
                               UserId = userId
                               Timestamp = DateTime.Now }

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
        sprintf "sub-%A" userId

    let private storeSubscriptionEvent eventStore event =
        storeEvent eventStore (userId >> subscriptionStreamId) event

    let private subscriptionEvents eventStore userId =
        let (events, _) = getAllEventsFromStreamById eventStore (subscriptionStreamId userId)
        events

    let private feedExists eventStore feedId =
        getFeedComposition eventStore (feedId.ToString())

    let addSubscriptionComposition eventStore rendition user =
        let result = feedExists eventStore rendition.FeedId
        match result with
        | Success _ -> let events = subscriptionEvents eventStore (user.Id)
                       let added = addSubscription rendition.FeedId (user.Id) events
                       storeSubscriptionEvent eventStore added
                       ok ("added subscription")
        | Failure m -> fail m

    let getSubscriptionsComposition eventStore user =
        let evs = subscriptionEvents eventStore user.Id
        ok (getSubscriptions evs)

    let subscriptionsRouter eventStore = router {
        pipe_through authorize
        get "" (processAuthorizedAsync getSubscriptionsComposition eventStore)
        post "" (processDataAuthorizedAsync addSubscriptionComposition eventStore)
    }