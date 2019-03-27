namespace Castos

open Giraffe
open Saturn

open Castos
open Castos.Http
open SubscriptionSource

module SubscriptionCompositions =
    open Castos

    let private allSubscriptionsEvents eventStore =
        eventStore.GetEvents (StreamId("$ce-subscription"))

    let private allSubscriptionAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-subscriptions"))

    let private allEpisodeAddedEvents eventStore =
        eventStore.GetEvents (StreamId("latest-subscriptions"))

    let getSubscriptionStreamId id =
        (StreamId(sprintf "subscription-%s" id))

    let subscriptionEvents eventStore id =
        eventStore.GetEvents (getSubscriptionStreamId id)

    let private storeSubscriptionEvent eventStore (version, event) =
        let streamId event = StreamId (sprintf "subscription-%A" (subscriptionId event))
        eventStore.SaveEvents (streamId event) version [event]

    let getCategoriesComposition eventStore =
        let result = allSubscriptionsEvents eventStore
        match result with
        | Success (_, events) -> ok (getCategories events)
        | _ -> failwith "bla"

    let addEpisodeComposition eventStore subscriptionId rendition =
        let result = subscriptionEvents eventStore subscriptionId
                        >>= (addEpisode subscriptionId rendition)
                        >>= storeSubscriptionEvent eventStore
        match result with
        | Success _ -> ok ("added episode")
        | Failure m -> fail m

    let addSubscriptionComposition eventStore rendition =

        let result = addSubscription rendition
                     |> storeSubscriptionEvent eventStore
        match result with
        | Success _ -> ok ("added subscription")
        | Failure m -> fail m

    let deleteSubscriptionComposition eventStore id =
        let result = subscriptionEvents eventStore id
                        >>= deleteSubscription
                        >>= storeSubscriptionEvent eventStore
        match result with
        | Success _ -> ok (sprintf "Deleted %s" id)
        | Failure m -> fail m

    let getSubscriptionsComposition eventStore =
        let result = allSubscriptionsEvents eventStore
        match result with
        | Success (_, events) -> ok (getSubscriptions events)
        | _ -> failwith "bla"

    let getSubscriptionComposition eventStore id =
        let result = subscriptionEvents eventStore id
        match result with
        | Success (_, events) -> ok (getSubscription events)
        | _ -> failwith "stream not found"

    let getSubscriptionsOfCategoryComposition eventStore category =
        let result = allSubscriptionsEvents eventStore
        match result with
        | Success (_, events) -> ok (getSubscriptionsOfCategory category events)
        | _ -> failwith "bla"

    let getEpisodesOfSubscriptionComposition eventStore id =
        let result = subscriptionEvents eventStore (string id)
        match result with
        | Success (_, events) -> ok (getEpisodes events)
        | _ -> failwith "bla"

    let getEpisodeOfSubscriptionComposition eventStore subscriptionId episodeId =
        let result = subscriptionEvents eventStore (string subscriptionId)
        match result with
        | Success (_, events) -> (getEpisodes events)
                                 |> List.find (fun x -> x.Id = episodeId)
        | _ -> failwith("bla")

    let subscriptionsRouter eventStore = router {
        get ""  (processAsync getSubscriptionsComposition eventStore)
        post ""  (processDataAsync addSubscriptionComposition eventStore)

        get "/categories" (processAsync getCategoriesComposition eventStore)
        getf "/categories/%s"  (fun category -> processAsync (fun eventStore -> getSubscriptionsOfCategoryComposition eventStore category) eventStore)

        getf "/%s/episodes/%i" (fun (subscriptionId, episodeId) -> text (sprintf "Metadata of Episode %i of subscription %A" episodeId subscriptionId))

        getf "/%s/episodes" (fun id -> processAsync (fun eventStore -> getEpisodesOfSubscriptionComposition eventStore id) eventStore)
        postf "/%s/episodes" (fun id -> processDataAsync (fun eventStore -> addEpisodeComposition eventStore id) eventStore )
    }