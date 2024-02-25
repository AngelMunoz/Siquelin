namespace Siquelin.Database

open System
open System.Data
open System.Globalization
open Donald

open Siquelin
open Siquelin.Types

module Workday =
  let readAsWorkDay (reader: IDataReader) = {
    id = reader.ReadInt32 "id"
    date = reader.ReadString "date" |> DateOnly.Parse
    name = reader.ReadStringOption "item_name"
  }

  let create
    (connection: unit -> IDbConnection)
    (date: DateOnly, name: string option)
    =
    use connection = connection()

    connection
    |> Db.newCommand
      "INSERT INTO work_days (date, item_name) VALUES (@date, @item_name)"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
      "item_name", SqlType.sqlStringOrNull name
    ]
    |> Db.exec

  let list (connection: unit -> IDbConnection) =
    use connection = connection()

    connection
    |> Db.newCommand "SELECT * FROM work_days"
    |> Db.query readAsWorkDay

  let get (connection: unit -> IDbConnection) (date: DateOnly) =
    use connection = connection()

    connection
    |> Db.newCommand "SELECT * FROM work_days WHERE date = @date"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
    ]
    |> Db.querySingle readAsWorkDay

  let exists (connection: unit -> IDbConnection) (date: DateOnly) =

    use connection = connection()

    connection
    |> Db.newCommand "SELECT EXISTS(SELECT 1 FROM work_days WHERE date = @date)"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
    ]
    |> Db.scalar(fun value ->
      match value with
      | :? int as value -> value > 0
      | :? int64 as value -> value > 0L
      | _ -> false
    )

  let factory (connection: unit -> IDbConnection) =
    { new Env.WorkdayService with
        member _.create(targetDate, ?label) =
          create connection (targetDate, label)

        member _.list() = list connection
        member _.get date = get connection date
        member _.exists date = exists connection date
    }

module ShiftItem =

  let readAsShiftItem (reader: IDataReader) = {
    id = reader.ReadInt32 "id"
    workDayId = reader.ReadInt32 "work_day_id"
    start = reader.ReadString "start_time" |> TimeOnly.Parse
    finish = reader.ReadString "end_time" |> TimeOnly.Parse
    name = reader.ReadString "item_name"
  }

  let findItemDbUnit (dateId: int, name: string) (connection: IDbConnection) =
    connection
    |> Db.newCommand
      "SELECT * FROM shift_items WHERE work_day_id = @work_day_id AND item_name LIKE @item_name"
    |> Db.setParams [
      "work_day_id", SqlType.sqlInt32 dateId
      "item_name", SqlType.sqlString name
    ]

  let insertItemDbUnit
    (connection: IDbConnection)
    (dateId: int, name: string, start: TimeOnly, finish: TimeOnly)
    =
    connection
    |> Db.newCommand
      "INSERT INTO shift_items (work_day_id, start_time, end_time, item_name) VALUES (@work_day_id, @start_time, @end_time, @item_name)"
    |> Db.setParams [
      "work_day_id", SqlType.sqlInt32 dateId
      "start_time",
      SqlType.sqlString(start.ToString("o", CultureInfo.InvariantCulture))
      "end_time",
      SqlType.sqlString(finish.ToString("o", CultureInfo.InvariantCulture))
      "item_name", SqlType.sqlString name
    ]

  let updateItemDbUnit
    (connection: IDbConnection)
    (itemId: int)
    (startTime: TimeOnly)
    (endTime: TimeOnly)
    =
    connection
    |> Db.newCommand
      "UPDATE shift_items SET start_time = @start_time, end_time = @end_time WHERE id = @id"
    |> Db.setParams [
      "id", SqlType.sqlInt32 itemId
      "start_time",
      SqlType.sqlString(startTime.ToString("o", CultureInfo.InvariantCulture))
      "end_time",
      SqlType.sqlString(endTime.ToString("o", CultureInfo.InvariantCulture))
    ]

  let upsert
    (connection: unit -> IDbConnection)
    (date: DateOnly, name: string, start: TimeOnly, finish: TimeOnly)
    =

    Workday.get connection date
    |> Option.toResult(fun () -> Env.WorkDayNotFound)
    |> Result.map(fun date ->
      use connection = connection()

      let operation =
        match
          connection
          |> findItemDbUnit(date.id, name)
          |> Db.querySingle readAsShiftItem
        with
        | Some item -> updateItemDbUnit connection item.id start finish
        | None -> insertItemDbUnit connection (date.id, name, start, finish)

      operation |> Db.exec
    )

  let list (connection: unit -> IDbConnection) (date: DateOnly) =

    Workday.get connection date
    |> Option.toResult(fun () -> Env.WorkDayNotFound)
    |> Result.map(fun workday ->

      use connection = connection()

      connection
      |> Db.newCommand
        "SELECT * FROM shift_items WHERE work_day_id = @work_day_id"
      |> Db.setParams [ "work_day_id", SqlType.sqlInt32 workday.id ]
      |> Db.query readAsShiftItem
    )

  let factory (connection: unit -> IDbConnection) =
    { new Env.ShiftItemService with
        member _.upsert(targetDate, label, startDate, endDate) =
          upsert connection (targetDate, label, startDate, endDate)

        member _.list date = list connection date
    }