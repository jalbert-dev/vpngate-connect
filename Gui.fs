module Gui

open System

type private MenuState = { data: VpnList.Row array; selectionIndex: int }

let showMenu data = 
    let rec showMenu' state =
        Console.Clear()
        printfn "%d X %d" Console.WindowWidth Console.WindowHeight
        match Console.ReadKey().Key with
        | ConsoleKey.Q -> printfn "Done."
        | _ -> showMenu' state
    showMenu' { data=data; selectionIndex=0 }