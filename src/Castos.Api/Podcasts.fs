namespace Castos.Podcasts

open System

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
          Current : PendingEpisode
          Epiosdes : Episode list }
