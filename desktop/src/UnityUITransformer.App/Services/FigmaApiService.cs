using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityUITransformer.App.Models;

namespace UnityUITransformer.App.Services
{
    public class FigmaApiService
    {
        private readonly HttpClient _httpClient;

        public FigmaApiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.figma.com/v1/");
        }

        public static (string FileId, string NodeId) ParseFigmaUrl(string figmaUrl)
        {
            if (string.IsNullOrWhiteSpace(figmaUrl))
                throw new FormatException("Figma URL cannot be null or empty.");

            var uri = new Uri(figmaUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string fileId = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                if ((segments[i] == "file" || segments[i] == "design") && i + 1 < segments.Length)
                {
                    fileId = segments[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(fileId))
                throw new FormatException("Invalid Figma URL format: Could not locate file or design key.");

            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string? nodeId = queryParams["node-id"];

            if (string.IsNullOrEmpty(nodeId))
                throw new FormatException("Invalid Figma URL: Missing required 'node-id' query parameter.");

            nodeId = System.Web.HttpUtility.UrlDecode(nodeId);
            nodeId = nodeId.Replace('-', ':');

            return (fileId, nodeId);
        }

        public async Task<string> GetFigmaNodeAsync(string figmaUrl, string? providerToken = null)
        {
            var (fileId, nodeId) = ParseFigmaUrl(figmaUrl);
            string endpoint = string.IsNullOrEmpty(nodeId)
                ? $"files/{fileId}"
                : $"files/{fileId}/nodes?ids={Uri.EscapeDataString(nodeId)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrWhiteSpace(providerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);
                request.Headers.TryAddWithoutValidation("X-Figma-Token", providerToken);
            }

            try
            {
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FigmaApiService] Exception in GetFigmaNodeAsync: {ex.Message}");
                throw new InvalidOperationException($"Figma API request failed for endpoint '{endpoint}': {ex.Message}", ex);
            }
        }

        public async Task<(string Handle, string ImgUrl, string Email)> GetFigmaUserProfileAsync(string providerToken)
        {
            if (string.IsNullOrWhiteSpace(providerToken))
            {
                return (string.Empty, string.Empty, string.Empty);
            }

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(200));
                using var request = new HttpRequestMessage(HttpMethod.Get, "me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);

                using var response = await _httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    string handle = root.TryGetProperty("handle", out var h) ? h.GetString() ?? "" : "";
                    string imgUrl = root.TryGetProperty("img_url", out var i) ? i.GetString() ?? "" : "";
                    string email = root.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
                    return (handle, imgUrl, email);
                }
            }
            catch
            {
                // Fallback for offline or simulated session
            }

            return (string.Empty, string.Empty, string.Empty);
        }

        private FigmaNode? ExtractFigmaNode(string json, string? nodeId, JsonSerializerOptions options)
        {
            using var document = JsonDocument.Parse(json);
            var rootElement = document.RootElement;

            if (!string.IsNullOrEmpty(nodeId) && rootElement.TryGetProperty("nodes", out var nodesElement))
            {
                if (nodesElement.TryGetProperty(nodeId, out var nodeContainerElement))
                {
                    if (nodeContainerElement.TryGetProperty("document", out var docElement))
                    {
                        return JsonSerializer.Deserialize<FigmaNode>(docElement.GetRawText(), options);
                    }
                }
            }

            if (rootElement.TryGetProperty("document", out var rootDocElement))
            {
                return JsonSerializer.Deserialize<FigmaNode>(rootDocElement.GetRawText(), options);
            }

            try
            {
                return JsonSerializer.Deserialize<FigmaNode>(json, options);
            }
            catch
            {
                return null;
            }
        }

        public async Task<FigmaNode> GetFigmaNodeModelAsync(string figmaUrl, string? providerToken = null)
        {
            var (fileId, nodeId) = ParseFigmaUrl(figmaUrl);

            string json = await GetFigmaNodeAsync(figmaUrl, providerToken);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var root = ExtractFigmaNode(json, nodeId, options);
            if (root != null)
            {
                return root;
            }

            throw new InvalidOperationException($"Could not parse valid FigmaNode root document from response for file '{fileId}' node '{nodeId}'.");
        }

        // TODO: Implement full image asset download pipeline using Figma's REST API /v1/images/:key endpoint.
        // This method queries Figma for rendered image URLs for nodes containing imageRef or vector icon assets.
        public Task<System.Collections.Generic.Dictionary<string, string>> ExportFigmaImagesAsync(string fileKey, System.Collections.Generic.IEnumerable<string> nodeIds, string? providerToken = null, string format = "png", float scale = 2.0f)
        {
            var imageMap = new System.Collections.Generic.Dictionary<string, string>();
            return Task.FromResult(imageMap);
        }

        public static bool IsVisualAssetNode(FigmaNode node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.Id)) return false;
            bool isText = string.Equals(node.Type, "TEXT", StringComparison.OrdinalIgnoreCase);
            if (isText) return false;

            bool hasFills = node.Fills != null && node.Fills.Exists(f => f.Visible != false);
            bool isVisualType = string.Equals(node.Type, "IMAGE", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(node.Type, "VECTOR", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(node.Type, "RECTANGLE", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(node.Type, "ELLIPSE", StringComparison.OrdinalIgnoreCase);

            return hasFills || isVisualType;
        }

        public static System.Collections.Generic.List<string> CollectImageNodes(FigmaNode node)
        {
            var list = new System.Collections.Generic.List<string>();
            TraverseForImageNodes(node, list);
            return list;
        }

        private static void TraverseForImageNodes(FigmaNode node, System.Collections.Generic.List<string> list)
        {
            if (node == null) return;

            if (IsVisualAssetNode(node) && !string.IsNullOrWhiteSpace(node.Id))
            {
                list.Add(node.Id);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    TraverseForImageNodes(child, list);
                }
            }
        }
    }
}
