module Siquelin.Env

open System
open System.IO

open Microsoft.Extensions.Logging

open Migrondi.Core

open Siquelin.Types.Env
open Siquelin.Migrations

let private loggerFactory =
  lazy
    (LoggerFactory.Create(fun builder ->
      builder
        .AddConsole()
#if DEBUG
        .SetMinimumLevel(LogLevel.Debug)
#else
        .SetMinimumLevel(LogLevel.Information)
#endif
      |> ignore
    ))


let private getEnvLocations () =
  let appData =
    // avoid polluting the user's home directory and use the local app data folder
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "siquelin"
    )

  // Where do we actually want our database to be?
  let dbPath = Path.Combine(appData, "siquelin.db")

  let appDir =
#if DEBUG
    // At dev time we want to be in the project directory
    // as they will be copied to the output directory when building for release
    Environment.CurrentDirectory
#else
    // where is our assembly located
    // migrations will be in the same directory (e.g. AppContext.BaseDirectory/migrations/*.sql)
    AppContext.BaseDirectory
#endif

  {
    appDirectory = appDir
    appDataDirectory = appData
    databasePath = dbPath
  }

let getEnv () : Types.Env.AppEnv =
  let logger = loggerFactory.Value.CreateLogger("Siquelin")
  let locations = getEnvLocations()
  let migrondi = Runner.getMigrondi locations MigrondiConfig.Default

  {
    locations = locations
    logger = logger
    migrondi = migrondi
  }