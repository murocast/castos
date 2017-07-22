namespace Castos

type Subscription = {
    Id: SubscriptionId
    Url: string
    Name: string
    Active: bool
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

    let rec private evolve state events =
        match events with
        | head :: rest -> evolve (apply state head) rest
        | [] -> state

    let addSubscription name url =        
        SubscriptionAdded { Id = System.Guid.NewGuid()
                            Name = name
                            Url = url }

