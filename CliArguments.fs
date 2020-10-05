module Cli

open Argu

type DataSourceType =
    | [<CustomCommandLine("url")>] RemoteUrl
    | [<CustomCommandLine("file")>] LocalPath

type ArgParser = 
    | [<AltCommandLine("-s"); Unique>] Source of DataSourceType*string
    | [<AltCommandLine("-a"); Unique>] AppendConfigs of string list
    | [<AltCommandLine("-r"); Unique>] Regions of string list
with 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Source _ -> "Specify a data source to get VPN list from (either remote URL or local path)"
            | AppendConfigs _ -> "Specify any number of paths to files whose contents should be appended to the OpenVPN config"
            | Regions _ -> "Specify any number of region codes to include in the VPN list. (By default, shows all regions.)"

let parseArgs argv =
    let parser = ArgumentParser.Create<ArgParser>(checkStructure=true)
    try
        parser.ParseCommandLine(inputs=argv) |> Ok
    with
    | :?Argu.ArguParseException as ex -> Error (ex.Message)