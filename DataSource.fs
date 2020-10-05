module VpnGateConnect.DataSource

open System.IO
open FSharp.Data

let private downloadVpnListCsv apiUrl =
    try
        let result = Http.Request(apiUrl, silentHttpErrors=true)
        match result.StatusCode with
        | 200 -> Ok result
        | code -> Error <| UnexpectedStatusCode code
    with ex -> Error <| WebError ex.Message

let private expectTextResponse response =
    match response.Body with
    | Text body -> Ok body
    | _ -> Error UnexpectedContentType

let private normalizeVpnGateCsv (csv : string) = 
    match csv with
    | "" ->
        Error EmptyCsv
    | _ ->
        let lines = csv.Split('\n')
        lines.[2..lines.Length-3]
        |> String.concat "\n"
        |> Ok

let private stringToVpnList csv =
    try
        csv |> VpnList.ParseRows |> Ok
    with
    | ex -> CsvParseError ex.Message |> Error

let private (>>=) a b = Result.bind b a

let private getLocalData path = 
    if File.Exists(path) |> not then
        Error <| InvalidPath path
    else
        try
            File.ReadAllText path |> Ok
        with ex -> Error (CannotOpenFileBecause ex.Message)

let private getRemoteData url = 
    url |> downloadVpnListCsv >>= expectTextResponse

let connect dataSource =
    match dataSource with
    | Cli.RemoteUrl, url -> getRemoteData url
    | Cli.LocalPath, path -> getLocalData path
    >>= normalizeVpnGateCsv
    >>= stringToVpnList