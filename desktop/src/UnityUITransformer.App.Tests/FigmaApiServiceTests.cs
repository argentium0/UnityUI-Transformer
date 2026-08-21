using System;
using UnityUITransformer.App.Services;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class FigmaApiServiceTests
    {
        [Fact]
        public void ParseFigmaUrl_StandardUrl_ExtractsFileIdAndNodeId()
        {
            string url = "https://www.figma.com/file/XYZ123456/App-UI-Kit?node-id=1:2";
            var (fileId, nodeId) = FigmaApiService.ParseFigmaUrl(url);

            Assert.Equal("XYZ123456", fileId);
            Assert.Equal("1:2", nodeId);
        }

        [Fact]
        public void ParseFigmaUrl_DesignUrlWithHyphenNodeId_NormalizesNodeId()
        {
            string url = "https://www.figma.com/design/ABC789/Dashboard?node-id=10-45";
            var (fileId, nodeId) = FigmaApiService.ParseFigmaUrl(url);

            Assert.Equal("ABC789", fileId);
            Assert.Equal("10:45", nodeId);
        }

        [Fact]
        public void ParseFigmaUrl_EncodedColonNodeId_UnescapesCorrectly()
        {
            string url = "https://www.figma.com/file/DEF456/LoginScreen?node-id=2%3A14";
            var (fileId, nodeId) = FigmaApiService.ParseFigmaUrl(url);

            Assert.Equal("DEF456", fileId);
            Assert.Equal("2:14", nodeId);
        }

        [Fact]
        public void ParseFigmaUrl_InvalidUrl_ThrowsFormatException()
        {
            string invalidUrl = "https://google.com/not-a-figma-url";
            Assert.Throws<FormatException>(() => FigmaApiService.ParseFigmaUrl(invalidUrl));
        }
    }
}
