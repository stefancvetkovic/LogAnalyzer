using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    public class ResolveDnsOptimization
    {
        public static void ApplyTo(SocketsHttpHandler handler)
        {
            CachedAddress cachedAddress = null;

            // Remove the latencies when using host name over IP address
            // Changing pool connection lifetime and forcing to open them all does not work, the DNS resolution is always done.
            // Source: https://stackoverflow.com/a/70475741/1529139
            handler.ConnectCallback = async (context, cancellationToken) =>
            {
                if (cachedAddress == null || cachedAddress.Host != context.DnsEndPoint.Host)
                {
                    // Use DNS to look up the IP address(es) of the target host and filter for IPv4 addresses only
                    IPHostEntry ipHostEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host);
                    IPAddress ipAddress = ipHostEntry.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                    if (ipAddress == null)
                    {
                        cachedAddress = null;
                        throw new Exception($"No IP4 address for {context.DnsEndPoint.Host}");
                    }
                    cachedAddress = new CachedAddress() { Ip = ipAddress, Host = context.DnsEndPoint.Host };
                }

                TcpClient tcp = new();
                await tcp.ConnectAsync(cachedAddress.Ip, context.DnsEndPoint.Port, cancellationToken);
                return tcp.GetStream();
            };
        }

        private class CachedAddress
        {
            public IPAddress Ip;
            public string Host;
        }
    }
}