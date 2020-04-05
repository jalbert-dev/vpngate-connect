open System
open FSharp.Data
open Argu

let DefaultVpnListSource = (Cli.RemoteUrl, "https://www.vpngate.net/api/iphone")
let getDataSourceFromArgs (args : ParseResults<Cli.ArgParser>) =
    args.TryGetResult(<@ Cli.Source @>) |> Option.defaultValue DefaultVpnListSource

let errorToMessage = function
    | UnexpectedStatusCode code -> sprintf "Unexpected status code from response: %d" code
    | UnexpectedContentType -> "Unexpected content type from response (expected text body)"
    | WebError msg -> sprintf "Internal web error when making request: %s" msg
    | EmptyCsv -> "CSV is empty"
    | CsvParseError msg -> sprintf "Error while parsing CSV: %s" msg
    | InvalidPath path -> sprintf "Nonexistent file or invalid path '%s'" path
    | CannotOpenFileBecause msg -> sprintf "Internal error when opening file: %s" msg

[<EntryPoint>]
let main argv =
    match argv |> Cli.parseArgs with
    | Ok args -> 
        match (getDataSourceFromArgs args |> DataSource.connect) with
        | Ok rows -> 
            Gui.showMenu rows
            0
        | Error err -> 
            err |> errorToMessage |> printfn "%s" 
            1
    | Error msg -> 
        printfn "%s" msg
        2

