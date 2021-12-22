module Murocast.Client.Template
open Feliz.Bulma
open Feliz

let inTemplate (content) =
    Bulma.hero [
        Bulma.heroBody [
            Bulma.columns [
                Bulma.column [
                    column.is4
                    column.isOffset4
                    text.hasTextCentered
                    prop.children [content]
                ]
            ]
        ]
    ]