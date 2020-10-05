namespace VpnGateConnect

open VpnGateConnect
open Argu

type Config = {
    DataSource: Cli.DataSourceType * string;
    AllowedRegions: string array;
    ConfigPaths: string array;
}

module Config =
    let private defaultVpnListSource = (Cli.RemoteUrl, "https://www.vpngate.net/api/iphone")

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
    }