#nullable enable
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Pipeline
{
    public class FigmaToUnityPipeline
    {
        private readonly HttpClient _figmaHttpClient;

        public FigmaToUnityPipeline(HttpClient? httpClient = null)
        {
            _figmaHttpClient = httpClient ?? new HttpClient();
        }

        public async Task<string> FetchLiveDocumentAsync(string fileKey, CancellationToken ct = default)
        {
            using var response = await FigmaHttpRetry.SendWithBackoffAsync(
                () => _figmaHttpClient.GetAsync($"https://api.figma.com/v1/files/{fileKey}", ct),
                ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
    }
}
