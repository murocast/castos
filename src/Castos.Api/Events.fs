[<AutoOpen>]
module Castos.Events

open Castos.Podcasts

type EpisodeEventData =
    | PlaySecondsReported of PlaySecondsReported
    | PlayEpisodeStopped of PlayEpisodeStopped
and PlaySecondsReported = {
        Id: PodcastId
        Position: int }
and PlayEpisodeStopped = {
        Id: PodcastId
        Position: int }

type SubscriptionEventData =
    | SubscriptionAdded of SubscriptionAdded
    | SubscriptionDeleted of SubscriptionDeleted
and SubscriptionAdded = {
    Id: SubscriptionId
    Url: string 
    Name: string }
and SubscriptionDeleted = { Id: SubscriptionId }
and SubscriptionId = System.Guid

type Error =
    | NotImplemented of string
    | VersionConflict of string