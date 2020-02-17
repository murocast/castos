namespace Castos

open System
open CosmoStore
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module EventStore =
    let private getUnionCaseName (x:'a) =
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let private createEvent event =
        ({ Id = (Guid.NewGuid())
           CorrelationId = None
           CausationId = None
           Name = getUnionCaseName event
           Data = event
           Metadata = None })

    let private appendEvent store stream event =
        store.AppendEvent stream Any event
        |> Async.AwaitTask
        |> Async.RunSynchronously

    //TODO: Version
    let storeEvent eventStore getStreamId ev =
        createEvent (ev)
        |> appendEvent eventStore (getStreamId ev)
        |> ignore

    let rec getEventsFromEventRead (lastVersion:int64) (events:EventRead<'a, 'b> list) =
        match events with
        | e :: rest -> let (events, version) = getEventsFromEventRead lastVersion rest
                       ((e.Data :: events), (System.Math.Max(version,lastVersion))) //TODO: Tail recursion??
        | [ ] -> [ ], lastVersion

    let getAllEventsFromStreamById store streamId =
        AllEvents
        |> store.GetEvents (streamId)
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> getEventsFromEventRead 0L