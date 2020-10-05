namespace VpnGateConnect

type ProgramFlow<'a> = 
    | ContinueData of 'a
    | ExecResult of int * string option

module ProgramFlow =
    let bind f x =
        match x with
        | ContinueData data -> f data
        | ExecResult (x, y) -> ExecResult (x, y)
    let map f = bind (f >> ContinueData)
    let unwrap = function
        | ContinueData data -> data
        | ExecResult (x, y) -> failwith (sprintf "Attempted to unwrap result (%d, %A)!!" x y)

    module Operators =
        let (>>=) a b = bind b a
        let (<!>) a b = map b a

    let normalExit = ExecResult (0, None)
    let normalExitWithMsg msg = ExecResult (0, Some msg)
    let runtimeError msg = ExecResult (1, Some msg)
    let usageError msg = ExecResult (2, Some msg)