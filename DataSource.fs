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

/// Extracts the text body from an HTTP response, or returns an Error result if not text.
let private expectTextResponse response =
    match response.Body with
    | Text body -> Ok body
    | _ -> Error UnexpectedContentType

/// VPN Gate CSV has some unnecessary lines that trip up the CSV parser.
/// This function returns true for the header, column list, and footer lines.
let private isJunkLine (x : string) =
    x.Trim() = "*vpn_servers" ||
    x.StartsWith("#HostName") ||
    x.Trim() = "*"

/// Removes extraneous lines from VPN Gate CSV gateway list.
let private normalizeVpnGateCsv (csv : string) = 
    match csv with
    | "" ->
        Error EmptyCsv
    | _ ->
        csv.Split('\n')
        |> Array.filter (not << isJunkLine)
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

/// Takes a data source tuple, connects to it, and returns the resulting list of VPNs.
let connect dataSource =
    match dataSource with
    | Cli.RemoteUrl, url -> getRemoteData url
    | Cli.LocalPath, path -> getLocalData path
    >>= normalizeVpnGateCsv
    >>= stringToVpnList