using System.Diagnostics;
using Serilog;

namespace OpenNEL_Lite.Utils;

public static class ProcessOutputBinder
{
    public static void Bind(Process process, ILogger logger)
    {
        if (process == null || logger == null) return;
        var si = process.StartInfo;
        si.UseShellExecute = false;
        si.RedirectStandardOutput = true;
        si.RedirectStandardError = true;
        si.RedirectStandardInput = false;
        process.OutputDataReceived += (_, e) => { if (e.Data != null) logger.Information(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) logger.Error(e.Data); };
    }

    public static Process Start(string fileName, string? arguments, ILogger logger)
    {
        var p = new Process();
        p.StartInfo.FileName = fileName;
        p.StartInfo.Arguments = arguments ?? "";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = false;
        Bind(p, logger);
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        return p;
    }
}

