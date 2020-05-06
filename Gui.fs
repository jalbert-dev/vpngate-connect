module Gui

open System

type private State =
    | SelectVpn
    | Result of VpnList.Row option
type private MenuStatus = { 
    menu: State
    data: VpnList.Row array
    selectionIndex: int
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
    { state with menu=Result result }

let private incrementSelection state = 
    { state with selectionIndex=min (state.data.Length-1) (state.selectionIndex+1)}

let private decrementSelection state = 
    { state with selectionIndex=max 0 (state.selectionIndex-1)}

let private confirmVpn state =
    // TODO: SelectVpn should probably own a seq that's a subslice of the data which is what's actually displayed.
    //       I guess we can regenerate this any time the filter/sort criteria change.
    state |> resultState (Some state.data.[state.selectionIndex])

let private listDimensions termWidth termHeight = (termWidth - 2, termHeight - 7)

let private renderMenu state =
    let cw = Console.WindowWidth
    let ch = Console.WindowHeight

    seq {
        match state.menu with
        | SelectVpn ->
            yield "  Select a VPN endpoint to connect to" |> trimToLength cw
            yield lineOf cw '-'

            let listW, listH = listDimensions cw ch
            let displayMin = max 0 (state.selectionIndex - listH / 2)
            let displayMax = min (state.data.Length - 1) (displayMin + listH)

            let displaySlice = state.data.[displayMin..displayMax]
            for i in 0..displaySlice.Length-1 ->
                renderItem (displayMin+i+1) (i = state.selectionIndex - displayMin) listW displaySlice.[i]
            
            for i in (Array.length displaySlice) .. (ch - 7) -> blankLine cw
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
    match state.menu with
    | SelectVpn -> state |> selectVpnInputHandler (getKey())
    | Result _ -> state

let rec private execMenu drawFunction state =
    match state.menu with
    | Result result -> result
    | _ -> 
        state |> renderMenu |> drawFunction
        state |> updateMenu |> execMenu drawFunction
    
let showMenu drawFunction data =
    execMenu drawFunction { menu=SelectVpn; data=data; selectionIndex=0 }