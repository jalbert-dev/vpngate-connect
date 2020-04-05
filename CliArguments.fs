module Cli

open Argu

type DataSourceType =
    | [<CustomCommandLine("url")>] RemoteUrl
    | [<CustomCommandLine("file")>] LocalPath

type ArgParser = 
    | [<AltCommandLine("-s"); Unique>] Source of DataSourceType*string
with 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Source _ -> "Specify a data source to get VPN list from (either remote URL or local path)"

let parseArgs argv =
    let parser = ArgumentParser.Create<ArgParser>(checkStructure=true)
    try
        parser.ParseCommandLine(inputs=argv) |> Ok
    with
    | :?Argu.ArguParseException as ex -> Error (ex.Message)