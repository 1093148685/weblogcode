using UAParser;

namespace Weblog.Core.Common.Helpers;

public static class IpHelper
{
    private static Parser? _uaParser;
    private static readonly object _lock = new();

    private static Parser UaParser
    {
        get
        {
            if (_uaParser == null)
            {
                lock (_lock)
                {
                    _uaParser ??= Parser.GetDefault();
                }
            }
            return _uaParser;
        }
    }

    public static (string? deviceType, string? browser) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return (null, null);

        try
        {
            var clientInfo = UaParser.Parse(userAgent);
            
            var deviceType = clientInfo.Device.Family switch
            {
                "iPhone" or "iPod" => "iPhone",
                "iPad" => "iPad",
                "Android" => "Android",
                "Other" when clientInfo.OS.Family == "Android" => "Android",
                "Other" when clientInfo.OS.Family == "iOS" => "iPhone",
                _ => "PC"
            };

            var browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}";
            if (clientInfo.UA.Minor != null && int.TryParse(clientInfo.UA.Minor, out _))
            {
                browser += $".{clientInfo.UA.Minor}";
            }

            return (deviceType, browser);
        }
        catch
        {
            return (null, null);
        }
    }

    public static string? GetIpLocation(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        // 本地 IP 不显示归属地
        if (ipAddress == "::1" || ipAddress == "127.0.0.1" || ipAddress == "0.0.0.0")
            return null;

        // 注意：IP 属地查询需要 ip2region.db 文件
        // 如需启用，请：
        // 1. 下载 ip2region.db: https://github.com/lionsoul2014/ip2region/releases
        // 2. 放置到项目运行目录
        // 3. 取消下面代码的注释
        
        // try
        // {
        //     var region = Ip2RegionHelper.Search(ipAddress);
        //     if (!string.IsNullOrWhiteSpace(region))
        //     {
        //         var parts = region.Split('|');
        //         if (parts.Length >= 4)
        //         {
        //             var province = parts[3];
        //             province = province.Replace("省", "").Replace("市", "").Replace("自治区", "").Replace("壮族", "").Replace("回族", "").Replace("维吾尔", "");
        //             if (province.Contains("内蒙古")) province = "内蒙古";
        //             else if (province.Contains("黑龙江")) province = "黑龙江";
        //             return province;
        //         }
        //     }
        // }
        // catch { }

        return null;
    }
}

public static class Ip2RegionHelper
{
    private static string? _dbPath;
    private static readonly object _lock = new();

    public static string? Search(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        try
        {
            var dbPath = GetDbPath();
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                return null;

            using var reader = new BinaryReader(File.OpenRead(dbPath));
            
            var headerSip = new byte[4];
            var headerLip = new byte[4];
            var lLength = new byte[4];
            var offset = new byte[4];

            reader.ReadBytes(8);
            reader.Read(headerSip, 0, 4);
            reader.Read(headerLip, 0, 4);

            var start = IP2Long(headerSip);
            var end = IP2Long(headerLip);
            var ipNum = IP2Long(StringToIP(ip));

            if (ipNum < start || ipNum > end)
                return null;

            var left = start;
            var right = end;
            var mid = 0L;

            while (left <= right)
            {
                mid = (left + right) / 2;

                reader.BaseStream.Position = 8 + mid * 12;
                reader.Read(headerSip, 0, 4);
                reader.Read(lLength, 0, 4);
                reader.Read(offset, 0, 4);

                var sip = IP2Long(headerSip);
                var lip = IP2Long(lLength);
                var loffset = IP2Long(offset);

                if (ipNum < sip)
                {
                    right = mid - 1;
                }
                else if (ipNum > lip)
                {
                    left = mid + 1;
                }
                else
                {
                    reader.BaseStream.Position = loffset;
                    var dataLen = reader.ReadByte();
                    var data = reader.ReadBytes(dataLen);
                    return System.Text.Encoding.UTF8.GetString(data);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetDbPath()
    {
        if (_dbPath != null)
            return _dbPath;

        lock (_lock)
        {
            if (_dbPath != null)
                return _dbPath;

            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "ip2region.db"),
                Path.Combine(AppContext.BaseDirectory, "Data", "ip2region.db"),
                Path.Combine(Directory.GetCurrentDirectory(), "ip2region.db"),
                "ip2region.db"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _dbPath = path;
                    return _dbPath;
                }
            }

            _dbPath = possiblePaths[0];
            return _dbPath;
        }
    }

    private static long IP2Long(byte[] ip)
    {
        return ((long)ip[0] << 24) | ((long)ip[1] << 16) | ((long)ip[2] << 8) | ip[3];
    }

    private static string StringToIP(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4)
            return string.Empty;

        return string.Join(".", 
            int.Parse(parts[0]), 
            int.Parse(parts[1]), 
            int.Parse(parts[2]), 
            int.Parse(parts[3]));
    }
}