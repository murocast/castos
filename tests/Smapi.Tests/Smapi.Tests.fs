module Smapi.Tests

open Smapi.Respond

open System
open System.IO
open System.Linq
open System.Xml
open System.Xml.XPath
open System.Xml.Linq

open Expecto

[<Tests>]
let smapi = 
    testList "samples" [
        testCase "Count should return number of items in collection" <| fun _ ->
            let items = [ MediaMetadata {  Id = ""
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
            let metadata = getMetadataResponse (Seq.ofList items)
            let reader = XmlReader.Create(new StringReader(metadata))
            let xml = XElement.Load(reader)
            let nameTable = reader.NameTable
            let nsManager = new XmlNamespaceManager(nameTable)
            nsManager.AddNamespace("sn", NsSonos)
            let count = xml.XPathSelectElements("//sn:count", nsManager).Single()
            Expect.equal "1" count.Value "XML-Node should be there"
    ]

[<EntryPoint>]
let main args =
  runTestsInAssembly defaultConfig args