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

type Error =
    | NotImplemented of string
    | VersionConflict of string