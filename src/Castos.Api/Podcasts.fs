namespace Castos

open System
open System.IO

open TagLib

module Podcasts =
    type Episode =
        { Id : PodcastId
          Name : string
          Length : TimeSpan
          Path : string }
    and PodcastId = string

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

    let getTagFile filepath = 
        let name = Path.GetFileName(filepath)
        use stream = File.OpenRead(filepath)
        let tagFile = TagLib.File.Create(StreamFileAbstraction(name, stream,stream))        
        tagFile

    let episodes path category podcast =
        let files =
            Directory.GetFiles(path)
            |> Seq.map (fun x ->                   
                   let fileName = Path.GetFileName(x)
                   let tagFile = getTagFile x
                   { Id = sprintf "%s___%s___%s" category podcast fileName
                     Name = tagFile.Tag.Title
                     Length = tagFile.Properties.Duration
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
        podcasts

    let categories =
        Directory.GetDirectories(basePath)
        |> Seq.filter (fun x -> not (x.Substring(basePath.Length + 1).StartsWith(".")))
        |> Seq.map (fun x -> x.Substring(basePath.Length + 1))        

    let readPodcasts() =
        categories
        |> Seq.collect (fun x -> podcastsOfCategory basePath x)
        |> List.ofSeq
        |> Seq.ofList
    
    let mutable podcasts = readPodcasts()
    let watcher = new FileSystemWatcher(Path = basePath, EnableRaisingEvents = true, IncludeSubdirectories = true)    
    let rec loop() = 
        async { 
            let! _ = Async.AwaitEvent watcher.Changed
            podcasts <- readPodcasts()
            return! loop()
        }
    Async.Start (loop())
    
    let GetPodcasts() =              
        podcasts
    let GetPathFromId (id:string) =
        let splitted = id.Split([|"___"|], System.StringSplitOptions.RemoveEmptyEntries)
        sprintf @"%s\%s\%s\%s" basePath splitted.[0] splitted.[1] splitted.[2]
