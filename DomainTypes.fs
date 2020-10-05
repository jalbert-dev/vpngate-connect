[<AutoOpen>]
module DomainTypes

open FSharp.Data

type VpnList = CsvProvider<"data/sample_vpn_list.csv">

type ErrorType = 
    | UnexpectedStatusCode of int
    | UnexpectedContentType
    | WebError of string
    | EmptyCsv
    | CsvParseError of string
    | InvalidPath of string
    | CannotOpenFileBecause of string
