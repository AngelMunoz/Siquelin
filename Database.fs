namespace Siquelin.Database

open System
open System.Data
open System.Globalization
open Donald

open Siquelin
open Siquelin.Types

module Workday =

  let create (connection: unit -> IDbConnection) (date: DateOnly) =
    use connection = connection()

    connection
    |> Db.newCommand "INSERT INTO work_days (date) VALUES (@date)"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
    ]
    |> Db.exec

  let list (connection: unit -> IDbConnection) =
    use connection = connection()

    connection
    |> Db.newCommand "SELECT * FROM work_days"
    |> Db.query(fun (reader: IDataReader) -> {
      id = reader.ReadInt32 "id"
      date = reader.ReadString "date" |> DateOnly.Parse
    })

  let get (connection: unit -> IDbConnection) (date: DateOnly) =
    use connection = connection()

    connection
    |> Db.newCommand "SELECT * FROM work_days WHERE date = @date"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
    ]
    |> Db.querySingle(fun (reader: IDataReader) -> {
      id = reader.ReadInt32 "id"
      date = reader.ReadString "date" |> DateOnly.Parse
    })

  let exists (connection: unit -> IDbConnection) (date: DateOnly) =

    use connection = connection()

    connection
    |> Db.newCommand "SELECT COUNT(*) FROM work_days WHERE date = @date"
    |> Db.setParams [
      "date",
      SqlType.sqlString(date.ToString("o", CultureInfo.InvariantCulture))
    ]
    |> Db.scalar(fun value ->
      match value with
      | :? int64 as value -> value > 0L
      | :? int as value -> value > 0
      | _ -> false
    )

  let factory (connection: unit -> IDbConnection) =
    { new Env.WorkdayService with
        member _.create date = create connection date
        member _.list() = list connection
        member _.get date = get connection date
        member _.exists date = exists connection date
    }

module ShiftItem =

  let create
    (connection: unit -> IDbConnection)
    (date: DateOnly, start: TimeOnly, finish: TimeOnly)
    =
    Workday.get connection date
    |> Option.toResult(fun () -> Env.WorkDayNotFound)
    |> Result.map(fun date ->
      use connection = connection()

      connection
      |> Db.newCommand
        "INSERT INTO shift_items (work_day_id, start_time, end_time) VALUES (@work_day_id, @start_time, @end_time)"
      |> Db.setParams [
        "work_day_id", SqlType.sqlInt32(date.id)
        "start_time",
        SqlType.sqlString(start.ToString("o", CultureInfo.InvariantCulture))
        "end_time",
        SqlType.sqlString(finish.ToString("o", CultureInfo.InvariantCulture))
      ]
      |> Db.exec
    )

  let list (connection: unit -> IDbConnection) (date: DateOnly) =

    Workday.get connection date
    |> Option.toResult(fun () -> Env.WorkDayNotFound)
    |> Result.map(fun workday ->

      use connection = connection()

      connection
      |> Db.newCommand
        "SELECT * FROM shift_items WHERE work_day_id = @work_day_id"
      |> Db.setParams [ "work_day_id", SqlType.sqlInt32(workday.id) ]
      |> Db.query(fun (reader: IDataReader) -> {
        id = reader.ReadInt32 "id"
        workDayId = reader.ReadInt32 "work_day_id"
        start = reader.ReadString "start_time" |> TimeOnly.Parse
        finish = reader.ReadString "end_time" |> TimeOnly.Parse
      })
    )

  let factory (connection: unit -> IDbConnection) =
    { new Env.ShiftItemService with
        member _.create(date, start, finish) =
          create connection (date, start, finish)

        member _.list date = list connection date
    }