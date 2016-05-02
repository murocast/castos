namespace Castos

open System

[<AutoOpen>]
module Podcasts =
    type Episode =
        { Name : string
          Length : TimeSpan
          Path : string }

    type PendingEpisode =
        { Episode : Episode
          Position : TimeSpan }

    type Podcast =
        { Name : string
          Folder : string
          Category : string
          Current : PendingEpisode option
          Episodes : Episode list }
