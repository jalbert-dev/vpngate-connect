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
let private renderItem selected maxWidth (row : VpnList.Row)  =
    sprintf "  [%c]  %s  %3dms, %s (%s)" 
        (if selected then 'X' else ' ')
        row.CountryShort
        (int row.Ping)
        row.``#HostName``
        row.IP
    |> trimToLength maxWidth
let private clearConsoleContents w h =
    Console.SetCursorPosition(0, 0)
    for y = 1 to h-1 do
        Console.WriteLine(String.replicate (w - 1) " ")
    Console.SetCursorPosition(0, 0)

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

let private listDimensions termWidth termHeight = (termWidth - 2, termHeight - 4)

let private renderMenu state =
    let cw = Console.WindowWidth
    let ch = Console.WindowHeight

    clearConsoleContents cw ch

    match state.menu with
    | SelectVpn ->
        let listW, listH = listDimensions cw ch
        let displayMin = max 0 (state.selectionIndex - listH / 2)
        let displayMax = min (state.data.Length - 1) (displayMin + listH)

        let displaySlice = state.data.[displayMin..displayMax]
        displaySlice |> Array.iteri (fun i x -> renderItem (i = state.selectionIndex - displayMin) listW x |> printfn "%s")
    | Result _ -> 
        ()
    state

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

let rec private execMenu state =
    match state.menu with
    | Result result -> result
    | _ -> 
        state
        |> renderMenu
        |> updateMenu
        |> execMenu
    
let showMenu data = 
    Console.Clear()
    Console.CursorVisible <- false
    let rv = execMenu { menu=SelectVpn; data=data; selectionIndex=0 }
    Console.CursorVisible <- true
    rv