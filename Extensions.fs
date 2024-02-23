namespace Siquelin

[<AutoOpen>]
module Extensions =
  [<RequireQualifiedAccess>]
  module Option =
    let toResult<'Value, 'Error>
      (orElse: unit -> 'Error)
      (option: 'Value option)
      =
      match option with
      | Some value -> Ok value
      | None -> Error(orElse())