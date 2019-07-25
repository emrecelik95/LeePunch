using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Lidgren.Network;

namespace LeePunchP2P.Net
{
    public static class LocalNetworkUtils
    {
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern string getLocalWifiIpAddress();
#endif

        public static string GetLocalIPAddress()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        try 
        {
        IPAddress mask;
        IPAddress adr = NetUtility.GetMyAddress(out mask);
        if(!adr.Equals(string.Empty))
            return adr.ToString();
        }
        catch { }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            string adr = getLocalWifiIpAddress();
            if (!adr.Equals(string.Empty))
                return adr.ToString();
        }
        catch { }
#endif


            UnicastIPAddressInformation mostSuitableIp = null;

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var network in networkInterfaces)
            {
                if (network.OperationalStatus != OperationalStatus.Up)
                    continue;

                var properties = network.GetIPProperties();

                if (properties.GatewayAddresses.Count == 0)
                    continue;

                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (IPAddress.IsLoopback(address.Address))
                        continue;

                    if (!address.IsDnsEligible)
                    {
                        if (mostSuitableIp == null)
                            mostSuitableIp = address;
                        continue;
                    }

                    // The best IP is the IP got from DHCP server
                    if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                    {
                        if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            mostSuitableIp = address;
                        continue;
                    }

                    if (address.Address.Equals(string.Empty))
                        continue;

                    return address.Address.ToString();
                }
            }

            return (mostSuitableIp != null) ? mostSuitableIp.Address.ToString() : GetLocalIPDotNet();
        }

        /// <summary>
        /// Get the local Ip address
        /// Taken from Forge Networking Remastered.
        /// https://github.com/BeardedManStudios/ForgeNetworkingRemastered
        /// </summary>
        /// <returns>The Local Ip Address</returns>
        public static string GetLocalIPDotNet()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && IsPrivateIP(ip)) // JM: check for all local ranges
                {
                    localIP = ip.ToString();
                    break;
                }
            }

            return localIP;
        }

        /// Taken from Forge Networking Remastered.
        /// https://github.com/BeardedManStudios/ForgeNetworkingRemastered
        private static bool IsPrivateIP(IPAddress myIPAddress)
        {
            if (myIPAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] ipBytes = myIPAddress.GetAddressBytes();

                // 10.0.0.0/24 
                if (ipBytes[0] == 10)
                {
                    return true;
                }
                // 172.16.0.0/16
                else if (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31)
                {
                    return true;
                }
                // 192.168.0.0/16
                else if (ipBytes[0] == 192 && ipBytes[1] == 168)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
