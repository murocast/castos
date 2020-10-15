module Murocast.Client.View

open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader
open Feliz.Router

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Fable.Core
open Fable.React
open Fable.React.Props
open Thoth.Fetch
open Thoth.Json
open Fulma

open System
open Browser
open Shared

open Domain

let column (model : Model) (dispatch : Msg -> unit) =
    Column.column
        [ Column.Width (Screen.All, Column.Is4)
          Column.Offset (Screen.All, Column.Is4) ]
        [ Heading.h3
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Login" ]
          Heading.p
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Please login to proceed." ]
          Box.box' [ ]
            [ figure [ Class "avatar" ]
                [ img [ Src "https://placehold.it/128x128" ] ]
              form [ ]
                [ Field.div [ ]
                    [ Control.div [ ]
                        [ Input.email
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Email"
                              Input.Props [ AutoFocus true ]
                              Input.OnChange (fun ev -> dispatch (EmailChanged ev.Value)) ] ] ]
                  Field.div [ ]
                    [ Control.div [ ]
                        [ Input.password
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Password"
                              Input.OnChange (fun ev -> dispatch (PasswordChanged ev.Value)) ] ] ]
                  Button.button
                    [ Button.Color IsInfo
                      Button.IsFullWidth
                      Button.CustomClass "is-large is-block"
                      Button.OnClick (fun ev -> ev.preventDefault()
                                                dispatch Authorize) ]
                    [ str "Login" ] ] ]
          br [ ]
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.IsFullHeight ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ column model dispatch ] ] ]