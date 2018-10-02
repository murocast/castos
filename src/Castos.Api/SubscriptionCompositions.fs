namespace Castos

open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful

open Castos
open Castos.ErrorHandling
open SubscriptionSource

module SubscriptionCompositions =
    let private rawFormString x = System.Text.Encoding.UTF8.GetString x.request.rawForm

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

    let private processAsync f eventStore =
        fun context ->
            async{
                let result = f eventStore
                return! match result with
                        | Success (a) -> OK (mkjson a) context
                        | Failure (_) -> BAD_REQUEST "Error" context
            }

    let private processFormAsync f eventStore =
        fun context ->
            async{
                let data = rawFormString context
                let result = f eventStore data
                return! match result with
                        | Success (a) -> OK (mkjson a) context
                        | Failure (_) -> BAD_REQUEST "Error" context
            }

    let private storeSubscriptionEvent eventStore (version, event) =
        let streamId event = StreamId (sprintf "subscription-%A" (subscriptionId event))
        eventStore.SaveEvents (streamId event) version [event]

    let getCategoriesComposition eventStore =
        let result = allSubscriptionsEvents eventStore
        match result with
        | Success (_, events) -> ok (getCategories events)
        | _ -> failwith "bla"

    let addEpisodeComposition eventStore subscriptionId form =
        let (rendition:AddEpisodeRendition) = unjson form
        let result = subscriptionEvents eventStore subscriptionId
                        >>= (addEpisode subscriptionId rendition)
                        >>= storeSubscriptionEvent eventStore
        match result with
        | Success _ -> ok ("added episode")
        | Failure m -> fail m

    let addSubscriptionComposition eventStore form =
        //TODO: Validation
        let (rendition:AddSubscriptionRendition) = unjson form
        addSubscription rendition
        |> storeSubscriptionEvent eventStore

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

    let subscriptionRoutes eventStore =
        choose [ path "/api/subscriptions"
                    >=> choose [ GET >=> warbler ( fun _ -> processAsync getSubscriptionsComposition eventStore)
                                 POST >=> warbler( fun _ ->  processFormAsync addSubscriptionComposition eventStore) ]
                 path "/api/subscriptions/categories"
                    >=> GET >=> warbler (fun _ -> processAsync getCategoriesComposition eventStore)
                 pathScan "/api/subscriptions/categories/%s"
                    <| fun (category) -> choose [GET >=> warbler (fun _ -> processAsync (fun eventStore -> getSubscriptionsOfCategoryComposition eventStore category) eventStore)]
                 pathScan "/api/subscriptions/%s/episodes/%i"
                    <| fun (subscriptionId, episodeId) -> choose [ GET >=> OK (sprintf "Metadata of Episode %i of subscription %A" episodeId subscriptionId)]
                 pathScan "/api/subscriptions/%s/episodes"
                    <| fun id -> choose [ GET >=> warbler (fun _ -> processAsync (fun eventStore -> getEpisodesOfSubscriptionComposition eventStore id) eventStore)
                                          POST >=> warbler (fun _ -> processFormAsync (fun eventStore -> addEpisodeComposition eventStore id) eventStore) ]
                 pathScan "/api/subscriptions/%s"
                    <| fun id -> choose [ GET >=> warbler ( fun _ -> processAsync (fun eventStore -> getSubscriptionComposition eventStore id) eventStore)
                                          DELETE >=> warbler (fun _ -> processAsync (fun eventStore -> deleteSubscriptionComposition eventStore id) eventStore) ]]