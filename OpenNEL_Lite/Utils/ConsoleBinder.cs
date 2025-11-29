using System.Runtime.InteropServices;
using Serilog;

namespace OpenNEL_Lite.Utils;

public static class ConsoleBinder
{
    public static void Bind()
    {
        if (HasConsole()) return;
        if (!AttachConsole(0xFFFFFFFF)) AllocConsole();
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            var stdout = Console.OpenStandardOutput();
            var stderr = Console.OpenStandardError();
            var outWriter = new StreamWriter(stdout) { AutoFlush = true };
            var errWriter = new StreamWriter(stderr) { AutoFlush = true };
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
        }
        catch
        {
        }
        Log.Information("控制台已绑定");
    }

    static bool HasConsole()
    {
        try
        {
            var _ = Console.BufferWidth;
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AllocConsole();
}

