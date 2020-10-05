open System
open Argu
open VpnGateConnect

let errorToMessage = function
    | UnexpectedStatusCode code -> sprintf "Unexpected status code from response: %d" code
    | UnexpectedContentType -> "Unexpected content type from response (expected text body)"
    | WebError msg -> sprintf "Internal web error when making request: %s" msg
    | EmptyCsv -> "CSV is empty"
    | CsvParseError msg -> sprintf "Error while parsing CSV: %s" msg
    | InvalidPath path -> sprintf "Nonexistent file or invalid path '%s'" path
    | CannotOpenFileBecause msg -> sprintf "Internal error when opening file: %s" msg

module ProgramFlow =
    type T<'a> = 
        | Data of 'a
        | Result of int * string option
    let bind f x =
        match x with
        | Data data -> f data
        | Result (x, y) -> Result (x, y)
    let map f = bind (f >> Data)
    let unwrap = function
        | Data data -> data
        | Result (x, y) -> failwith (sprintf "Attempted to unwrap result (%d, %A)!!" x y)

    module Operators =
        let (>>=) a b = bind b a
        let (<!>) a b = map b a

let normalExit = ProgramFlow.Result (0, None)
let normalExitWithMsg msg = ProgramFlow.Result (0, Some msg)
let runtimeError msg = ProgramFlow.Result (1, Some msg)
let usageError msg = ProgramFlow.Result (2, Some msg)

open ProgramFlow.Operators

let private drawFullScreen (str : String) =
    Console.SetCursorPosition(0, 0)
    Console.WriteLine str

let parseArguments argv =
    match argv |> Cli.parseArgs with
    | Ok args -> ProgramFlow.Data args
    | Error msg -> usageError msg

let connectToDataSource dataSource =
    match dataSource |> DataSource.connect with
    | Ok rows -> ProgramFlow.Data rows
    | Error errCode -> errCode |> errorToMessage |> runtimeError

let resetConsoleProperties _ =
    Console.CursorVisible <- true
    Console.ResetColor()

let promptForSelection filterPredicate rows =
    Console.CancelKeyPress.Add(resetConsoleProperties)
    Console.CursorVisible <- false
    Console.Clear()

    let filteredRows = Array.filter filterPredicate rows

    let rv = 
        match Gui.execRowSelectorMenu drawFullScreen filteredRows with
        | Some selection -> ProgramFlow.Data selection
        | None -> normalExitWithMsg "No VPN selected"
    
    resetConsoleProperties()
    Console.Clear()
    rv

let extractOpenVpnConfig (vpnData : VpnList.Row) =
    printfn "Selected %s" (vpnData.``#HostName``)
    try
        vpnData.OpenVPN_ConfigData_Base64
        |> Convert.FromBase64String
        |> Text.Encoding.UTF8.GetString
        |> ProgramFlow.Data
    with ex -> ex.Message |> runtimeError

let appendCustomConfigs configPaths configStr =
    try 
        let appends = configPaths |> Array.map IO.File.ReadAllText
        let modifiedCfg = String.concat "\n" <| seq {
            yield configStr
            for a in appends -> a
        }
        ProgramFlow.Data modifiedCfg
    with ex -> ex.Message |> runtimeError

let writeConfigToTempFile str =
    try
        let tempFile = IO.Path.GetTempFileName()
        IO.File.WriteAllText(tempFile, str)
        ProgramFlow.Data tempFile
    with ex -> ex.Message |> runtimeError
    
let invokeOpenVpn configPath =
    try
        let proc = Diagnostics.Process.Start(fileName="openvpn", arguments=configPath)
        proc.WaitForExit()
        ProgramFlow.Result (proc.ExitCode, None)
    with ex -> ex.Message |> runtimeError

let printDataSource (_, path) = 
    printfn "Fetching endpoint list from '%s'..." path

let printFetchResult rows =
    printfn "Fetched %d rows from endpoint source." (Array.length rows)
    rows

let execute (config: Config) =
    printDataSource config.DataSource

    let regionFilter = fun (x: VpnList.Row) -> 
        config.AllowedRegions.Length = 0 || config.AllowedRegions |> Array.contains (x.CountryShort.ToLower())
    
    connectToDataSource config.DataSource
    <!> printFetchResult
    >>= promptForSelection regionFilter
    >>= extractOpenVpnConfig
    >>= appendCustomConfigs config.ConfigPaths
    >>= writeConfigToTempFile
    >>= invokeOpenVpn

[<EntryPoint>]
let main argv =
    match argv |> parseArguments <!> Config.fromArgs >>= execute with
    | ProgramFlow.Data _ ->
        printfn "Error: program exited without result"; 1
    | ProgramFlow.Result (errCode, msg) ->
        match msg with 
        | Some msg -> msg |> printfn "%s" 
        | None -> ()
        errCode
