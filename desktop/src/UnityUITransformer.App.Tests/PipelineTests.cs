using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Figma2Unity.Pipeline;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class PipelineTests
    {
        [Fact]
        public async Task FetchLiveDocumentAsync_SuccessfulResponse_ReturnsContent()
        {
            string expectedJson = "{\"name\":\"Test Document\"}";
            var handler = new MockHttpMessageHandler(expectedJson);
            var httpClient = new HttpClient(handler);
            var pipeline = new FigmaToUnityPipeline(httpClient);

            string result = await pipeline.FetchLiveDocumentAsync("testFileKey");

            Assert.Equal(expectedJson, result);
        }

        [Fact]
        public async Task FetchBatchWithRetryAsync_SuccessfulResponse_ReturnsContent()
        {
            string expectedJson = "{\"images\":{\"1:2\":\"https://figma.com/img.png\"}}";
            var handler = new MockHttpMessageHandler(expectedJson);
            var httpClient = new HttpClient(handler);
            var resolver = new FigmaImageBatchResolver(httpClient);

            string result = await resolver.FetchBatchWithRetryAsync("https://api.figma.com/v1/images/testFileKey");

            Assert.Equal(expectedJson, result);
        }
    }
}
