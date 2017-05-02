namespace Smapi

open System.Xml
open System.Xml.Linq

open Microsoft.FSharp.Reflection

type ItemType =
    | Artist
    | Album
    | Genre
    | Playlist
    | Search
    | Program
    | Favorites
    | Favorite
    | Collection
    | Container
    | AlbumList
    | TrackList
    | Track
    | StreamList
    | ArtistTrackList
    | Audiobook
    | Other

type Collection = {
    Id:string
    ItemType:ItemType
    Title:string
    CanPlay:bool
}

type StreamMetadata = {
    Logo: string
    CurrentHost: string
    CurrentShow: string
}

type TrackMetadata = {
    Artist:string
    Duration:int
    CanResume:bool
}

type ItemMediaData =
    | StreamMetadata of StreamMetadata
    | TrackMetadata of TrackMetadata

type MediaMetadata = {
    Id:string
    ItemType:ItemType
    Title:string
    MimeType: string
    ItemMetadata: ItemMediaData
}

type MediaEntry =
    | MediaCollection of Collection
    | MediaMetadata of MediaMetadata

module Respond =
    [<Literal>]
    let NsEnvelope = "http://schemas.xmlsoap.org/soap/envelope/"
    [<Literal>]
    let NsSonos = "http://www.sonos.com/Services/1.1"

    let toString (x:'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None

    let getNode name ns =
        XElement(XName.Get(name, ns))

    let addToNode (node:XElement) name ns =
        let newNode = getNode name ns
        node.Add(newNode)
        newNode

    let addToNodeWithValue node name ns value =
        let node = addToNode node name ns
        node.Value <- value
        node

    let getMediaCollectionNode (c:Collection) =
        let root = getNode "mediaCollection" NsSonos
        addToNodeWithValue root "id" NsSonos c.Id |> ignore
        addToNodeWithValue root "itemType" NsSonos ((toString c.ItemType).ToLower()) |> ignore
        addToNodeWithValue root "title" NsSonos (string c.Title) |> ignore
        addToNodeWithValue root "canPlay" NsSonos (string c.CanPlay) |> ignore
        root

    let getTrackMetadata t =
        let root = getNode "trackMetadata" NsSonos
        // addToNodeWithValue root "albumId" NsSonos t.AlbumId |> ignore
        addToNodeWithValue root "duration" NsSonos (string t.Duration) |> ignore
        // addToNodeWithValue root "artistId" NsSonos t.ArtistId |> ignore
        // addToNodeWithValue root "genre" NsSonos t.Genre |> ignore
        addToNodeWithValue root "artist" NsSonos t.Artist |> ignore
        // addToNodeWithValue root "album" NsSonos t.Album |> ignore
        // addToNodeWithValue root "albumArtUri" NsSonos t.AlbumArtURI |> ignore
        addToNodeWithValue root "canResume" NsSonos (string t.CanResume) |> ignore
        root

    let getStreamMetadata s =
        let root = getNode "streamMetadata" NsSonos
        addToNodeWithValue root "logo" NsSonos s.Logo |> ignore
        addToNodeWithValue root "currentHost" NsSonos s.CurrentHost |> ignore
        addToNodeWithValue root "currentShow" NsSonos s.CurrentShow |> ignore
        root

    let getMediaMetadataBody root e =
        addToNodeWithValue root "id" NsSonos e.Id |> ignore
        addToNodeWithValue root "title" NsSonos (string e.Title) |> ignore
        addToNodeWithValue root "itemType" NsSonos ((toString e.ItemType).ToLower()) |> ignore
        addToNodeWithValue root "mimeType" NsSonos (string e.MimeType) |> ignore
        let node = match e.ItemMetadata with
                   | TrackMetadata t -> getTrackMetadata t
                   | StreamMetadata s -> getStreamMetadata s
        root.Add(node)
        root

    let getMediaMetadataNode e =
        let root = getNode "mediaMetadata" NsSonos
        getMediaMetadataBody root e

    let getEnvelopeWithBody() =
        let envelope = getNode "Envelope" NsEnvelope
        let body = addToNode envelope "Body" NsEnvelope
        envelope, body


    let getMetadataResponse items =
        let envelope, body = getEnvelopeWithBody()
        let response = addToNode body "getMetadataResponse" NsSonos
        let result = addToNode response "getMetadataResult" NsSonos

        let index = addToNodeWithValue result "index" NsSonos "0"
        let amount = string (Seq.length items)
        let count = addToNodeWithValue result "count" NsSonos amount
        let total = addToNodeWithValue result "total" NsSonos amount

        for item in items do
            let node = match item with
                       | MediaCollection c -> getMediaCollectionNode c
                       | MediaMetadata e -> getMediaMetadataNode e
            result.Add(node)

        envelope.ToString()

    let getMediaMetadataRepnose item =
        let envelope, body = getEnvelopeWithBody()
        let response = addToNode body "getMediaMetadataResponse" NsSonos
        let result = addToNode response "getMediaMetadataResult" NsSonos

        let result = getMediaMetadataBody result item

        envelope.ToString()

    let getPositionInformation id position =
        let positionInformation = getNode "positionInformation" NsSonos
        
        addToNodeWithValue positionInformation "id" NsSonos id
        |> ignore
        
        addToNodeWithValue positionInformation "index" NsSonos (string 0)
        |> ignore

        addToNodeWithValue positionInformation "offsetMillis" NsSonos (string position)
        |> ignore

        positionInformation
    
    let getMediaUriResponse uri id position =
        let envelope, body = getEnvelopeWithBody()
        let response = addToNode body "getMediaURIResponse" NsSonos
        let result = addToNode response "getMediaURIResult" NsSonos
        result.Value <- uri

        match position with
        | Some i -> response.Add(getPositionInformation id i)
        | _ -> ()

        envelope.ToString()



