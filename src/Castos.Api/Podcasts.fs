namespace Castos

open System
open System.IO

module Podcasts =
    type Episode =
        { Id : string
          Name : string
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

    let basePath = @"\\le-nas\brase\music\podcasts"

    let episodes path category podcast =
        let files =
            Directory.GetFiles(path)
            |> Seq.map (fun x ->
                   let name = Path.GetFileName(x)
                   { Id = sprintf "%s___%s___%s" category podcast name
                     Name = name
                     Length = TimeSpan.FromMinutes(0.) //TODO: Read from File
                     Path = x })
            |> Seq.sortBy (fun e -> e.Id)
            |> Seq.toList
        files

    let podcastsOfCategory path category =
        let categoryPath = Path.Combine(path, category)

        let podcasts =
            Directory.GetDirectories(categoryPath)
            |> Seq.map (fun x ->
                   let name = x.Substring(categoryPath.Length + 1)
                   { Name = name
                     Category = category
                     Folder = x
                     Current = None
                     Episodes = episodes x category name })
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

    let GetPodcasts() =
        podcasts

    let GetPathFromId (id:string) =
        let splitted = id.Split([|"___"|], System.StringSplitOptions.RemoveEmptyEntries)
        sprintf @"%s\%s\%s\%s" basePath splitted.[0] splitted.[1] splitted.[2]
