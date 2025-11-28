namespace OpenNEL_Lite.type;
using System.Threading;

internal class ChannelInfo
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public CancellationTokenSource Cts { get; set; } = new CancellationTokenSource();
    public string PlayerId { get; set; } = string.Empty;
    public string ForwardHost { get; set; } = string.Empty;
    public int ForwardPort { get; set; }
    public int LocalPort { get; set; }
    public dynamic? Connection { get; set; }
    public Guid Identifier { get; set; }
}