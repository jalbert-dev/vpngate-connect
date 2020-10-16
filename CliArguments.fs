module VpnGateConnect.Cli

open Argu

type DataSourceType =
    | [<CustomCommandLine("url")>] RemoteUrl
    | [<CustomCommandLine("file")>] LocalPath

type ArgParser = 
    | [<AltCommandLine("-s"); CustomCommandLine("--source"); Unique>] Source of DataSourceType*string
    | [<AltCommandLine("-a"); CustomCommandLine("--config-paths"); Unique>] AppendConfigs of string list
    | [<AltCommandLine("-r"); CustomCommandLine("--regions"); Unique>] Regions of string list
    | [<AltCommandLine("-p"); CustomCommandLine("--openvpn-path"); Unique>] OpenVpnPath of string
with 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Source _ -> "specify a data source to get VPN list from (either remote URL or local path)"
            | AppendConfigs _ -> "specify any number of paths to files whose contents should be appended to the OpenVPN config"
            | Regions _ -> "specify any number of region codes to include in the VPN list (by default, shows all regions)"
            | OpenVpnPath _ -> "specify the path to OpenVPN"

let parseArgs argv =
    let parser = ArgumentParser.Create<ArgParser>(checkStructure=true)
    try
        parser.ParseCommandLine(inputs=argv) |> Ok
    with
    | :?Argu.ArguParseException as ex -> Error (ex.Message)