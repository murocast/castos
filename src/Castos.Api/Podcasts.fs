namespace Castos

open System
open System.IO

open Chessie.ErrorHandling

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

    let basePath = @"\\qnap\Music\Podcasts"

    let episodes path =
        let files =
            Directory.GetFiles(path)
            |> Seq.map (fun x ->
                   { Name = Path.GetFileName(x)
                     Length = TimeSpan.FromMinutes(0.) //TODO: Read from File
                     Path = x })
            |> Seq.toList
        files

    let podcastsOfCategory path category =
        let categoryPath = Path.Combine(path, category)

        let podcasts =
            Directory.GetDirectories(categoryPath)
            |> Seq.map (fun x ->
                   { Name = x.Substring(categoryPath.Length + 1)
                     Category = category
                     Folder = x
                     Current = None
                     Episodes = episodes x })
            |> Seq.toList
        podcasts

    let categories =
        Directory.GetDirectories(basePath)
        |> Seq.filter (fun x -> not (x.Substring(basePath.Length + 1).StartsWith(".")))
        |> Seq.map (fun x -> x.Substring(basePath.Length + 1))
        |> Seq.toList

    let podcasts =
        categories
        |> Seq.collect (fun x -> podcastsOfCategory basePath x)
        |> Seq.toList

    let GetPodcasts (s:string) =
        ok podcasts
