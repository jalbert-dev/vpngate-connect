module Gui

open System

type private State =
    | SelectVpn
    | Result of VpnList.Row option
type private MenuStatus = { menu: State; data: VpnList.Row array; selectionIndex: int }

let private renderItem selected (row : VpnList.Row) =
    sprintf "[%c]  %s  %3dms, %s (%s)" 
        (if selected then 'X' else ' ')
        row.CountryShort
        (int row.Ping)
        row.``#HostName``
        row.IP

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

let private renderMenu state =
    Console.Clear()
    Console.WriteLine()
    match state.menu with
    | SelectVpn -> state.data.[0..20] |> Array.iteri (fun i x -> renderItem (i = state.selectionIndex) x |> printfn "  %s")
    | Result _ -> ()
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
    execMenu { menu=SelectVpn; data=data; selectionIndex=0 }