namespace Castos

type Episode = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    MediaUrl: string
    Title: string
    ReleaseDate: System.DateTime
}

type Subscription = {
    Id: SubscriptionId
    Url: string
    Name: string
    Category: string
    Active: bool
    Episodes: Episode list
}

type SubscriptionListItemRendition = {
    Id: SubscriptionId
    Url: string
    Name: string
    Category: string
    EpidsodesAmount: int
}

type AddSubscriptionRendition = {
    Name: string
    Url: string
    Category: string
}

type AddEpisodeRendition = {
    Title: string
    Url: string
    ReleaseDate: System.DateTime
}

module SubscriptionSource =
    let private initialSubscriptionState =
        { Id = System.Guid.Empty
          Url = ""
          Name = ""
          Category = ""
          Active = false
          Episodes = [] }

    let private apply state event =
        match event with
        | SubscriptionAdded ev -> { state with Id = ev.Id
                                               Url = ev.Url
                                               Name = ev.Name
                                               Category = ev.Category
                                               Active = true }
        | SubscriptionDeleted _ -> { state with Active = false }
        | EpisodeAdded ev ->    let newEpisode = { Id = ev.Id
                                                   SubscriptionId = ev.SubscriptionId
                                                   MediaUrl = ev.MediaUrl
                                                   Title = ev.Title
                                                   ReleaseDate = ev.ReleaseDate }
                                { state with Episodes = newEpisode :: state.Episodes }
        | _ -> failwith("Unknown event")

    let private evolve state events =
        events
        |> List.fold apply state

    let subscriptionId =
        function
        | SubscriptionAdded data -> data.Id
        | SubscriptionDeleted data -> data.Id
        | EpisodeAdded data -> data.SubscriptionId
        | _ -> failwith("Unknown event")

    let subscriptionRendition (subscription:Subscription) =
        { Id = subscription.Id
          Url = subscription.Url
          Name = subscription.Name
          Category = subscription.Category
          EpidsodesAmount = List.length subscription.Episodes}

    let getSubscriptions events =
        events
        |> List.groupBy subscriptionId
        |> List.map ((fun ev -> evolve initialSubscriptionState (snd ev)) >> subscriptionRendition)

    let getSubscription events =
        events
        |> evolve initialSubscriptionState

    let addSubscription rendition =
        (StreamVersion 0, SubscriptionAdded { Id = System.Guid.NewGuid()
                                              Name = rendition.Name
                                              Category = rendition.Category
                                              Url = rendition.Url })

    let deleteSubscription (version, events) =
        let state = getSubscription events
        match state.Active with
        | true -> ok (version, SubscriptionDeleted { Id = state.Id })
        | _ -> fail (NotFound "")

    let addEpisode (subscriptionId:string) rendition (version, events) =
        let state = getSubscription events
        let lastEpisodeId = match List.length state.Episodes with
                            | 0 -> 0
                            | _ -> (List.maxBy (fun (e:Episode) -> e.Id) state.Episodes).Id
        ok ((version, EpisodeAdded { Id = lastEpisodeId + 1
                                     SubscriptionId = SubscriptionId subscriptionId
                                     MediaUrl = rendition.Url
                                     Title = rendition.Title
                                     ReleaseDate = rendition.ReleaseDate }))

    let getCategories events =
        getSubscriptions events
        |> List.map (fun s -> s.Category)
        |> List.distinct
        |> List.filter (System.String.IsNullOrWhiteSpace >> not)

    let getSubscriptionsOfCategory category events =
        getSubscriptions events
        |> List.filter (fun s -> s.Category = category)

    let getEpisodes events =
        let subscription = getSubscription events
        subscription.Episodes