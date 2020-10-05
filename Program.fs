open System
open VpnGateConnect

let errorToMessage = function
    | UnexpectedStatusCode code -> sprintf "Unexpected status code from response: %d" code
    | UnexpectedContentType -> "Unexpected content type from response (expected text body)"
    | WebError msg -> sprintf "Internal web error when making request: %s" msg
    | EmptyCsv -> "CSV is empty"
    | CsvParseError msg -> sprintf "Error while parsing CSV: %s" msg
    | InvalidPath path -> sprintf "Nonexistent file or invalid path '%s'" path
    | CannotOpenFileBecause msg -> sprintf "Internal error when opening file: %s" msg

open ProgramFlow.Operators

let private drawFullScreen (str : string) =
    Console.SetCursorPosition(0, 0)
    Console.WriteLine str

let parseArguments argv =
    match argv |> Cli.parseArgs with
    | Ok args -> ContinueData args
    | Error msg -> ProgramFlow.usageError msg

let connectToDataSource dataSource =
    match dataSource |> DataSource.connect with
    | Ok rows -> ContinueData rows
    | Error errCode -> errCode |> errorToMessage |> ProgramFlow.runtimeError

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
        | Some selection -> ContinueData selection
        | None -> ProgramFlow.normalExitWithMsg "No VPN selected"
    
    resetConsoleProperties()
    Console.Clear()
    rv

let extractOpenVpnConfig (vpnData : VpnList.Row) =
    printfn "Selected %s" (vpnData.``#HostName``)
    try
        vpnData.OpenVPN_ConfigData_Base64
        |> Convert.FromBase64String
        |> Text.Encoding.UTF8.GetString
        |> ContinueData
    with ex -> ex.Message |> ProgramFlow.runtimeError

let appendCustomConfigs configPaths configStr =
    try 
        let appends = configPaths |> Array.map IO.File.ReadAllText
        let modifiedCfg = String.concat "\n" <| seq {
            yield configStr
            for a in appends -> a
        }
        ContinueData modifiedCfg
    with ex -> ex.Message |> ProgramFlow.runtimeError

let writeConfigToTempFile str =
    try
        let tempFile = IO.Path.GetTempFileName()
        IO.File.WriteAllText(tempFile, str)
        ContinueData tempFile
    with ex -> ex.Message |> ProgramFlow.runtimeError
    
let invokeOpenVpn configPath =
    try
        let proc = Diagnostics.Process.Start(fileName="openvpn", arguments=configPath)
        proc.WaitForExit()
        ExecResult (proc.ExitCode, None)
    with ex -> ex.Message |> ProgramFlow.runtimeError

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
    | ContinueData _ ->
        printfn "Error: program exited without result"; 1
    | ExecResult (errCode, msg) ->
        match msg with 
        | Some msg -> msg |> printfn "%s" 
        | None -> ()
        errCode
