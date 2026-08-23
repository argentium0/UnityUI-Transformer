using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityUITransformer.App.Models;
using UnityUITransformer.App.Services;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class TransformationEngineTests
    {
        [Fact]
        public void UxmlGenerator_GeneratesValidXmlWithUnityNamespaces()
        {
            var generator = new UxmlGenerator();
            var root = new FigmaNode
            {
                Id = "1:1",
                Name = "Main Screen",
                Type = "FRAME",
                Children = new List<FigmaNode>
                {
                    new FigmaNode
                    {
                        Id = "1:2",
                        Name = "Header Title",
                        Type = "TEXT",
                        Characters = "Welcome User"
                    }
                }
            };

            string uxml = generator.GenerateUxml(root);

            Assert.Contains("xmlns:ui=\"UnityEngine.UIElements\"", uxml);
            Assert.Contains("<ui:VisualElement name=\"MainScreen\"", uxml);
            Assert.Contains("<ui:Label name=\"HeaderTitle\" text=\"Welcome User\"", uxml);
        }

        [Fact]
        public void UxmlGenerator_SanitizesNamesAndKebabCaseClasses()
        {
            string clean = UxmlGenerator.SanitizeName("Header - Title @123");
            string kebab = UxmlGenerator.ToKebabCase(clean);

            Assert.Equal("Header-Title123", clean);
            Assert.Equal("header-title123", kebab);
        }

        [Fact]
        public void UxmlGenerator_SanitizesNodeIdForFileName_Correctly()
        {
            string sanitized = UxmlGenerator.SanitizeNodeIdForFileName("1:2;3:4");
            Assert.Equal("1_2_3_4", sanitized);
        }

        [Fact]
        public async Task ExportService_ExportsUxmlFileToDirectory()
        {
            var exportService = new ExportService();
            string tempDir = Path.Combine(Path.GetTempPath(), "UnityUITransformerTests", System.Guid.NewGuid().ToString());
            string uxmlContent = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"></ui:UXML>";

            string filePath = await exportService.ExportUxmlAsync(uxmlContent, tempDir, "TestScreen");

            Assert.True(File.Exists(filePath));
            Assert.EndsWith("TestScreen.uxml", filePath);
            string readContent = await File.ReadAllTextAsync(filePath);
            Assert.Equal(uxmlContent, readContent);

            // Clean up
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void FigmaApiService_CollectsImageNodes_Correctly()
        {
            var root = new FigmaNode
            {
                Id = "1:1",
                Name = "Main Screen",
                Type = "FRAME",
                Children = new List<FigmaNode>
                {
                    new FigmaNode
                    {
                        Id = "1:2",
                        Name = "Hero Image",
                        Type = "RECTANGLE",
                        Fills = new List<FigmaPaint>
                        {
                            new FigmaPaint
                            {
                                Type = "IMAGE",
                                ImageRef = "img_123"
                            }
                        }
                    },
                    new FigmaNode
                    {
                        Id = "1:3",
                        Name = "Vector Icon",
                        Type = "RECTANGLE",
                        Fills = new List<FigmaPaint>
                        {
                            new FigmaPaint
                            {
                                Type = "IMAGE",
                                ImageRef = "icon_456"
                            }
                        }
                    }
                }
            };

            var imageNodes = FigmaApiService.CollectImageNodes(root);

            Assert.Equal(2, imageNodes.Count);
            Assert.Contains("1:2", imageNodes);
            Assert.Contains("1:3", imageNodes);
        }
    }
}
