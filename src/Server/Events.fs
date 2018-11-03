[<AutoOpen>]
module Castos.Events

type CastosEventData =
    //Stream per subscription
    | SubscriptionAdded of SubscriptionAdded
    | SubscriptionDeleted of SubscriptionDeleted
    | EpisodeAdded of EpisodeAdded

    //Stream per user
    | Subscribed
    | SubscriptionCanceled
    | PlayEpisodeStarted of PlayEpisodeStarted
    | PlayEpisodeStopped of PlayEpisodeStopped
    | PlaySecondsReported of PlaySecondsReported

and SubscriptionAdded = {
    Id: SubscriptionId
    Url: string
    Name: string
    Category: string }
and SubscriptionDeleted = { Id: SubscriptionId }
and EpisodeAdded = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    Guid: string
    Url: string
    MediaUrl: string
    Title: string
    Length: int
    ReleaseDate: System.DateTime
}
and PlayEpisodeStarted = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    TimeStamp: System.DateTime
    Position: int
    Player: string
}
and PlaySecondsReported = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    Position: int }
and PlayEpisodeStopped = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    Position: int }
and Subscribed = {
    SubscriptionId: SubscriptionId
}
and SubscriptionCanceled = {
    SubscriptionId: SubscriptionId
}
and SubscriptionId = System.Guid
and EpisodeId = int

type Error =
    | NotImplemented of string
    | VersionConflict of string
    | NotFound of string
