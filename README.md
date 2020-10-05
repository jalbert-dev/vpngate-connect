# vpngate-connect
A *very* simple CLI frontend for using OpenVPN with VPN Gate endpoints

![Screenshot of the menu in action](../media/1.jpg?raw=true)

## Usage

By default, the program will download the current list of VPNs from the [VPN Gate site](http://www.vpngate.net/api/iphone) and display it as a scrollable menu. Once the user selects an endpoint from the menu, the frontend passes the corresponding config to OpenVPN.

The ping displayed in the menu is from the source list, but the user can press a key to manually send a ping and display the RTT to the selected server.

Note that OpenVPN by default requires root privileges to create its tunnels. `vpngate-connect` invokes OpenVPN, and must therefore be run with whatever privileges OpenVPN requires.

There are a few command line options that can be used to customize the frontend's functionality:
* Custom source for VPN list data from a custom URL or file on disk (CSV, following VPN Gate's schema)
* Merge additional config files into the downloaded OpenVPN config (useful for machine-specific settings)
* Filter VPN list by region
* Custom invoke path (defaults to "openvpn")

## Building

Being a basic .NET Core app, as long as you've got a recent version of .NET Core installed, `dotnet build` should do the trick.

This app has only been built and tested on Linux, although being as simple as it is means it should work on any typical desktop OS, provided OpenVPN is available. (I don't know that there's any real need to use it on Windows, though, since there's already a perfectly serviceable official frontend.)

### Dependencies:
* [Argu](https://fsprojects.github.io/Argu/) (command line argument parsing)
* [FSharp.Data](https://fsharp.github.io/FSharp.Data/) (HTTP request/response, CSV type provider)

Both dependencies are automatically acquired by Nuget as part of the normal build process.
