module Cli

open Argu

type DataSourceType =
    | [<CustomCommandLine("url")>] RemoteUrl
    | [<CustomCommandLine("file")>] LocalPath

type ArgParser = 
    | [<AltCommandLine("-s"); Unique>] Source of DataSourceType*string
    | [<AltCommandLine("-a"); Unique>] Append of string list
with 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Source _ -> "Specify a data source to get VPN list from (either remote URL or local path)"
            | Append _ -> "Specify any number of paths to files whose contents should be appended to the OpenVPN config"

let parseArgs argv =
    let parser = ArgumentParser.Create<ArgParser>(checkStructure=true)
    try
        parser.ParseCommandLine(inputs=argv) |> Ok
    with
    | :?Argu.ArguParseException as ex -> Error (ex.Message)