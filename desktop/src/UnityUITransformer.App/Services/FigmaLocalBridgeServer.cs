using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityUITransformer.App.Models;

namespace UnityUITransformer.App.Services
{
    public class FigmaLocalBridgeServer : IDisposable
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly UxmlGenerator _uxmlGenerator;
        private readonly UssGenerator _ussGenerator;
        private readonly ExportService _exportService;
        private readonly string _prefix;

        public event Action<string, string, string>? PayloadReceivedAndProcessed;

        public bool IsRunning => _listener?.IsListening ?? false;

        public FigmaLocalBridgeServer(string prefix = "http://127.0.0.1:5142/sync/")
        {
            _prefix = prefix;
            _uxmlGenerator = new UxmlGenerator();
            _ussGenerator = new UssGenerator();
            _exportService = new ExportService();
        }

        public void Start(Func<string>? getTargetDirectory = null)
        {
            if (IsRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_prefix);
                _listener.Start();
                _cts = new CancellationTokenSource();

                Task.Run(() => ListenAsync(getTargetDirectory, _cts.Token));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FigmaLocalBridgeServer] Failed to start listener on {_prefix}: {ex.Message}");
            }
        }

        private async Task ListenAsync(Func<string>? getTargetDirectory, CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context, getTargetDirectory);
                }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FigmaLocalBridgeServer] Listener error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, Func<string>? getTargetDirectory)
        {
            var req = context.Request;
            var resp = context.Response;

            // Enable CORS headers for browser/Figma plugin origin
            resp.AddHeader("Access-Control-Allow-Origin", "*");
            resp.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS, GET");
            resp.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                resp.StatusCode = 200;
                resp.Close();
                return;
            }

            if (!req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                resp.StatusCode = 405;
                resp.Close();
                return;
            }

            try
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
                string jsonBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(jsonBody))
                {
                    SendJsonResponse(resp, 400, new { success = false, message = "Empty body" });
                    return;
                }

                FigmaNode? rootNode = null;

                // 1. Try parsing as FigmaNodeResponse
                try
                {
                    var responseObj = JsonSerializer.Deserialize<FigmaNodeResponse>(jsonBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    rootNode = responseObj?.GetRootNode();
                }
                catch { }

                // 2. Try parsing as direct FigmaNode if responseObj didn't match
                if (rootNode == null)
                {
                    try
                    {
                        rootNode = JsonSerializer.Deserialize<FigmaNode>(jsonBody, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch { }
                }

                // 3. Try parsing as document root if wrapped in IR or document object
                if (rootNode == null)
                {
                    using var doc = JsonDocument.Parse(jsonBody);
                    if (doc.RootElement.TryGetProperty("document", out var docElem))
                    {
                        rootNode = JsonSerializer.Deserialize<FigmaNode>(docElem.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                }

                if (rootNode == null)
                {
                    SendJsonResponse(resp, 400, new { success = false, message = "Could not parse valid FigmaNode root from payload" });
                    return;
                }

                string fileName = !string.IsNullOrWhiteSpace(rootNode.Name) ? UxmlGenerator.SanitizeName(rootNode.Name) : "FigmaComponent";
                string uxmlContent = _uxmlGenerator.GenerateUxml(rootNode, fileName);
                string ussContent = _ussGenerator.GenerateUss(rootNode);

                string targetDirectory = getTargetDirectory?.Invoke() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "UI", "Generated");
                }

                string uxmlPath = await _exportService.ExportUxmlAsync(uxmlContent, targetDirectory, fileName);
                string ussPath = await _exportService.ExportUssAsync(ussContent, targetDirectory, fileName);

                PayloadReceivedAndProcessed?.Invoke(fileName, uxmlPath, ussPath);

                SendJsonResponse(resp, 200, new
                {
                    success = true,
                    message = "Synced successfully to Unity UI Toolkit",
                    fileName,
                    uxmlPath,
                    ussPath
                });
            }
            catch (Exception ex)
            {
                SendJsonResponse(resp, 500, new { success = false, message = ex.Message });
            }
        }

        private void SendJsonResponse(HttpListenerResponse resp, int statusCode, object data)
        {
            try
            {
                resp.StatusCode = statusCode;
                resp.ContentType = "application/json";
                string json = JsonSerializer.Serialize(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                resp.ContentLength64 = bytes.Length;
                resp.OutputStream.Write(bytes, 0, bytes.Length);
                resp.Close();
            }
            catch { }
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            _listener = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
