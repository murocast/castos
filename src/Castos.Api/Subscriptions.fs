namespace Castos

type Subscription = {
    Id: SubscriptionId
    Url: string
    Name: string
    Active: bool
}

module SubscriptionSource =

    let addSubscription name url =
        SubscriptionAdded { Id = System.Guid.NewGuid()
                            Name = name
                            Url = url }
