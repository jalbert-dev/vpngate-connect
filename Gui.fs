module Gui

open System

type private State =
    | SelectVpn
    | Result of VpnList.Row option
type private MenuStatus = { 
    Menu: State
    Data: VpnList.Row array
    SelectionIndex: int
}

let private trimToLength width str =
    if String.length str > width then
        str.[..width-4] + "..."
    else
        str

let private padToLength width (padChar : Char) str =
    str + String.replicate (width - String.length str) (string padChar)

let private lineOf width char =
    String.Empty |> padToLength width char

let private blankLine width = lineOf width ' '

let private renderItem index selected maxWidth (row : VpnList.Row)  =
    sprintf "  [%c] %3d. %s  %3dms, %s (%s)" 
        (if selected then 'X' else ' ')
        index
        row.CountryShort
        (int row.Ping)
        row.``#HostName``
        row.IP
    |> trimToLength maxWidth

let private resultState result state = 
    { state with Menu=Result result }

let private incrementSelection state = 
    { state with SelectionIndex=min (state.Data.Length-1) (state.SelectionIndex+1)}

let private decrementSelection state = 
    { state with SelectionIndex=max 0 (state.SelectionIndex-1)}

let private confirmVpn state =
    // TODO: SelectVpn should probably own a seq that's a subslice of the data which is what's actually displayed.
    //       I guess we can regenerate this any time the filter/sort criteria change.
    state |> resultState (Some state.Data.[state.SelectionIndex])

let private listDimensions termWidth termHeight = (termWidth - 2, termHeight - 7)

let private renderMenu state =
    let cw = Console.WindowWidth
    let ch = Console.WindowHeight

    seq {
        match state.Menu with
        | SelectVpn ->
            yield "  Select a VPN endpoint to connect to" |> trimToLength cw
            yield lineOf cw '-'

            let listW, listH = listDimensions cw ch
            let displayMin = max 0 (state.SelectionIndex - listH / 2)
            let displayMax = min (state.Data.Length - 1) (displayMin + listH)

            let displaySlice = state.Data.[displayMin..displayMax]
            for i in 0..displaySlice.Length-1 ->
                let isSelected = (i = state.SelectionIndex - displayMin)
                renderItem (displayMin+i+1) isSelected listW displaySlice.[i]
            
            for _ in (Array.length displaySlice) .. (ch - 7) -> 
                blankLine cw
            
            yield lineOf cw '-'
            yield " q - Quit without selecting | Enter - Confirm selection" |> trimToLength cw
            yield lineOf cw '-'
            
        | Result _ -> 
            yield ""
    }
    |> Seq.map (padToLength cw ' ')
    |> String.concat "\n"

let private selectVpnInputHandler = function
    | ConsoleKey.DownArrow -> incrementSelection
    | ConsoleKey.UpArrow -> decrementSelection
    | ConsoleKey.Enter -> confirmVpn
    | ConsoleKey.Q -> resultState None
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
    
let execRowSelectorMenu drawFunction data =
    { Menu=SelectVpn; Data=data; SelectionIndex=0 } |> menuLoop drawFunction 