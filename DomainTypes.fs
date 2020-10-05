[<AutoOpen>]
module VpnGateConnect.DomainTypes

open FSharp.Data

// Generating this type from the source URL might be nice, but the source CSV
// uses some non-standard elements that the CsvProvider's parser chokes on,
// so I'm using a locally-stored CSV file that has the same schema instead

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
