namespace Castos

type Subscription = {
    Id: SubscriptionId
    Url: string
    Name: string
    Active: bool
}

type AddSubscriptionRendition = {
    Name: string
    Url: string
}

module SubscriptionSource =
    let private initialSubscriptionState =
        { Id = System.Guid.Empty
          Url = ""
          Name = ""
          Active = false }

    let private apply state event =
        match event with
        | SubscriptionAdded ev -> { state with Id = ev.Id
                                               Url = ev.Url
                                               Name = ev.Name
                                               Active = true }
        | SubscriptionDeleted ev -> { state with Active = false }
        | _ -> failwith("Unknown event")

    let private evolve state events =
        events
        |> List.fold apply state

    let subscriptionId =
        function
        | SubscriptionAdded data -> data.Id
        | SubscriptionDeleted data -> data.Id
        | _ -> failwith("Unknown event")

    let getSubscriptions events =
        events
        |> List.groupBy subscriptionId
        |> List.map (fun ev -> evolve initialSubscriptionState (snd ev))

    let getSubscription events =
        events
        |> evolve initialSubscriptionState

    let addSubscription rendition =
        SubscriptionAdded { Id = System.Guid.NewGuid()
                            Name = rendition.Name
                            Url = rendition.Url }

