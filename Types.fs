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

  type AppEnv = {
    locations: SiquelinDataLocations
    logger: ILogger
    migrondi: IMigrondi
  }