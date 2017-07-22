[<AutoOpen>]
module Castos.Events

open Castos.Podcasts

type CastosEventData =
    | PlaySecondsReported of PlaySecondsReported
    | PlayEpisodeStopped of PlayEpisodeStopped
    
    | SubscriptionAdded of SubscriptionAdded
    | SubscriptionDeleted of SubscriptionDeleted
and PlaySecondsReported = {
        Id: PodcastId
        Position: int }
and PlayEpisodeStopped = {
        Id: PodcastId
        Position: int }
and SubscriptionAdded = {
    Id: SubscriptionId
    Url: string 
    Name: string }
and SubscriptionDeleted = { Id: SubscriptionId }
and SubscriptionId = System.Guid

type Error =
    | NotImplemented of string
    | VersionConflict of string