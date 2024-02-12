using System.Net.WebSockets;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace StartMobileHotspot
{
    internal class Program
    {
        static void Log(string message)
        {
            Console.WriteLine(message);
        }

        NetworkOperatorTetheringManager TetheringManager = null;
        ConnectionProfile ConnectionProfile = null;
        bool EnsureTetheringManager()
        {
            if (TetheringManager != null)
            {
                return true;
            }
            ConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (ConnectionProfile == null)
            {
                Log($"NOTE: No internet connection detected");
                return false;
            }
            TetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(ConnectionProfile);
            return TetheringManager != null;
        }

        private void Report()
        {
            Log("Mobile Hotspot Report");
            try
            {
                if (!EnsureTetheringManager())
                {
                    return;
                }
                var cap = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(ConnectionProfile);
                Log($"TetheringCapability\t\t: {cap}");
                var isno = NetworkOperatorTetheringManager.IsNoConnectionsTimeoutEnabled();
                Log($"NoConnectionTimeoutEnabled\t: {isno}");
                var state = TetheringManager.TetheringOperationalState;
                Log($"State\t\t\t\t: {state}");
                var conf = TetheringManager.GetCurrentAccessPointConfiguration();
                Log($"SSID\t\t\t\t: {conf.Ssid}");
                Log($"Passphrase\t\t\t: {conf.Passphrase}");
                Log($"Band\t\t\t\t: {conf.Band}");

                var clients = TetheringManager.GetTetheringClients();
                int index = 1;
                Log($"Client Count\t\t\t: {clients.Count}");
                foreach (var client in clients)
                {
                    var names = client.HostNames;
                    var mac = client.MacAddress;
                    Log($"Client {index} MAC\t\t: {mac}");
                    foreach (var name in names)
                    {
                        Log($"Client {index} Name\t\t: {name}");
                    }
                    index++;
                }
            }
            catch (Exception ex)
            {
                Log($"Error: unable to report on hotspot {ex.Message}");
            }
        }


        private async Task StartHotspotWithConfigAsync(string ssid, string passphrase, TetheringWiFiBand band)
        {
            Log($"StartConfig Mobile Hotspot {ssid} {passphrase}");
            try
            {
                if (!EnsureTetheringManager())
                {
                    return;
                }
                var configure = new NetworkOperatorTetheringAccessPointConfiguration()
                {
                    Ssid = ssid,
                    Passphrase = passphrase,
                    Band = band,
                };
                await TetheringManager.ConfigureAccessPointAsync(configure);
                var result = await TetheringManager.StartTetheringAsync();
                Log($"Tether: {result.Status} {result.AdditionalErrorMessage}");
            }
            catch (Exception ex)
            {
                Log($"Error: unable to create hotspot {ex.Message}");
            }
        }
        private async Task StartHotspotSessionAsync(string ssid, string passphrase, TetheringWiFiBand band)
        {
            Log($"StartConfig Mobile Hotspot {ssid} {passphrase}");
            try
            {
                if (!EnsureTetheringManager())
                {
                    return;
                }
                var configure = new NetworkOperatorTetheringSessionAccessPointConfiguration()
                {
                    Ssid = ssid,
                    Passphrase = passphrase,
                    Band = band,
                };
                await TetheringManager.ConfigureAccessPointAsync(configure);
                var result = await TetheringManager.StartTetheringAsync();
                Log($"Tether: {result.Status} {result.AdditionalErrorMessage}");
            }
            catch (Exception ex)
            {
                Log($"Error: unable to create hotspot {ex.Message}");
            }
        }
        private async Task StopAsync()
        {
            Log("Stop Mobile Hotspot");
            try
            {
                if (!EnsureTetheringManager())
                {
                    return;
                }
                var result = await TetheringManager.StopTetheringAsync();
                Log($"Tether: {result.Status} {result.AdditionalErrorMessage}");
            }
            catch (Exception ex)
            {
                Log($"Error: unable to create hotspot {ex.Message}");
            }
        }

        enum Action {  Error, Help, Report, StartConfig, StartSession, Stop };
        static void Main(string[] args)
        {
            var ssid = "starpainter";
            var passphrase = "stellarjay";
            var band = TetheringWiFiBand.FiveGigahertz;
            var action = Action.Help;
            var obj = new Program();
            var errarg = "";

            for (int i=0; i<args.Length; i++)
            {
                var arg = args[i];
                var haveNext = (i + 1) < args.Length;
                var next = haveNext ? args[i + 1] : "";
                switch (arg)
                {
                    case "-help":
                        action = Action.Help;
                        break;
                    case "-action":
                        switch (next)
                        {
                            case "report":
                                action = Action.Report;
                                i++;
                                break;
                            case "start":
                            case "startconfig":
                                action = Action.StartConfig;
                                i++;
                                break;
                            case "startsession":
                                action = Action.StartSession;
                                i++;
                                break;
                            case "stop":
                                action = Action.Stop;
                                i++;
                                break;
                            default:
                                errarg = next;
                                action = Action.Error;
                                break;
                        }
                        break;
                    case "-band":
                        switch (next)
                        {
                            case "auto":
                                band = TetheringWiFiBand.Auto;
                                i++;
                                break;
                            case "2.4":
                                band = TetheringWiFiBand.TwoPointFourGigahertz;
                                i++;
                                break;
                            case "5":
                                band = TetheringWiFiBand.FiveGigahertz;
                                i++;
                                break;
                            case "6":
                                band = TetheringWiFiBand.SixGigahertz;
                                i++;
                                break;
                            default:
                                errarg = next;
                                action = Action.Error;
                                break;
                        }
                        break;
                    case "-password":
                        passphrase = next;
                        i++;
                        break;
                    case "-ssid":
                        ssid = next;
                        i++;
                        break;
                    default:
                        errarg = arg;
                        action = Action.Error; 
                        break;   
                }
                if (action == Action.Error)
                {
                    break;
                }
            }

            const string helpstr = "-action <startconfig|startsession|stop|report> -ssid <name> -password <string> -band <auto|2.4|5|6> ";
            switch (action)
            {
                case Action.Error:
                    Log($"Error: incorrect argument {errarg}\nValid arguments are {helpstr}");
                    break;
                case Action.Help:
                    Log($"StartMobileHotspot {helpstr}");
                    break;
                case Action.Report:
                    obj.Report();
                    break;
                case Action.Stop:
                    var taskp = obj.StopAsync();
                    taskp.Wait();
                    break;
                case Action.StartConfig:
                    var tasks = obj.StartHotspotWithConfigAsync(ssid, passphrase, band);
                    tasks.Wait();
                    break;
            }
        }
    }
}
