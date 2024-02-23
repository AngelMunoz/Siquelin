namespace Siquelin.Migrations

module Runner =
  open System.IO
  open Migrondi.Core
  open Microsoft.Extensions.Logging
  open Siquelin.Types.Env

  let internal getMigrondi paths config =
    let {
          databasePath = dbPath
          appDirectory = rootDir
        } =
      paths

    // create the database directory if it doesn't exist
    // to avoid exceptions when trying to create the database file
    Path.GetDirectoryName dbPath |> Directory.CreateDirectory |> ignore

    let config = {
      config with
          connection = $"Data Source={dbPath};"
    }

    let migrondi = Migrondi.MigrondiFactory(config, rootDir)
    migrondi.Initialize()
    migrondi

  let runMigrations (logger: ILogger, migrondi: IMigrondi) =
    let hasPending =
      migrondi.MigrationsList()
      |> Seq.choose(fun m ->
        match m with
        | Pending p -> Some p
        | _ -> None
      )
      |> Seq.length > 0

    let applied: seq<MigrationRecord> =
      if hasPending then migrondi.RunUp() else Seq.empty

    for migration in applied do
      logger.LogInformation("Applied migration {}", migration.name)

  let addNewMigration (logger: ILogger, migrondi: IMigrondi) (name: string) =
    let migration = migrondi.RunNew name
    logger.LogInformation("Generated migration {}", migration.name)