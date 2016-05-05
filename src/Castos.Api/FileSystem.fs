namespace Castos

open System
open System.IO

module FileSystem =
    let basePath = @"\\qnap\Music\Podcasts"

    let episodes path =
        let files = Directory.GetFiles(path)
                    |> Seq.map(fun x -> {Name = Path.GetFileName(x)
                                         Length = TimeSpan.FromMinutes(0.) //TODO: Read from File
                                         Path = x})
                    |> Seq.toList
        files

    let podcastsOfCategory path category =
        let categoryPath = path + "\\" + category
        let podcasts = Directory.GetDirectories(categoryPath)
                       |> Seq.map(fun x -> { Name = x.Substring(categoryPath.Length + 1)
                                             Category = category
                                             Folder = x
                                             Current = None
                                             Episodes = episodes x })
                       |> Seq.toList
        podcasts

    let Podcasts =
        let categories = Directory.GetDirectories(basePath)
                         |> Seq.filter(fun x -> not(x.Substring(basePath.Length + 1).StartsWith(".")))
                         |> Seq.map(fun x -> x.Substring(basePath.Length + 1))
                         |> Seq.toList
        let podcasts = categories
                       |> Seq.collect(fun x -> podcastsOfCategory basePath x)
                       |> Seq.toList
        ()