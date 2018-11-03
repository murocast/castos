namespace Smapi

open Smapi.Respond

type LastUpdateResult = { AutoRefreshEnabled : bool
                          Catalog: string
                          Favorites: string
                          PollIntervall : int }

module GetLastUpdate =
    let toLastUpdateXml lastUpdateResult =
        let envelope, body = getEnvelopeWithBody()
        let response = addToNode body "getLastUpdateResponse" NsSonos
        let result = addToNode response "getLastUpdateResult" NsSonos

        if lastUpdateResult.AutoRefreshEnabled then
            addToNodeWithValue result "autoRefreshEnabled" NsSonos (string true)
            |> ignore

        addToNodeWithValue result "favorites" NsSonos lastUpdateResult.Favorites
        |> ignore
        addToNodeWithValue result "catalog" NsSonos lastUpdateResult.Catalog
        |> ignore
        addToNodeWithValue result "pollInterval" NsSonos (string lastUpdateResult.PollIntervall)
        |> ignore

        envelope.ToString()
