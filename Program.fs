open FSharp.SystemCommandLine
open Siquelin
open Siquelin.Types
open Siquelin.Migrations


[<EntryPoint>]
let main argv =

  let env = Env.getEnv()

  // run this at the start of the app regardless of the commands
  // this will ensure that the database is up to date
  Runner.runMigrations(env.logger, env.migrondi)

  rootCommand argv {
    description "A simple time tracking cli example"

    // set the main handler to do nothing
    setHandler id

  // add a subcommand to list shifts

#if DEBUG
    // only allow this command in debug mode as it is meant for dev purposes
    addCommand(Commands.Hidden.newMigration env)
#endif


  }