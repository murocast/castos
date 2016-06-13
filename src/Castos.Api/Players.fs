namespace Castos

open FSharp.Data

module Players =
    type Player =
        { Id : string }

    type Zones = JsonProvider<"Samples/zones.json">

    let players =
        let zonesResponse = Http.RequestString("http://localhost:5005/zones")
        let zones = Zones.Parse zonesResponse
        zones
        |> Seq.map (fun z ->  { Id = z.Coordinator.RoomName })

    let GetPlayers (s:string)  =
        Success players