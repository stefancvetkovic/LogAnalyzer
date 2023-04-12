// See https://aka.ms/new-console-template for more information
using LogAnalyzer;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

Console.WriteLine("Welcome to IIS LogAnalyzer!");
Console.WriteLine("-----------------------------------------------");
Console.WriteLine("Do you want me to load the log file? (Y/N)");

ConsoleKeyInfo cki = Console.ReadKey();

if (cki.Key.ToString().ToLower() == "y")
{
    //load .log file from predefined path

    var path = Environment.CurrentDirectory + "/LogFile/ex120326.log";

    List<IPAddressCustom> ips = new();
    List<string> notSuccessIps = new();

    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    using (BufferedStream bs = new BufferedStream(fs))
    using (StreamReader sr = new StreamReader(bs))
    {
        string line;
        while ((line = sr.ReadLine()!) != null)
        {
            //extract your IP from the line string
            var ipAdress = GetIpAddressByLine(line);

            if (ipAdress != null)
            {
                var record = ips.Where(x => x.IpAddress.Equals(ipAdress)).FirstOrDefault();
                if (record != null)
                {
                    record.Hint++;
                }
                else
                {
                    ips.Add(new IPAddressCustom
                    {
                        IpAddress = ipAdress,
                        Hint = 1
                    });
                }
            }
        }
    }

    var orderedIps = ips.OrderByDescending(x => x.Hint);

    Console.WriteLine("Preliminary records:");
    foreach (var item in orderedIps)
    {
        item.DnsName = await GetDnsName(item.IpAddress);
        Console.WriteLine($"{item.DnsName} ({item.IpAddress}) - {item.Hint}");
    }

    async Task<string> GetDnsName(IPAddress ipAddress)
    {
        try
        {
            var stringResult = await Dns.GetHostEntryAsync(ipAddress);

            return stringResult.HostName;
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            return ($"DNS Unreachable. An error has occured. {ex.Message}");
        }
    }

    IPAddress GetIpAddressByLine(string line)
    {
        string ipAddress = string.Empty;
        IPAddress ipAddressFormated = null;
        if (line.Contains("GET") || line.Contains("POST") || line.Contains("PUT") || line.Contains("PATCH") || line.Contains("DELETE"))
        {
            ipAddress = line.Split(' ')[2].Trim(new char[] { '[', ']', ' ' });
        }


        if (IPAddress.TryParse(ipAddress, out ipAddressFormated))
        {
            return ipAddressFormated;
        }

        return null;
    }
}
else
{
    Console.WriteLine("Goodbye!");
}