using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Manager;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.type;
using Serilog;
using OpenNEL_Lite.Utils;

namespace OpenNEL_Lite;

internal class Program
{
    static async Task Main(string[] args){
        ConfigureRuntime();
        ConsoleBinder.Bind(args);
        ConfigureLogger();
        await Hwid.ReportAsync();
        AppState.Debug = IsDebug();
        Log.Information("OpenNEL github: {github}",AppInfo.GithubUrL);
        Log.Information("版本: {version}",AppInfo.AppVersion);
        Log.Information("QQ群: {qqgroup}",AppInfo.QQGroup);
        Log.Information("本项目遵循 GNU GPL 3.0 协议开源");
        Log.Information("https://www.gnu.org/licenses/gpl-3.0.zh-cn.html");
        Log.Information(
            "\n" +
            "OpenNEL_Lite  Copyright (C) 2025 OpenNEL_Lite Studio" +
            "\n" +
            "本程序是自由软件，你可以重新发布或修改它，但必须：" +
            "\n" +
            "- 保留原始版权声明" +
            "\n" +
            "- 采用相同许可证分发" +
            "\n" +
            "- 提供完整的源代码");
        
        int port = 8080;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (string.Equals(a, "--port", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p)) port = p;
            }
            else if (a.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
            {
                var v = a.Substring("--port=".Length);
                if (int.TryParse(v, out var p)) port = p;
            }
        }
        TcpServer server = new TcpServer(port, "/gateway", Log.Logger);
        await server.StartAsync();
        await InitializeSystemComponentsAsync();
        AppState.Services = await CreateServices();
        await AppState.Services.X19.InitializeDeviceAsync();

        await Task.Delay(Timeout.Infinite);
    }

    static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
    }
    
    static async Task InitializeSystemComponentsAsync()
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));
            Interceptor.EnsureLoaded();
            PacketManager.Instance.EnsureRegistered();
            try
            {
                PluginManager.Instance.EnsureUninstall();
                PluginManager.Instance.LoadPlugins("plugins");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "插件加载失败");
            }
            await Task.CompletedTask;
        }

    static async Task<Services> CreateServices()
    {
        var c4399 = new C4399();
        var x19 = new X19();

        var yggdrasil = new StandardYggdrasil(new YggdrasilData
        {
            LauncherVersion = x19.GameVersion,
            Channel = "netease",
            CrcSalt = await CrcSalt.Compute()
        });

        return new Services(c4399, x19, yggdrasil);
    }
    public static bool IsDebug()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var a in args)
            {
                if (string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch { }
        var env = Environment.GetEnvironmentVariable("NEL_DEBUG");
        return string.Equals(env, "1") || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }
    static void ConfigureRuntime()
    {
        Environment.SetEnvironmentVariable("COMPlus_UseSpecialUserModeApc", "0");
    }
}
