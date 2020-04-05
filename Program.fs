// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open Argu

type SourceType =
    | [<CustomCommandLine("url")>] RemoteUrl
    | [<CustomCommandLine("file")>] LocalPath

type CliArgs = 
    | [<AltCommandLine("-s"); Unique>] Source of SourceType*string
with 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Source _ -> "Specify a data source to get VPN list from (either remote URL or local path)"

type VpnList = CsvProvider<"data/sample_vpn_list.csv">

type ErrorType = 
    | UnexpectedStatusCode of int
    | UnexpectedContentType
    | WebError of string
    | EmptyCsv
    | CsvParseError of string
    | InvalidPath of string
    | CannotOpenFileBecause of string

let downloadVpnListCsv apiUrl =
    try
        let result = Http.Request(apiUrl, silentHttpErrors=true)
        match result.StatusCode with
        | 200 -> Ok result
        | code -> Error <| UnexpectedStatusCode code
    with
    | :?Net.WebException as ex -> Error <| WebError ex.Message

let expectTextResponse response =
    match response.Body with
    | Text body -> Ok body
    | _ -> Error UnexpectedContentType

let normalizeVpnGateCsv (csv : string) = 
    match csv with
    | "" ->
        Error EmptyCsv
    | _ ->
        let lines = csv.Split('\n')
        lines.[2..lines.Length-3]
        |> String.concat "\n"
        |> Ok

let stringToVpnList csv =
    try
        csv |> VpnList.ParseRows |> Ok
    with
    | ex -> CsvParseError ex.Message |> Error

let private (>>=) a b = Result.bind b a
let private (>=>) a b = a >> Result.bind b

let parseArgs (parser : ArgumentParser<'a>) argv =
    try
        parser.ParseCommandLine(inputs=argv) |> Ok
    with
    | :?Argu.ArguParseException as ex -> Error (ex.Message)

let getLocalData path = 
    if IO.File.Exists(path) |> not then
        Error <| InvalidPath path
    else
        try
            IO.File.ReadAllText path |> Ok
        with ex -> Error (CannotOpenFileBecause ex.Message)
let getRemoteData = downloadVpnListCsv >=> expectTextResponse

let connectDataSource dataSource =
    match dataSource with
    | RemoteUrl, url -> getRemoteData url
    | LocalPath, path -> getLocalData path
    >>= normalizeVpnGateCsv
    >>= stringToVpnList

let DefaultVpnListSource = (RemoteUrl, "https://www.vpngate.net/api/iphone")
let getDataSourceFromArgs (args : ParseResults<CliArgs>) =
    args.TryGetResult(<@ Source @>) |> Option.defaultValue DefaultVpnListSource

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
    Console.Clear();

    let parser = ArgumentParser.Create<CliArgs>(checkStructure=true)
    
    match argv |> parseArgs parser with
    | Ok args -> 
        match (getDataSourceFromArgs args |> connectDataSource) with
        | Ok rows -> 
            rows |> printfn "%A"
            0
        | Error err -> 
            err |> errorToMessage |> printfn "%s" 
            1
    | Error msg -> 
        printfn "%s" msg
        2

