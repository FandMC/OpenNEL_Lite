/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using OpenNEL_Lite.type;
using Microsoft.Win32;
using System.Net;

namespace OpenNEL_Lite.Utils;

internal static class Hwid
{
    public static string Compute()
    {
        try
        {
            var os = Environment.OSVersion.VersionString;
            var cpu = Environment.ProcessorCount.ToString();
            var guid = GetMachineGuid();
            var s = string.Join("|", new[] { os, cpu, guid });
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "计算HWID失败");
            using var sha = SHA256.Create();
            var fallbackGuid = GetMachineGuid();
            var s = string.Join("|", new[] { Environment.OSVersion.VersionString, Environment.ProcessorCount.ToString(), fallbackGuid });
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(hash);
        }
    }

    public static async Task<string?> ReportAsync(string? hwid = null, string? endpoint = null)
    {
        try
        {
            var h = hwid ?? Compute();
            var ip = GetLocalIp();
            var url = endpoint ?? AppInfo.HwidEndpoint;
            using var client = new HttpClient();
            var payload = "{\"hwid\":\"" + h + "\",\"ip\":\"" + ip + "\"}";
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            return text;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    static string GetMachineGuid()
    {
        try
        {
            using var lm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var crypt64 = lm64.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", false);
            var g64 = crypt64?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(g64)) return g64!;

            using var lm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            using var crypt = lm.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", false);
            var g = crypt?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrWhiteSpace(g)) return g!;
            return "";
        }
        catch
        {
            return "";
        }
    }

    static string GetLocalIp()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var a in host.AddressList)
            {
                if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return a.ToString();
                }
            }
            return "";
        }
        catch
        {
            return "";
        }
    }

}
