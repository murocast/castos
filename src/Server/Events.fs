[<AutoOpen>]
module Castos.Events

open Murocast.Shared.Core.UserAccount.Domain.Queries

type CastosEventData =
    //Stream per feed
    | FeedAdded of FeedAdded
    | FeedDeleted of FeedDeleted
    | EpisodeAdded of EpisodeAdded

    //Stream for all users
    | UserAdded of UserAdded
    | PasswordChanged

    //Stream per user
    | Subscribed of Subscribed
    | Unsubscribed of Unsubscribed

    //per user
    | PlayEpisodeStarted of PlayEpisodeStarted
    | PlayEpisodeStopped of PlayEpisodeStopped
    | PlaySecondsReported of PlaySecondsReported

and FeedAdded = {
    Id: FeedId
    Url: string
    Name: string
    Category: string }
and FeedDeleted = { Id: FeedId }
and EpisodeAdded = {
    Id: EpisodeId
    Episode: int
    FeedId: FeedId
    Guid: string
    Url: string
    MediaUrl: string
    Title: string
    Length: int
    ReleaseDate: System.DateTime
}
and PlayEpisodeStarted = {
    Id: EpisodeId
    FeedId: FeedId
    TimeStamp: System.DateTime
    Position: int
    Player: string
}
and PlaySecondsReported = {
    Id: EpisodeId
    FeedId: FeedId
    Position: int }
and PlayEpisodeStopped = {
    Id: EpisodeId
    FeedId: FeedId
    Position: int }
and Subscribed = {
    FeedId: FeedId
    UserId: UserId
    Timestamp: System.DateTime
}
and Unsubscribed = {
    FeedId: FeedId
    UserId: UserId
    Timestamp: System.DateTime
}
and FeedId = System.Guid
and EpisodeId = System.Guid
and UserAdded = {
    Id : UserId
    Email: string
    PasswordHash: string
    Salt: byte array
}
and PasswordChanged = {
    Id: UserId
    Password: string
}


type Error =
    | NotImplemented of string
    | VersionConflict of string
    | NotFound of string
    | GenericError of string
