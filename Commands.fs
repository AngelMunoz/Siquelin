namespace Siquelin

open System
open FSharp.SystemCommandLine

open Siquelin.Types

module Parsing =
  let dayParser (str: string) =
    let parseISODate (str: string) =
      match DateOnly.TryParseExact(str, "yyyy-MM-dd") with
      | true, date -> Ok date
      | _ -> Error "Invalid date format"

    let parseUSDate (str: string) =
      match DateOnly.TryParseExact(str, "MM/dd/yyyy") with
      | true, date -> Ok date
      | _ -> Error "Invalid date format"

    let parseNormalDates (str: string) =
      match DateOnly.TryParseExact(str, "dd/MM/yyyy") with
      | true, date -> Ok date
      | _ -> Error "Invalid date format"

    match parseISODate str with
    | Ok value -> Ok value
    | Error _ ->
      match parseNormalDates str with
      | Ok value -> Ok value
      | Error _ -> parseUSDate str

  let parseDateOnlyArgument (arg: CommandLine.Parsing.ArgumentResult) =
    match arg.Tokens |> Seq.tryHead with
    | None -> None
    | Some token ->
      let value = dayParser(token.Value)
      value |> Result.toOption

module Handlers =
  open Siquelin.Types.Env
  open Microsoft.Extensions.Logging

  let logDay
    (logger: ILogger, workdays: WorkdayService)
    (day: DateOnly, label: string option)
    =
    logger.LogInformation(
      "Logging a new work day: {day}",
      day.ToLongDateString()
    )

    workdays.create(day, ?label = label)

  let logItem
    (logger: ILogger, workDays: WorkdayService, shiftItems: ShiftItemService)
    (start: TimeOnly, finish: TimeOnly, label: string, day: DateOnly option)
    =
    let day = defaultArg day (DateOnly.FromDateTime(DateTime.Today))

    logger.LogInformation("Logging an item for: {day}", day.ToLongDateString())

    match workDays.exists day with
    | true -> logger.LogDebug("Work day '{day}' found", day.ToLongDateString())
    | false ->
      logger.LogWarning(
        "Work day '{day}' not found, logging it...",
        day.ToLongDateString()
      )

      workDays.create day


    match shiftItems.upsert(day, label, start, finish) with
    | Ok() -> 0
    | Error e ->
      logger.LogError("Failed to create shift item: {error}", e)
      1

  let listItemsForDay
    (logger: ILogger, shiftItems: ShiftItemService)
    (day: DateOnly option)
    =
    let day = defaultArg day (DateOnly.FromDateTime(DateTime.Today))

    logger.LogInformation(
      "Listing shift items for: {day}",
      day.ToLongDateString()
    )

    match shiftItems.list day with
    | Ok items ->
      items
      |> List.iter(fun item ->
        logger.LogInformation(
          "Shift item: '{item}': {start} - {finish}",
          item.name,
          item.start,
          item.finish
        )
      )

      0
    | Error e ->
      logger.LogError("Failed to list shift items: {error}", e)
      1

module Commands =

  let logDay (env: Env.AppEnv) =
    let argument =
      Input.Argument<string>(
        "day",
        "The day to log, in the format of 'yyyy-MM-dd'"
      )

    let label =
      Input.OptionMaybe<string>(
        [ "-l"; "--label" ],
        "The label of the work day"
      )

    command "log" {
      description "Start a new work day"

      inputs(argument, label)

      setHandler(
        (fun (day, label) ->
          match Parsing.dayParser day with
          | Ok day -> day, label
          | Error e -> failwith e
        )
        >> Handlers.logDay(env.logger, env.workdays)
      )
    }

  let logItem (env: Env.AppEnv) =
    let start = Input.Argument<TimeOnly>("start", "The start time of the shift")

    let label = Input.Argument<string>("label", "The label of the shift item")

    let finish =
      Input.Argument<TimeOnly>("finish", "The finish time of the shift")

    let day =
      CommandLine.Option<DateOnly option>(
        aliases = [| "-d"; "--day" |],
        parseArgument = Parsing.parseDateOnlyArgument,
        Arity = CommandLine.ArgumentArity.ExactlyOne
      )
      |> Input.OfOption

    command "item" {
      description "Add a new shift item"

      inputs(start, finish, label, day)

      setHandler(Handlers.logItem(env.logger, env.workdays, env.shiftItems))
    }


  let listItemsForDay (env: Env.AppEnv) =
    let day =
      Input.ArgumentMaybe<string>(
        "day",
        "The day to log, in the format of 'yyyy-MM-dd'"
      )

    command "list-items" {
      description "List shift items for a day"

      inputs day

      setHandler(
        (fun day ->
          match day with
          | Some day ->
            match Parsing.dayParser day with
            | Ok day -> Some day
            | Error e -> None
          | None -> None
        )
        >> Handlers.listItemsForDay(env.logger, env.shiftItems)
      )
    }


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