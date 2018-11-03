namespace Castos


open Giraffe
open Saturn
open FSharp.Control.Tasks.V2

open Castos
open Castos.ErrorHandling
open SubscriptionSource
open Microsoft.AspNetCore.Http

module SubscriptionCompositions =
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
    let getJson<'a> (ctx: HttpContext) =
        ctx.BindJsonAsync<'a>()

    let private processAsync f eventStore =
        fun next ctx ->
            task {
                let result = f eventStore
                return! match result with
                        | Success (a) -> Successful.OK a next ctx
                        | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
            }

    let private processFormAsync<'a> (f:EventStore<CastosEventData,Error> -> 'a -> Result<string, Error>) eventStore =
        let getJson = getJson<'a>
        fun next ctx ->
            task {
                let! data = getJson ctx
                let result = f eventStore data
                return! match result with
                        | Success (a) -> Successful.OK a next ctx
                        | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
            }

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
        get "/subscriptions"  (processAsync getSubscriptionsComposition eventStore)
        post "/subscriptions"  (processFormAsync<AddSubscriptionRendition> addSubscriptionComposition eventStore)

        get "/subscriptions/categories" (processAsync getCategoriesComposition eventStore)
        getf "/subscriptions/categories/%s"  (fun category -> processAsync (fun eventStore -> getSubscriptionsOfCategoryComposition eventStore category) eventStore)

        getf "/subscriptions/%s/episodes/%i" (fun (subscriptionId, episodeId) -> text (sprintf "Metadata of Episode %i of subscription %A" episodeId subscriptionId))

        getf "/subscriptions/%s/episodes" (fun id -> processAsync (fun eventStore -> getEpisodesOfSubscriptionComposition eventStore id) eventStore)
        postf "/subscriptions/%s/episodes" (fun id -> processFormAsync<AddEpisodeRendition> (fun eventStore -> addEpisodeComposition eventStore id) eventStore )
    }