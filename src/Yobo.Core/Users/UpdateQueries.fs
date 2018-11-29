module Yobo.Core.Users.UpdateQueries

open Yobo.Core
open System

let private getById (ctx:ReadDb.Db.dataContext) i =
    query {
        for x in ctx.Dbo.Users do
        where (x.Id = i)
        select x
    } |> Seq.head

let register (args:CmdArgs.Register) (ctx:ReadDb.Db.dataContext) =
    let item = ctx.Dbo.Users.Create()
    item.Id <- args.Id
    item.Email <- args.Email
    item.FirstName <- args.FirstName
    item.LastName <- args.LastName
    item.ActivationKey <- args.ActivationKey
    item.PasswordHash <- args.PasswordHash
    item.RegisteredUtc <- DateTime.UtcNow
    ctx.SubmitUpdates()

let activate (args:CmdArgs.Activate) (ctx:ReadDb.Db.dataContext) =
    let item = args.Id |> getById ctx
    item.ActivatedUtc <- Some DateTime.UtcNow
    ctx.SubmitUpdates()