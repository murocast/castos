namespace Castos

open FSharp.Data

module Players =
    type Player =
        { Id : string }

    type Zones = JsonProvider<"Samples/zones.json">

    let players =
        Zones.Load "http://localhost:5005/zones"
        |> Seq.map (fun z ->  { Id = z.Coordinator.RoomName })

    let GetPlayers (s:string)  =
        ok players