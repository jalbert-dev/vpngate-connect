module VpnGateConnect.Gui

open System

type private State =
    | SelectVpn
    | Result of VpnList.Row option
type private MenuStatus = { 
    Menu: State
    Data: VpnList.Row array
    RTT: string option array
    SelectionIndex: int
}

let private trimToLength width str =
    if String.length str > width then
        str.[..width-4] + "..."
    else
        str

let private padToLength width (padChar : Char) str =
    if String.length str <= width then
        str + String.replicate (width - String.length str) (string padChar)
    else
        str

let private lineOf width char =
    String.Empty |> padToLength width char

let private blankLine width = lineOf width ' '

let private renderItem index selected maxWidth (row : VpnList.Row) rtt  =
    sprintf "%c  [%c] %3d. %s  %sms, %s (%s)%c" 
        (if selected then encodeConsoleColor ConsoleColor.Green else encodeDefaultConsoleColor)
        (if selected then 'X' else ' ')
        index
        row.CountryShort
        (rtt |> Option.defaultValue (string (int row.Ping)) |> padToLength 3 ' ')
        row.``#HostName``
        row.IP
        encodeDefaultConsoleColor
    |> trimToLength maxWidth

let private resultState result state = 
    { state with Menu=Result result }

let private incrementSelection state = 
    { state with SelectionIndex=min (state.Data.Length-1) (state.SelectionIndex+1)}

let private decrementSelection state = 
    { state with SelectionIndex=max 0 (state.SelectionIndex-1)}

let private pingSelection state =
    let pinger = new Net.NetworkInformation.Ping()
    let reply = pinger.Send(state.Data.[state.SelectionIndex].IP, 1000)

    // This is pretty shoddy and chucks immutability right out the window
    Array.set state.RTT state.SelectionIndex 
        (match reply.Status with
        | Net.NetworkInformation.IPStatus.Success -> string reply.RoundtripTime |> Some
        | _ -> Some "???")

    state

let private confirmSelection state =
    state |> resultState (Some state.Data.[state.SelectionIndex])

[<Literal>]
let private VerticalPadding = 8

let private listDimensions termWidth termHeight = (termWidth - 2, termHeight - VerticalPadding)

let private renderMenu state =
    let consoleWidth = Console.WindowWidth
    let consoleHeight = Console.WindowHeight

    seq {
        match state.Menu with
        | SelectVpn ->
            yield lineOf consoleWidth '-'
            yield "  Select a VPN endpoint to connect to" |> trimToLength consoleWidth
            yield lineOf consoleWidth '-'

            let listWidth, listHeight = listDimensions consoleWidth consoleHeight
            let displayMin = max 0 (state.SelectionIndex - listHeight / 2)
            let displayMax = min (state.Data.Length - 1) (displayMin + listHeight)

            let displaySlice = state.Data.[displayMin..displayMax]
            for i in 0..displaySlice.Length-1 ->
                let isSelected = (i = state.SelectionIndex - displayMin)
                renderItem (displayMin+i+1) isSelected listWidth displaySlice.[i] state.RTT.[displayMin + i]
            
            for _ in (Array.length displaySlice) .. (consoleHeight - VerticalPadding) -> 
                blankLine consoleWidth
            
            yield lineOf consoleWidth '-'
            yield " q - Quit without selecting | p - Update RTT (ping) of selection | Enter - Confirm selection" |> trimToLength consoleWidth
            yield lineOf consoleWidth '-'
            
        | Result _ -> 
            yield ""
    }
    |> Seq.map (padToLength consoleWidth ' ')
    |> String.concat "\n"

let private selectVpnInputHandler = function
    | ConsoleKey.DownArrow -> incrementSelection
    | ConsoleKey.UpArrow -> decrementSelection
    | ConsoleKey.Enter -> confirmSelection
    | ConsoleKey.P -> pingSelection
    | ConsoleKey.Q -> resultState None
    | ConsoleKey.Escape -> resultState None
    | _ -> id

let private getKey() = Console.ReadKey().Key

let private updateMenu state =
    match state.Menu with
    | SelectVpn -> state |> selectVpnInputHandler (getKey())
    | Result _ -> state

let rec private menuLoop drawFunction state =
    match state.Menu with
    | Result result -> result
    | _ -> 
        state |> renderMenu |> drawFunction
        state |> updateMenu |> menuLoop drawFunction

/// Shows a menu for choosing from a list of VPN items.
let execRowSelectorMenu drawFunction data =
    { 
        Menu=SelectVpn; 
        Data=data; 
        RTT=data |> Array.map (fun _ -> None)
        SelectionIndex=0
    } 
    |> menuLoop drawFunction 