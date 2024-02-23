namespace Siquelin

open FSharp.SystemCommandLine

open Siquelin.Types

module Handlers =
  ()


module Commands =

  let startDay (env: Env.AppEnv) =
    let cmd = command "start-day" {
      description "Start a new work day"

      setHandler(fun () ->
        printfn "Starting a new work day"
        0
      )
    }

    cmd

  module Hidden =
    open Siquelin.Migrations

    let newMigration (env: Env.AppEnv) =
      let name = Input.Argument<string>("name", "The name of the migration")

      let newMigration = Runner.addNewMigration(env.logger, env.migrondi)

      let cmd = command "new-migration" {
        description "Generate a new migration"
        inputs name

        setHandler(fun name ->
          newMigration name
          0
        )
      }

      cmd.IsHidden <- true
      cmd