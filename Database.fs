namespace Siquelin.Database

open System
open System.Data
open Donald

open Siquelin
open Siquelin.Types


module Workday =

  let create (connection: IDbConnection) (date: DateOnly) =
    connection
    |> Db.newCommand "INSERT INTO work_days (date) VALUES (@date)"
    |> Db.setParams [ "date", SqlType.sqlString(date.ToShortDateString()) ]
    |> Db.exec

  let list (connection: IDbConnection) =
    connection
    |> Db.newCommand "SELECT * FROM work_days"
    |> Db.query(fun (reader: IDataReader) -> {
      id = reader.ReadInt32 "id"
      date = reader.ReadString "date" |> DateOnly.Parse
    })

  let get (connection: IDbConnection) (date: DateOnly) =
    connection
    |> Db.newCommand "SELECT * FROM work_days WHERE date = @date"
    |> Db.setParams [ "date", SqlType.sqlString(date.ToShortDateString()) ]
    |> Db.querySingle(fun (reader: IDataReader) -> {
      id = reader.ReadInt32 "id"
      date = reader.ReadString "date" |> DateOnly.Parse
    })

module ShiftItem =

  [<Struct>]
  type ShiftItemQueryError = | WorkDayNotFound

  let create
    (connection: IDbConnection)
    (date: DateOnly, start: TimeOnly, finish: TimeOnly)
    =
    Workday.get connection date
    |> Option.toResult(fun () -> WorkDayNotFound)
    |> Result.map(fun date ->
      connection
      |> Db.newCommand
        "INSERT INTO shift_items (work_day_id, start, finish) VALUES (@work_day_id, @start, @finish)"
      |> Db.setParams [
        "work_day_id", SqlType.sqlInt32(date.id)
        "start", SqlType.sqlString(start.ToString())
        "finish", SqlType.sqlString(finish.ToString())
      ]
      |> Db.exec
    )

  let list (connection: IDbConnection) (date: DateOnly) =
    Workday.get connection date
    |> Option.toResult(fun () -> WorkDayNotFound)
    |> Result.map(fun workday ->
      connection
      |> Db.newCommand
        "SELECT * FROM shift_items WHERE work_day_id = @work_day_id"
      |> Db.setParams [ "work_day_id", SqlType.sqlInt32(workday.id) ]
      |> Db.query(fun (reader: IDataReader) -> {
        id = reader.ReadInt32 "id"
        workDayId = reader.ReadInt32 "work_day_id"
        start = reader.ReadString "start" |> TimeOnly.Parse
        finish = reader.ReadString "finish" |> TimeOnly.Parse
      })
    )