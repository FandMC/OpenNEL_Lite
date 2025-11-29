using System.Runtime.InteropServices;
using Serilog;
using System.Text;

namespace OpenNEL_Lite.Utils;

public static class ConsoleBinder
{
    public static void Bind()
    {
        if (HasConsole()) return;

        var originalOut = Console.Out;
        var originalErr = Console.Error;

        if (!AttachConsole(0xFFFFFFFF)) AllocConsole();
        
        try
        {
            var hIn = CreateFileW("CONIN$", 0x80000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
            var hOut = CreateFileW("CONOUT$", 0x40000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
            var hErr = CreateFileW("CONOUT$", 0x40000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (hIn != INVALID_HANDLE_VALUE) SetStdHandle(-10, hIn);
            if (hOut != INVALID_HANDLE_VALUE) SetStdHandle(-11, hOut);
            if (hErr != INVALID_HANDLE_VALUE) SetStdHandle(-12, hErr);
        }
        catch
        {
        }
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            
            var consoleOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            var consoleErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };

            Console.SetOut(new MultiTextWriter(consoleOut, originalOut));
            Console.SetError(new MultiTextWriter(consoleErr, originalErr));
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

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    class MultiTextWriter : TextWriter
    {
        private readonly TextWriter[] _writers;
        public MultiTextWriter(params TextWriter[] writers) => _writers = writers;
        public override Encoding Encoding => _writers[0].Encoding;
        public override void Write(char value) { foreach (var w in _writers) try { w.Write(value); } catch { } }
        public override void Write(string? value) { foreach (var w in _writers) try { w.Write(value); } catch { } }
        public override void Write(char[] buffer, int index, int count) { foreach (var w in _writers) try { w.Write(buffer, index, count); } catch { } }
        public override void WriteLine(string? value) { foreach (var w in _writers) try { w.WriteLine(value); } catch { } }
        public override void Flush() { foreach (var w in _writers) try { w.Flush(); } catch { } }
    }
}
