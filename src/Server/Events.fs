[<AutoOpen>]
module Castos.Events

type CastosEventData =
    //Stream per feed
    | FeedAdded of FeedAdded
    | FeedDeleted of FeedDeleted
    | EpisodeAdded of EpisodeAdded

    //Stream for all users
    | UserAdded of UserAdded
    | PasswordChanged

    //Stream per user
    | Subscribed
    | FeedCanceled
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
}
and FeedCanceled = {
    FeedId: FeedId
}
and FeedId = System.Guid
and EpisodeId = int
and UserAdded = {
    Id : UserId
    Email: string
    Password: string
}
and PasswordChanged = {
    Id: UserId
    Password: string
}
and UserId = | UserId of System.Guid


type Error =
    | NotImplemented of string
    | VersionConflict of string
    | NotFound of string
    | GenericError of string
