namespace Siquelin.Types

open System

type WorkDay = { id: int; date: DateOnly }

type ShiftItem = {
  id: int
  workDayId: int
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
    abstract member create: DateOnly -> unit
    abstract member list: unit -> WorkDay list
    abstract member get: DateOnly -> WorkDay option
    abstract member exists: DateOnly -> bool

  type ShiftItemService =
    abstract member create:
      DateOnly * TimeOnly * TimeOnly -> Result<unit, ShiftItemQueryError>

    abstract member list:
      DateOnly -> Result<ShiftItem list, ShiftItemQueryError>

  type AppEnv = {
    locations: SiquelinDataLocations
    logger: ILogger
    migrondi: IMigrondi
    workdays: WorkdayService
    shiftItems: ShiftItemService
  }