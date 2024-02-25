namespace Siquelin.Types

open System

type WorkDay = {
  id: int
  date: DateOnly
  name: string option
}

type ShiftItem = {
  id: int
  workDayId: int
  name: string
  start: TimeOnly
  finish: TimeOnly
}

module Env =
  open Microsoft.Extensions.Logging
  open Migrondi.Core

  type SiquelinDataLocations = {
    appDirectory: string
    appDataDirectory: string
    databasePath: string
  }

  [<Struct>]
  type ShiftItemQueryError = | WorkDayNotFound

  type WorkdayService =
    abstract member create: targetDate: DateOnly * ?label: string -> unit
    abstract member list: unit -> WorkDay list
    abstract member get: DateOnly -> WorkDay option
    abstract member exists: DateOnly -> bool

  type ShiftItemService =
    abstract member upsert:
      targetDate: DateOnly *
      label: string *
      startTime: TimeOnly *
      endTime: TimeOnly ->
        Result<unit, ShiftItemQueryError>

    abstract member list:
      DateOnly -> Result<ShiftItem list, ShiftItemQueryError>

  type AppEnv = {
    locations: SiquelinDataLocations
    logger: ILogger
    migrondi: IMigrondi
    workdays: WorkdayService
    shiftItems: ShiftItemService
  }