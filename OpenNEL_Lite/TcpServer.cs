using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using OpenNEL_Lite.Message;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite;

internal class TcpServer
{
    private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();
    private CancellationTokenSource? _cts;
    private TcpListener? _tcpListener;
    private volatile bool _running;
    private int _currentPort;
    private readonly ILogger _logger;
    private readonly string _path;
    
    public TcpServer(int defaultPort = 8080, string path = "/", ILogger? logger = null)
    {
        _path = path;
        _clients = new ConcurrentDictionary<Guid, TcpClient>();
        _logger = logger ?? Log.Logger;
        _currentPort = defaultPort;
    }
    
    public async Task StartAsync(bool listenAll = false)
    {
        if (_running) return;
        _cts = new CancellationTokenSource();
        var started = false;
        while (!started && _currentPort <= 65535)
        {
            try
            {
                var ip = listenAll ? IPAddress.Any : IPAddress.Loopback;
                _tcpListener = new TcpListener(ip, _currentPort);
                _tcpListener.Start();
                started = true;
                Log.Information("-> 访问: tcp://127.0.0.1:{Port} 使用OpenNEL", _currentPort);
            }
            catch (SocketException ex)
            {
                Log.Warning("端口 {Port} 已被占用，尝试下一个端口。错误: {Error}", _currentPort, ex.Message);
                try { _tcpListener?.Stop(); } catch { }
                _currentPort++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "监听启动失败 {Port}", _currentPort);
                try { _tcpListener?.Stop(); } catch { }
                return;
            }
        }
        if (!started)
        {
            Log.Error("端口无效，无法启动");
            return;
        }
        _running = true;
        _ = AcceptLoopAsync(_cts.Token);
    }

    int GetPort()
    {
        var env = Environment.GetEnvironmentVariable("NEL_PORT");
        if (int.TryParse(env, out var p) && p > 0) return p;
        return 8080;
    }

    async Task HandleTcpClientAsync(TcpClient client)
    {
        var id = Guid.NewGuid();
        _clients.TryAdd(id, client);
        using var tcp = client;
        var stream = tcp.GetStream();
        var buffer = new byte[4096];
        await SendText(stream, "connected");
        var sb = new StringBuilder();
        while (_running)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                break;
            }
            if (read <= 0) break;
            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
            var content = sb.ToString();
            int idx;
            while ((idx = content.IndexOf('\n')) >= 0)
            {
                var line = content.Substring(0, idx).TrimEnd('\r');
                content = content.Substring(idx + 1);
                if (line.Length == 0) continue;
                if (AppState.Debug)
                {
                    Log.Information("TCP Recv: {Text}", line);
                }
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        var handler = MessageFactory.Get(type);
                        if (handler != null)
                        {
                            object? payload = null;
                            try
                            {
                                payload = await handler.ProcessAsync(root);
                            }
                            catch (Exception ex)
                            {
                                var et = type + "_error";
                                payload = new { type = et, message = ex.Message };
                            }
                            if (payload != null)
                            {
                                await Send(stream, payload);
                            }
                            continue;
                        }
                    }
                    if (AppState.Debug)
                    {
                        Log.Information("TCP Echo: {Text}", line);
                    }
                    await SendText(stream, line);
                }
                catch
                {
                    if (AppState.Debug)
                    {
                        Log.Information("TCP Echo on error: {Text}", line);
                    }
                    await SendText(stream, line);
                }
            }
            sb.Clear();
            sb.Append(content);
        }
        _clients.TryRemove(id, out _);
    }

    async Task Send(NetworkStream stream, object payload)
    {
        var seq = payload as System.Collections.IEnumerable;
        if (seq != null && !(payload is string))
        {
            foreach (var item in seq)
            {
                if (item == null) continue;
                var msg = JsonSerializer.Serialize(item);
                var ok = await SendText(stream, msg);
                if (!ok) return;
            }
            return;
        }
        var text = JsonSerializer.Serialize(payload);
        await SendText(stream, text);
    }

    async Task<bool> SendText(NetworkStream stream, string text)
    {
        if (!stream.CanWrite) return false;
        var data = Encoding.UTF8.GetBytes(text + "\n");
        try
        {
            await stream.WriteAsync(data, 0, data.Length);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning("TCP发送失败: {Message}", ex.Message);
            return false;
        }
    }

    async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (_running && !token.IsCancellationRequested)
            {
                if (_tcpListener == null)
                {
                    Log.Warning("WS服务未运行");
                    break;
                }
                var acceptTask = _tcpListener.AcceptTcpClientAsync();
                var completed = await Task.WhenAny(acceptTask, Task.Delay(-1, token));
                if (token.IsCancellationRequested) break;
                if (completed != acceptTask) continue;
                var client = await acceptTask;
                _ = Task.Run(async () =>
                {
                    try { await HandleTcpClientAsync(client); }
                    catch (Exception ex) { Log.Error(ex, "请求处理异常"); }
                }, token);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "请求处理失败");
        }
    }

    public async Task StopAsync()
    {
        if (!_running) return;
        _running = false;
        if (_cts != null) await _cts.CancelAsync();
        foreach (var kv in _clients.ToArray())
        {
            try { kv.Value.Close(); } catch { }
            _clients.TryRemove(kv.Key, out _);
        }
        try { _tcpListener?.Stop(); } catch { }
        Log.Information("TCP服务已停止");
    }
}
