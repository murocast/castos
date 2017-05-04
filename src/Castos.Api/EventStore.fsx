#r @"bin\Debug\Castos.Api.exe"

open Castos.EventStore
open Castos.ErrorHandling

let eventStore = createGetEventStoreEventStore<string,string> "This is a version error"

eventStore.AddSubscriber "FirstSubscriber" (printfn "%A")

let res0 = eventStore.SaveEvents (StreamId "asd") (StreamVersion 0) ["Hello";"World"]
let res1 = eventStore.SaveEvents (StreamId "asd") (StreamVersion 1) ["Hello2";"World2"]
let res2 = eventStore.SaveEvents (StreamId "asd") (StreamVersion 2) ["Hello2";"World2"]

[res0;res1;res2] |> List.mapi (fun i v -> printfn "%i: %A" i v)

let events = eventStore.GetEvents (StreamId "asd")