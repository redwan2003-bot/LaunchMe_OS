using System.Net;
using System.Text;
using System.Text.Json;
using LaunchMe.Contracts.Agent;

namespace LaunchMe.Core.Agent;

/// <summary>
/// Local HTTP loopback hook listener for receiving agent permission prompts from Claude Code, AGY, and CLI tools.
/// </summary>
public class AgentHookServer : IDisposable
{
    private readonly AgentApprovalService _approvalService;
    private readonly int _port;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private bool _disposed;

    public bool IsRunning => _listener != null && _listener.IsListening;
    public int Port => _port;

    public AgentHookServer(AgentApprovalService approvalService, int port = 49152)
    {
        _approvalService = approvalService;
        _port = port;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/hook/");

        try
        {
            _listener.Start();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (HttpListenerException)
        {
            // Fallback: If port 49152 is occupied, try alternative loopback port 49153
            _listener.Close();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_port + 1}/hook/");
            _listener.Start();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
    }

    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        if (_listener != null)
        {
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
                // Ignore listener shutdown errors
            }
            _listener = null;
        }

        _listenTask = null;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, ct), ct);
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Continue listening
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var req = context.Request;
        var resp = context.Response;

        if (req.HttpMethod != "POST")
        {
            resp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            resp.Close();
            return;
        }

        try
        {
            using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
            string body = await reader.ReadToEndAsync(ct);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<AgentHookPayload>(body, options);

            if (payload == null || string.IsNullOrWhiteSpace(payload.RequestId))
            {
                resp.StatusCode = (int)HttpStatusCode.BadRequest;
                resp.Close();
                return;
            }

            var request = new AgentApprovalRequest(
                payload.RequestId,
                payload.AgentName ?? "AI Agent",
                payload.ToolName ?? "Tool",
                payload.CommandOrAction ?? string.Empty,
                payload.FileDiffUnified,
                Enum.TryParse<ApprovalRiskLevel>(payload.Risk, true, out var risk) ? risk : ApprovalRiskLevel.Moderate,
                payload.MultipleChoiceOptions,
                DateTime.UtcNow);

            var decision = await _approvalService.RequestApprovalAsync(request, ct);

            string responseJson = JsonSerializer.Serialize(new
            {
                requestId = decision.RequestId,
                action = decision.Action.ToString(),
                userResponseText = decision.UserResponseText
            });

            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            resp.ContentType = "application/json";
            resp.ContentLength64 = buffer.Length;
            await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length, ct);
            resp.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            resp.StatusCode = (int)HttpStatusCode.InternalServerError;
            byte[] err = Encoding.UTF8.GetBytes(ex.Message);
            resp.OutputStream.Write(err, 0, err.Length);
        }
        finally
        {
            resp.Close();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private record AgentHookPayload(
        string? RequestId,
        string? AgentName,
        string? ToolName,
        string? CommandOrAction,
        string? FileDiffUnified,
        string? Risk,
        List<string>? MultipleChoiceOptions
    );
}
