namespace Castos

open Smapi
open Smapi.Respond
open FSharp.Data

module Smapi =
    type getMetadataRequest = XmlProvider<"Samples/GetMetadataRequest.xml">

    let extractSmapiMethod (m:string) =
        m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

    let processGetMetadata s =
        let req = getMetadataRequest.Parse s
        let items = [ MediaMetadata { Id = ""
                                      ItemType = Artist
                                      Title = "TitleOne"
                                      MimeType = "media/mp3"
                                      ItemMetadata = TrackMetadata { AlbumId = "Album1"
                                                                     Duration = 123
                                                                     ArtistId = "Artist1"
                                                                     Genre = ""
                                                                     Artist = "Mobi"
                                                                     Album = "First"
                                                                     AlbumArtURI = "" }}]
        let response = getMetadataResponse items
        Success(response)

    let processSmapiMethod a form =
        match extractSmapiMethod a with
        | "getMetadata" -> processGetMetadata form
        | _ -> Failure(sprintf "Method not implemented %s" a)

