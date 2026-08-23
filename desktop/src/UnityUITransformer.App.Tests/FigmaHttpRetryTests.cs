using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Pipeline;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class FigmaHttpRetryTests
    {
        [Fact]
        public async Task SendWithBackoffAsync_SuccessfulResponseOnFirstAttempt_ReturnsResponseWithoutRetry()
        {
            int callCount = 0;
            Task<HttpResponseMessage> SendRequest()
            {
                callCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            using var response = await FigmaHttpRetry.SendWithBackoffAsync(
                SendRequest,
                CancellationToken.None,
                maxAttempts: 3,
                initialBackoff: TimeSpan.FromMilliseconds(10));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task SendWithBackoffAsync_429Response_RetriesUntilMaxAttempts()
        {
            int callCount = 0;
            Task<HttpResponseMessage> SendRequest()
            {
                callCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
            }

            using var response = await FigmaHttpRetry.SendWithBackoffAsync(
                SendRequest,
                CancellationToken.None,
                maxAttempts: 3,
                initialBackoff: TimeSpan.FromMilliseconds(10));

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task SendWithBackoffAsync_429ThenSuccess_ReturnsSuccessAfterRetry()
        {
            int callCount = 0;
            Task<HttpResponseMessage> SendRequest()
            {
                callCount++;
                if (callCount < 2)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            using var response = await FigmaHttpRetry.SendWithBackoffAsync(
                SendRequest,
                CancellationToken.None,
                maxAttempts: 3,
                initialBackoff: TimeSpan.FromMilliseconds(10));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, callCount);
        }
    }
}
