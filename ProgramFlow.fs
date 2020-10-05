namespace VpnGateConnect

/// Models program execution as a flow of data, terminating with some result value.
type ProgramFlow<'a> = 
    /// For passing data to next step in some process.
    | ContinueData of 'a
    /// For terminating execution with some status code and optional message.
    | ExecResult of int * string option

module ProgramFlow =
    let bind f x =
        match x with
        | ContinueData data -> f data
        | ExecResult (x, y) -> ExecResult (x, y)
    let map f = bind (f >> ContinueData)

    module Operators =
        let (>>=) a b = bind b a
        let (<!>) a b = map b a

    /// Creates a ProgramFlow termination representing POSIX success.
    let normalExit = ExecResult (0, None)
    /// Creates a ProgramFlow termination representing POSIX success with some message.
    let normalExitWithMsg msg = ExecResult (0, Some msg)
    /// Creates a ProgramFlow termination representing a runtime error (code 1) with some message.
    let runtimeError msg = ExecResult (1, Some msg)
    /// Creates a ProgramFlow termination representing a usage error (code 2) with some message.
    let usageError msg = ExecResult (2, Some msg)