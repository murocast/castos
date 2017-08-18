[<AutoOpen>]
module Castos.Events

open Castos.Podcasts

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
    Name: string }
and SubscriptionDeleted = { Id: SubscriptionId }
and EpisodeAdded = {
    Id: EpisodeId
    SubscriptionId: SubscriptionId
    MediaUrl: string
    Title: string
    ReleaseDate: System.DateTime
}
and PlayEpisodeStarted = {
    Id: EpisodeId
    TimeStamp: System.DateTime
    Position: int
    Player: string
}
and PlaySecondsReported = {
    Id: PodcastId //TODO: EpisodeId
    Position: int }
and PlayEpisodeStopped = {
    Id: PodcastId //TODO: EpisodeId
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