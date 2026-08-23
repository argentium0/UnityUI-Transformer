using System.Collections.Generic;
using Figma2Unity.Pipeline;
using UnityUITransformer.App.Models;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class UxmlDocumentBuilderTests
    {
        [Fact]
        public void Build_RootNodeWithFlexboxAndText_GeneratesResponsiveUxmlAndUss()
        {
            var rootNode = new FigmaNode
            {
                Id = "node-1",
                Name = "MainCard",
                Type = "FRAME",
                LayoutMode = "VERTICAL",
                PaddingLeft = 20f,
                PaddingRight = 20f,
                PaddingTop = 15f,
                PaddingBottom = 15f,
                PrimaryAxisAlignItems = "CENTER",
                CounterAxisAlignItems = "MAX",
                Children = new List<FigmaNode>
                {
                    new FigmaNode
                    {
                        Id = "node-2",
                        Name = "HeaderLabel",
                        Type = "TEXT",
                        Characters = "Welcome Title",
                        FontSize = 24f,
                        FontWeight = "700",
                        FontFamily = "Roboto"
                    }
                }
            };

            var builder = new UxmlDocumentBuilder();
            var (uxml, uss) = builder.Build(rootNode);

            Assert.Contains("<ui:UXML", uxml);
            Assert.Contains("<ui:VisualElement name=\"MainCard\" class=\"main-card\"", uxml);
            Assert.Contains("<ui:Label name=\"HeaderLabel\" class=\"header-label\" text=\"Welcome Title\"", uxml);

            Assert.Contains(".main-card {", uss);
            Assert.Contains("display: flex;", uss);
            Assert.Contains("flex-direction: column;", uss);
            Assert.Contains("padding-left: 20px;", uss);
            Assert.Contains("justify-content: center;", uss);

            Assert.Contains(".header-label {", uss);
            Assert.Contains("font-size: 24px;", uss);
            Assert.Contains("-unity-font-style: bold;", uss);
            Assert.Contains("-unity-font-definition: url('project://database/Assets/Fonts/Roboto.asset');", uss);
        }
    }
}
