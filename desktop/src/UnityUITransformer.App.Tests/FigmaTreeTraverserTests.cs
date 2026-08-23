using System.Text.Json;
using Figma2Unity.Pipeline;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class FigmaTreeTraverserTests
    {
        [Fact]
        public void TraverseNode_ParsesFlexboxAndTypographyProperties()
        {
            string json = @"{
                ""id"": ""1:10"",
                ""name"": ""HeaderCard"",
                ""type"": ""FRAME"",
                ""layoutMode"": ""VERTICAL"",
                ""itemSpacing"": 16.0,
                ""paddingLeft"": 20.0,
                ""paddingRight"": 20.0,
                ""paddingTop"": 24.0,
                ""paddingBottom"": 24.0,
                ""primaryAxisAlignItems"": ""CENTER"",
                ""counterAxisAlignItems"": ""MAX"",
                ""layoutGrow"": 1.0,
                ""children"": [
                    {
                        ""id"": ""1:11"",
                        ""name"": ""TitleText"",
                        ""type"": ""TEXT"",
                        ""characters"": ""Welcome to Unity UI"",
                        ""style"": {
                            ""fontFamily"": ""Roboto"",
                            ""fontSize"": 24.0,
                            ""fontWeight"": 700.0
                        }
                    }
                ]
            }";

            using var doc = JsonDocument.Parse(json);
            var rootNode = FigmaTreeTraverser.TraverseNode(doc.RootElement);

            Assert.Equal("1:10", rootNode.Id);
            Assert.Equal("HeaderCard", rootNode.Name);
            Assert.Equal("FRAME", rootNode.Type);
            Assert.Equal("VERTICAL", rootNode.LayoutMode);
            Assert.Equal(16f, rootNode.ItemSpacing);
            Assert.Equal(20f, rootNode.PaddingLeft);
            Assert.Equal(20f, rootNode.PaddingRight);
            Assert.Equal(24f, rootNode.PaddingTop);
            Assert.Equal(24f, rootNode.PaddingBottom);
            Assert.Equal("CENTER", rootNode.PrimaryAxisAlignItems);
            Assert.Equal("MAX", rootNode.CounterAxisAlignItems);
            Assert.Equal(1f, rootNode.LayoutGrow);

            Assert.NotNull(rootNode.Children);
            Assert.Single(rootNode.Children);

            var textNode = rootNode.Children[0];
            Assert.Equal("1:11", textNode.Id);
            Assert.Equal("TitleText", textNode.Name);
            Assert.Equal("TEXT", textNode.Type);
            Assert.Equal("Welcome to Unity UI", textNode.Characters);
            Assert.Equal("Roboto", textNode.FontFamily);
            Assert.Equal(24f, textNode.FontSize);
            Assert.Equal("700", textNode.FontWeight);
        }
    }
}
