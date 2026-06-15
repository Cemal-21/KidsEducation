using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using KidsEducation.Models;

namespace KidsEducation.Services;

/// <summary>
/// Simple LAN multiplayer: host runs an HttpListener, client polls via HttpClient.
/// No external dependencies — pure .NET networking.
/// </summary>
public class MultiplayerService : IDisposable
{
    private HttpListener? _listener;
    private HttpClient? _client;
    private CancellationTokenSource? _cts;

    // Shared state (host side)
    private MultiplayerQuestion? _currentQuestion;
    private MultiplayerAnswer? _lastAnswer;
    private readonly object _lock = new();

    public bool IsHost { get; private set; }
    public string? HostAddress { get; private set; }
    public bool IsConnected { get; private set; }

    public event Action<MultiplayerAnswer>? AnswerReceived;
    public event Action<MultiplayerQuestion>? QuestionReceived;
    public event Action? ClientConnected;

    // ── Host ─────────────────────────────────────────────────────────────────

    public string StartHost()
    {
        IsHost = true;
        _cts = new CancellationTokenSource();
        var ip = GetLocalIp();
        HostAddress = $"http://{ip}:7878/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(HostAddress);
        _listener.Start();

        _ = Task.Run(() => ListenLoopAsync(_cts.Token));
        return ip;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync().WaitAsync(ct);
                _ = Task.Run(() => HandleRequestAsync(ctx), ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore socket errors during shutdown */ }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "";
        ctx.Response.ContentType = "application/json";
        ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");

        string response;

        if (path == "/ping")
        {
            IsConnected = true;
            MainThread.BeginInvokeOnMainThread(() => ClientConnected?.Invoke());
            response = JsonSerializer.Serialize(new { ok = true });
        }
        else if (path == "/question")
        {
            lock (_lock)
                response = _currentQuestion is not null
                    ? JsonSerializer.Serialize(_currentQuestion)
                    : JsonSerializer.Serialize(new { waiting = true });
        }
        else if (path == "/answer" && ctx.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var answer = JsonSerializer.Deserialize<MultiplayerAnswer>(body);
            if (answer is not null)
            {
                lock (_lock) _lastAnswer = answer;
                MainThread.BeginInvokeOnMainThread(() => AnswerReceived?.Invoke(answer));
            }
            response = JsonSerializer.Serialize(new { ok = true });
        }
        else
        {
            ctx.Response.StatusCode = 404;
            response = "{}";
        }

        var bytes = Encoding.UTF8.GetBytes(response);
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }

    public void SendQuestion(MultiplayerQuestion question)
    {
        lock (_lock)
        {
            _currentQuestion = question;
            _lastAnswer = null;
        }
    }

    // ── Client ────────────────────────────────────────────────────────────────

    public async Task<bool> ConnectToHostAsync(string hostIp)
    {
        IsHost = false;
        HostAddress = $"http://{hostIp}:7778/";
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        _cts = new CancellationTokenSource();

        try
        {
            var resp = await _client.GetAsync($"http://{hostIp}:7878/ping");
            IsConnected = resp.IsSuccessStatusCode;
            if (IsConnected)
                _ = Task.Run(() => PollLoopAsync(_cts.Token));
            return IsConnected;
        }
        catch { return false; }
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        MultiplayerQuestion? lastSeen = null;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var json = await _client!.GetStringAsync(
                    $"{HostAddress?.Replace(":7878", ":7878")?.TrimEnd('/')}/../question"
                    .Replace("7778", "7878"), ct);

                var q = JsonSerializer.Deserialize<MultiplayerQuestion>(json);
                if (q?.Id is not null && q.Id != lastSeen?.Id)
                {
                    lastSeen = q;
                    MainThread.BeginInvokeOnMainThread(() => QuestionReceived?.Invoke(q));
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }

            await Task.Delay(800, ct);
        }
    }

    public async Task SendAnswerAsync(MultiplayerAnswer answer)
    {
        if (_client is null || HostAddress is null) return;
        var json = JsonSerializer.Serialize(answer);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try { await _client.PostAsync($"{HostAddress.TrimEnd('/')}/answer".Replace("7778", "7878"), content); }
        catch { }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _client?.Dispose();
    }
}

public class MultiplayerQuestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string QuestionText { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string CategoryEmoji { get; set; } = "";
    public int Round { get; set; }
    public int TotalRounds { get; set; }
}

public class MultiplayerAnswer
{
    public string QuestionId { get; set; } = "";
    public string SelectedAnswer { get; set; } = "";
    public bool IsCorrect { get; set; }
    public long AnswerTimeMs { get; set; }
}
