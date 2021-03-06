module Yobo.Client.Http

open Fable.Remoting.Client

module Cmd =
    open Elmish
    open Yobo.Shared.Communication

    let ofAsyncResult f msg i =
        Cmd.ofAsync
            f i            
            (msg)
            (fun ex -> ServerError.Exception(ex.Message) |> Error |> msg)

let authAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Yobo.Shared.Auth.Communication.routeBuilder
    |> Remoting.buildProxy<Yobo.Shared.Auth.Communication.API>

let adminAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Yobo.Shared.Admin.Communication.routeBuilder
    |> Remoting.buildProxy<Yobo.Shared.Admin.Communication.API>

let calendarAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Yobo.Shared.Calendar.Communication.routeBuilder
    |> Remoting.buildProxy<Yobo.Shared.Calendar.Communication.API>



module SecuredParam =
    open Yobo.Shared.Communication

    let create value = {
        Token = TokenStorage.tryGetToken() |> Option.defaultValue ""
        Param = value
    }