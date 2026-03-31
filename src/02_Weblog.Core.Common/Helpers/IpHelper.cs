using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
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

    public static string? GetRealIpAddress(
        IHeaderDictionary? headers,
        IPAddress? remoteIpAddress)
    {
        if (headers == null)
            return remoteIpAddress?.ToString();

        // 1. X-Forwarded-For (格式: "client, proxy1, proxy2")
        var xff = headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xff))
        {
            var ip = xff.Split(',')[0].Trim();
            if (IPAddress.TryParse(ip, out _))
                return ip;
        }

        // 2. X-Real-IP (Nginx)
        var xri = headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xri) && IPAddress.TryParse(xri, out _))
            return xri;

        // 3. CF-Connecting-IP (Cloudflare)
        var cfIp = headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp) && IPAddress.TryParse(cfIp, out _))
            return cfIp;

        // 4. True-Client-IP (Akamai, etc.)
        var tcIp = headers["True-Client-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tcIp) && IPAddress.TryParse(tcIp, out _))
            return tcIp;

        // 5. RemoteIpAddress
        return remoteIpAddress?.ToString();
    }

    public static (string? deviceType, string? browser, string? os) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return (null, null, null);

        try
        {
            var clientInfo = UaParser.Parse(userAgent);

            // 操作系统版本 (提前构建，以便传递给 DetectDeviceType)
            var osFamily = clientInfo.OS.Family;
            var osMajor = clientInfo.OS.Major;
            var osPatchMinor = clientInfo.OS.PatchMinor;
            var osVersion = $"{osFamily} {osMajor}";
            if (!string.IsNullOrEmpty(osPatchMinor))
            {
                osVersion += $".{osPatchMinor}";
            }

            // 设备类型检测（传入 osVersion 以返回具体的系统名称）
            var deviceType = DetectDeviceType(clientInfo, userAgent, osVersion);

            // 浏览器检测
            var browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}";
            if (!string.IsNullOrEmpty(clientInfo.UA.Minor) && int.TryParse(clientInfo.UA.Minor, out _))
            {
                browser += $".{clientInfo.UA.Minor}";
            }

            return (deviceType, browser, osVersion);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static string DetectDeviceType(ClientInfo clientInfo, string userAgent, string osVersion)
    {
        var family = clientInfo.Device.Family;
        var osFamily = clientInfo.OS.Family;
        var model = clientInfo.Device.Model;

        // iPhone 系列
        if (family == "iPhone" || (osFamily == "iOS" && userAgent.Contains("iPhone")))
        {
            // 尝试从 UserAgent 中提取具体型号
            var iphoneModel = ExtractiPhoneModel(userAgent);
            if (!string.IsNullOrEmpty(iphoneModel))
                return iphoneModel;
            return "iPhone";
        }

        // iPad
        if (family == "iPad" || (osFamily == "iOS" && (userAgent.Contains("iPad") || userAgent.Contains("iPad"))))
        {
            var ipadModel = ExtractiPadModel(userAgent);
            if (!string.IsNullOrEmpty(ipadModel))
                return ipadModel;
            return "iPad";
        }

        // Android
        if (osFamily == "Android" || family == "Android")
        {
            if (!string.IsNullOrEmpty(model) && model != "Android")
                return model;
            return "Android";
        }

        // macOS - 返回完整版本如 "Mac OS X 14.0"
        if (osFamily == "Mac OS" || osFamily == "macOS")
            return osVersion;

        // Windows - 返回完整版本如 "Windows 10.0.19045"
        if (osFamily == "Windows")
            return osVersion;

        // Linux - 返回 "Linux" + 版本号，如 "Linux 22.04"
        if (osFamily == "Linux" && !userAgent.Contains("Android"))
        {
            // 从 osVersion 提取版本号，格式: "Ubuntu 22.04" 或 "Linux 22.04"
            var parts = osVersion.Split(' ');
            if (parts.Length >= 2)
            {
                // 如果第一个词是 "Linux"，则直接用版本号；否则取发行版名 + 版本号
                if (parts[0] == "Linux")
                    return $"Linux {parts[1]}";
                return $"Linux {parts[1]}"; // 如 "Ubuntu 22.04" → "Linux 22.04"
            }
            return "Linux";
        }

        return "Other";
    }

    private static string? ExtractiPhoneModel(string userAgent)
    {
        // iPhone 模型标识提取
        var patterns = new[]
        {
            @"iPhone\s*(\d+),\s*(\d+)",  // "iPhone 15,2"
            @"iPhone\s*(\d+)",           // "iPhone 15"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(userAgent, pattern);
            if (match.Success)
            {
                var major = match.Groups[1].Value;
                return $"iPhone {major}";
            }
        }

        // 尝试从 ProcessInfo 提取
        if (userAgent.Contains("iPhone14"))
            return "iPhone 13";
        if (userAgent.Contains("iPhone15"))
            return "iPhone 15";
        if (userAgent.Contains("iPhone16"))
            return "iPhone 16";

        return null;
    }

    private static string? ExtractiPadModel(string userAgent)
    {
        if (userAgent.Contains("iPad14"))
            return "iPad";
        if (userAgent.Contains("iPad15"))
            return "iPad";

        return null;
    }

    private static bool IsPrivateIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
            return true;

        // IPv6 loopback
        if (ip == "::1")
            return true;

        // IPv4 loopback
        if (ip == "127.0.0.1" || ip == "0.0.0.0")
            return true;

        // 192.168.x.x
        if (ip.StartsWith("192.168."))
            return true;

        // 10.x.x.x
        if (ip.StartsWith("10."))
            return true;

        // 172.16.x.x - 172.31.x.x
        if (ip.StartsWith("172."))
        {
            var parts = ip.Substring(5).Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0], out var second))
            {
                if (second >= 16 && second <= 31)
                    return true;
            }
        }

        // 169.254.x.x (link-local)
        if (ip.StartsWith("169.254."))
            return true;

        return false;
    }

    public static string? GetIpLocation(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        // 跳过内网 IP
        if (IsPrivateIp(ipAddress))
            return null;

        try
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "ip2region_v4.xdb");
            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"[IpHelper] ip2region_v4.xdb not found at: {dbPath}");
                return null;
            }

            var region = SearchIp2Region(ipAddress, dbPath);
            if (string.IsNullOrEmpty(region))
                return null;

            // 解析返回格式: "国家|省|市"
            var parts = region.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                // 跳过国家，返回 "省|市" 格式
                var province = parts[1];
                var city = parts[2];

                // 去掉 "省"、"市" 等后缀以简化
                province = province.Replace("省", "").Replace("市", "").Replace("特别行政区", "").Replace("自治区", "");

                // 如果是直辖市，直接返回
                if (city == "北京市" || city == "上海市" || city == "天津市" || city == "重庆市")
                    return city.Replace("市", "");

                return province + city.Replace("市", "");
            }

            return region.Replace("|", "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IpHelper] SearchIp2Region error: {ex.Message}");
            return null;
        }
    }

    private static string? SearchIp2Region(string ip, string dbPath)
    {
        try
        {
            // 简单的 ip2region 二分查找实现
            // 完整实现建议使用官方库: https://github.com/lionsoul2014/ip2region

            using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // 跳过 header (4068 bytes)
            fs.Position = 4068;

            // 读取索引区
            var indexLength = (int)(fs.Length - 4068) / 8;
            var ipNum = IPToNumber(ip);

            var low = 0;
            var high = indexLength - 1;
            var result = "";

            while (low <= high)
            {
                var mid = (low + high) / 2;
                fs.Position = 4068 + mid * 8;
                var startIp = ReadUint32(br);
                var endIp = ReadUint32(br);
                var dataPtr = ReadUint32(br);

                if (ipNum >= startIp && ipNum <= endIp)
                {
                    fs.Position = dataPtr;
                    var dataLen = br.ReadByte();
                    var region = Encoding.UTF8.GetString(br.ReadBytes(dataLen));
                    return region;
                }
                else if (ipNum < startIp)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static uint ReadUint32(BinaryReader br)
    {
        var bytes = br.ReadBytes(4);
        return BitConverter.IsLittleEndian
            ? BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0)
            : BitConverter.ToUInt32(bytes, 0);
    }

    private static uint IPToNumber(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4)
            return 0;

        return ((uint.Parse(parts[0]) << 24) |
                (uint.Parse(parts[1]) << 16) |
                (uint.Parse(parts[2]) << 8) |
                uint.Parse(parts[3]));
    }

    public static string GenerateFingerprint(string? ip, string? userAgent)
    {
        var data = $"{ip}|{userAgent}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash)[..16];
    }
}