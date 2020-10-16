[<AutoOpen>]
module VpnGateConnect.DomainTypes

open FSharp.Data

// Generating this type from the source URL might be nice, but the source CSV
// uses some non-standard elements that the CsvProvider's parser chokes on,
// so I'm using a locally-stored CSV file that was manually cleaned

/// A type representing a list of VPNs and associated connection information.
type VpnList = CsvProvider<"data/sample_vpn_list.csv">

type ErrorType = 
    | UnexpectedStatusCode of int
    | UnexpectedContentType
    | WebError of string
    | EmptyCsv
    | CsvParseError of string
    | InvalidPath of string
    | CannotOpenFileBecause of string

// The first 16 codepoints of the Unicode private-use area at 0xE000 are used
// internally as color markers. 0xE000 resets color to default; values 0xE001 to
// 0xE010 map to the values of the System.ConsoleColor enum.
[<Literal>]
let ControlCharRangeStart = 0xE000
[<Literal>]
let ControlCharRangeEnd = 0xE010

let encodeDefaultConsoleColor = 
    ControlCharRangeStart |> char
let encodeConsoleColor (color: System.ConsoleColor) = 
    int color + ControlCharRangeStart + 1 |> char
let decodeConsoleColor (c: char) = 
    int c - ControlCharRangeStart - 1 |> enum<System.ConsoleColor>
