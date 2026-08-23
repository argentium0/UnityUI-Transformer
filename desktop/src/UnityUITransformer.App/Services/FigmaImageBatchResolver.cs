#nullable enable
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Figma2Unity.Pipeline
{
    public class FigmaImageBatchResolver
    {
        private readonly HttpClient _httpClient;

        public FigmaImageBatchResolver(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<string> FetchBatchWithRetryAsync(string url, CancellationToken ct = default, int maxAttempts = 5)
        {
            using var response = await FigmaHttpRetry.SendWithBackoffAsync(
                () => _httpClient.GetAsync(url, ct), ct, maxAttempts);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
    }
}
