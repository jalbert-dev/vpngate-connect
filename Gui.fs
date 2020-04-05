module Gui

open System

type CurrentMenu =
    | SelectVpn
    | Done
type private MenuState = { menu: CurrentMenu; data: VpnList.Row array; selectionIndex: int }

let private renderItem selected (row : VpnList.Row) =
    sprintf "[%c]  %s  %3dms, %s (%s)" 
        (if selected then 'X' else ' ')
        row.CountryShort
        (int row.Ping)
        row.``#HostName``
        row.IP

let private doneState state = 
    { state with menu=Done }
let private incrementSelection state = 
    { state with selectionIndex=min (state.data.Length-1) (state.selectionIndex+1)}
let private decrementSelection state = 
    { state with selectionIndex=max 0 (state.selectionIndex-1)}
let private confirmVpn state =
    let selected = state.data.[state.selectionIndex]
    printfn "Selected %s" (selected.``#HostName``)
    selected.OpenVPN_ConfigData_Base64
    |> Convert.FromBase64String
    |> Text.Encoding.UTF8.GetString
    |> printfn "Payload:\n%s"
    state |> doneState

let private renderMenu state =
    Console.Clear()
    Console.WriteLine()
    match state.menu with
    | SelectVpn -> state.data.[0..20] |> Array.iteri (fun i x -> renderItem (i = state.selectionIndex) x |> printfn "  %s")
    | Done -> ()
    state

let private selectVpnInputHandler = function
    | ConsoleKey.DownArrow -> incrementSelection
    | ConsoleKey.UpArrow -> decrementSelection
    | ConsoleKey.Enter -> confirmVpn
    | ConsoleKey.Q -> doneState
    | _ -> id

let private getKey() = Console.ReadKey().Key

let private updateMenu state =
    match state.menu with
    | SelectVpn -> state |> selectVpnInputHandler (getKey())
    | Done -> state

let rec private execMenu state =
    match state.menu with
    | Done -> ()
    | _ -> 
        state
        |> renderMenu
        |> updateMenu
        |> execMenu
    
let showMenu data = 
    execMenu { menu=SelectVpn; data=data; selectionIndex=0 }