namespace VpnGateConnect

open VpnGateConnect
open Argu

type Config = {
    DataSource: Cli.DataSourceType * string;
    AllowedRegions: string array;
    ConfigPaths: string array;
    OpenVpnPath: string;
}

module Config =
    [<Literal>] 
    let private DefaultOpenVpnPath = "openvpn"
    let private defaultVpnListSource = (Cli.RemoteUrl, "https://www.vpngate.net/api/iphone")

    /// Constructs a configuration object from a completed argument parse.
    let fromArgs (args: ParseResults<Cli.ArgParser>) = { 
            DataSource = 
                args.TryGetResult(<@ Cli.Source @>) 
                |> Option.defaultValue defaultVpnListSource; 

            AllowedRegions = 
                args.TryGetResult(<@ Cli.Regions @>) 
                |> Option.map List.toArray 
                |> Option.defaultValue [||] 
                |> Array.map (fun x -> x.ToLower()); 

            ConfigPaths = 
                args.TryGetResult(<@ Cli.AppendConfigs @>) 
                |> Option.map List.toArray 
                |> Option.defaultValue [||];

            OpenVpnPath =
                args.TryGetResult(<@ Cli.OpenVpnPath @>)
                |> Option.defaultValue DefaultOpenVpnPath
    }