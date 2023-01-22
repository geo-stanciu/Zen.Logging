using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Logging.Utils;

internal class Util
{
    public static string GetLocalIPv4(NetworkInterfaceType type = NetworkInterfaceType.Ethernet)
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .FirstOrDefault(ni =>
                ni.NetworkInterfaceType == type
                && ni.OperationalStatus == OperationalStatus.Up
                && ni.GetIPProperties().GatewayAddresses.FirstOrDefault() != null
                && ni.GetIPProperties().UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork) != null
            )
            ?.GetIPProperties()
            .UnicastAddresses
            .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
            ?.Address
            ?.ToString()
            ?? string.Empty;
    }

    public static string GetAppDataDirectory()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public static void CreateDirectoryIfNotExists(string directory)
    {
        if (Directory.Exists(directory))
            return;

        Directory.CreateDirectory(directory);
    }
}
