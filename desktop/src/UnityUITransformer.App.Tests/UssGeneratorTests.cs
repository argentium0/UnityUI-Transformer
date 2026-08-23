using System.Collections.Generic;
using UnityUITransformer.App.Models;
using UnityUITransformer.App.Services;
using Figma2Unity.Pipeline;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class UssGeneratorTests
    {
        [Fact]
        public void GenerateUss_RootNodeWithFillsDimensionsAndLayout_EmitsValidCssRules()
        {
            var generator = new UssGenerator();

            var rootNode = new FigmaNode
            {
                Name = "Main Container",
                Type = "FRAME",
                AbsoluteBoundingBox = new FigmaBoundingBox { Width = 800f, Height = 600f },
                Fills = new List<FigmaPaint>
                {
                    new FigmaPaint
                    {
                        Visible = true,
                        Type = "SOLID",
                        Color = new FigmaColor { R = 0.1f, G = 0.2f, B = 0.3f, A = 1.0f }
                    }
                },
                LayoutMode = "VERTICAL",
                ItemSpacing = 16f,
                PaddingLeft = 20f,
                PaddingTop = 20f,
                Children = new List<FigmaNode>
                {
                    new FigmaNode
                    {
                        Name = "Header Label",
                        Type = "TEXT",
                        Characters = "Welcome Title",
                        Style = new FigmaTypeStyle
                        {
                            FontFamily = "Roboto",
                            FontSize = 24f,
                            FontWeight = 700f,
                            TextAlignHorizontal = "CENTER"
                        },
                        Fills = new List<FigmaPaint>
                        {
                            new FigmaPaint
                            {
                                Visible = true,
                                Type = "SOLID",
                                Color = new FigmaColor { R = 1.0f, G = 1.0f, B = 1.0f, A = 1.0f }
                            }
                        }
                    }
                }
            };

            string ussOutput = generator.GenerateUss(rootNode);

            Assert.Contains(".main-container {", ussOutput);
            Assert.Contains("width: 800px;", ussOutput);
            Assert.Contains("height: 600px;", ussOutput);
            Assert.Contains("background-color: rgba(26, 51, 76, 1);", ussOutput);
            Assert.Contains("flex-direction: column;", ussOutput);
            Assert.DoesNotContain("gap:", ussOutput);
            Assert.Contains("padding-left: 20px;", ussOutput);

            Assert.Contains(".header-label {", ussOutput);
            Assert.Contains("-unity-font-definition: url('project://database/Assets/Fonts/Roboto.asset');", ussOutput);
            Assert.Contains("color: rgba(255, 255, 255, 1);", ussOutput);
            Assert.Contains("font-size: 24px;", ussOutput);
            Assert.Contains("-unity-font-style: bold;", ussOutput);
            Assert.Contains("-unity-text-align: middle-center;", ussOutput);
        }

        [Fact]
        public void GenerateUss_TextNodeWithoutFontName_OmitsUnityFontDefinition()
        {
            var generator = new UssGenerator();
            var textNode = new FigmaNode
            {
                Name = "Anonymous Label",
                Type = "TEXT",
                Style = new FigmaTypeStyle
                {
                    FontSize = 14f
                }
            };

            string ussOutput = generator.GenerateUss(textNode);

            Assert.DoesNotContain("-unity-font-definition", ussOutput);
            Assert.Contains("font-size: 14px;", ussOutput);
        }

        [Fact]
        public void UssStyleBuilder_AppendLayoutRule_StripsAbsolutePositioning()
        {
            var node = new FigmaNode
            {
                Name = "FlexboxChild",
                AbsoluteBoundingBox = new FigmaBoundingBox { X = 100f, Y = 200f, Width = 300f, Height = 150f }
            };

            var rules = new List<string>();
            UssStyleBuilder.AppendLayoutRule(node, rules);

            Assert.Contains("width: 300px;", rules);
            Assert.Contains("height: 150px;", rules);
            Assert.DoesNotContain(rules, r => r.Contains("position: absolute"));
            Assert.DoesNotContain(rules, r => r.StartsWith("left:"));
            Assert.DoesNotContain(rules, r => r.StartsWith("top:"));
        }

        [Fact]
        public void UssStyleBuilder_AppendTypographyRule_GeneratesFontSizeAndFontDefinition()
        {
            var textNode = new FigmaNode
            {
                Name = "TitleText",
                Type = "TEXT",
                FontFamily = "Inter Display",
                FontSize = 18f
            };

            var rules = new List<string>();
            UssStyleBuilder.AppendTypographyRule(textNode, rules);

            Assert.Contains("font-size: 18px;", rules);
            Assert.Contains("-unity-font-definition: url('project://database/Assets/Fonts/InterDisplay.asset');", rules);
        }
    }
}
