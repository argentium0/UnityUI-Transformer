#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Figma2Unity.Pipeline
{
    internal static class FigmaHttpRetry
    {
        public static async Task<HttpResponseMessage> SendWithBackoffAsync(
            Func<Task<HttpResponseMessage>> sendRequest,
            CancellationToken ct,
            int maxAttempts = 5,
            TimeSpan? initialBackoff = null)
        {
            var attempt = 0;
            var backoff = initialBackoff ?? TimeSpan.FromSeconds(2);

            while (true)
            {
                attempt++;
                var response = await sendRequest();

                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= maxAttempts)
                    return response;

                var wait = response.Headers.RetryAfter?.Delta ?? backoff;
                response.Dispose();

                string logMsg = $"[Rate Limit] 429 Too Many Requests received. Waiting {wait.TotalSeconds:0.##} seconds before attempt {attempt + 1}...";
                System.Diagnostics.Debug.WriteLine(logMsg);
                ShimLogSink.RaiseLog(ShimLogLevel.Warning, logMsg);

                await Task.Delay(wait, ct);
                backoff += backoff;
            }
        }
    }
}
