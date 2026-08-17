using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Tests.Editor
{
    public class UIToolkitGeneratorTests
    {
        [Test]
        public void SanitizeClassName_SanitizesSpecialCharactersAndPrependsID()
        {
            string className = USSStyleGenerator.SanitizeClassName("Card Header / Title", "10:2");
            Assert.AreEqual("card-header---title-10_2", className);
        }

        [Test]
        public void USSStyleGenerator_AutoLayoutProperties_GeneratesCorrectFlexboxRules()
        {
            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new FrameNode
                    {
                        id = "1:1",
                        name = "FlexFrame",
                        type = "FRAME",
                        autoLayout = new AutoLayout
                        {
                            layoutMode = "HORIZONTAL",
                            primaryAxisAlign = "CENTER",
                            counterAxisAlign = "MIN",
                            gap = 16f,
                            padding = new Padding { top = 10, right = 20, bottom = 10, left = 20 },
                            layoutGrow = 1f
                        },
                        fills = new List<Fill>
                        {
                            new Fill { type = "SOLID", color = new ColorValue { r = 0.1f, g = 0.5f, b = 0.9f, a = 1.0f } }
                        },
                        cornerRadius = new CornerRadius { topLeft = 8, topRight = 8, bottomRight = 0, bottomLeft = 0 }
                    }
                }
            };

            string uss = USSStyleGenerator.GenerateUSS(doc, "TestPackage");

            Assert.IsNotNull(uss);
            Assert.Contains("flex-direction: row;", uss);
            Assert.Contains("justify-content: center;", uss);
            Assert.Contains("align-items: flex-start;", uss);
            Assert.Contains("gap: 16px;", uss);
            Assert.Contains("padding-top: 10px;", uss);
            Assert.Contains("padding-right: 20px;", uss);
            Assert.Contains("flex-grow: 1;", uss);
            Assert.Contains("background-color: rgba(26, 128, 230, 1.00);", uss);
            Assert.Contains("border-top-left-radius: 8px;", uss);
        }

        [Test]
        public void UXMLTreeGenerator_IRNodeHierarchy_GeneratesValidUXMLWithElements()
        {
            var frame = new FrameNode
            {
                id = "10:10",
                name = "MainContainer",
                type = "FRAME",
                children = new List<IRNode>
                {
                    new TextNode
                    {
                        id = "10:11",
                        name = "HeaderLabel",
                        type = "TEXT",
                        characters = "Welcome to Figma2Unity"
                    },
                    new ImageNode
                    {
                        id = "10:12",
                        name = "AvatarImage",
                        type = "IMAGE",
                        imageAssetRef = "exports/images/10_12@1x.png"
                    },
                    new UnsupportedNode
                    {
                        id = "10:13",
                        name = "Unsupported3DWidget",
                        type = "UNSUPPORTED",
                        figmaNodeType = "WIDGET"
                    }
                }
            };

            string uxml = UXMLTreeGenerator.GenerateUXML(frame, "Assets/Figma2Unity/Generated/TestPackage/TestPackage.uss");

            Assert.IsNotNull(uxml);
            Assert.Contains("<ui:Style src=\"Assets/Figma2Unity/Generated/TestPackage/TestPackage.uss\" />", uxml);
            Assert.Contains("<ui:VisualElement name=\"MainContainer\"", uxml);
            Assert.Contains("<ui:Label text=\"Welcome to Figma2Unity\" name=\"HeaderLabel\"", uxml);
            Assert.Contains("<ui:Image name=\"AvatarImage\"", uxml);
            Assert.Contains("<ui:Image name=\"Unsupported3DWidget\"", uxml);
        }

        [Test]
        public void UIToolkitGenerator_Generate_WritesFilesToDestinationFolder()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityGenTest_" + System.Guid.NewGuid().ToString("N"));

            var doc = new IRDocument
            {
                rootNodes = new List<IRNode>
                {
                    new FrameNode
                    {
                        id = "100:1",
                        name = "HomeScreen",
                        type = "FRAME"
                    }
                }
            };

            var result = UIToolkitGenerator.Generate(doc, tempDir, "TestPkg");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(File.Exists(result.USSPath));
            Assert.AreEqual(1, result.UXMLPaths.Count);
            Assert.IsTrue(File.Exists(result.UXMLPaths[0]));

            // Cleanup temp directory
            Directory.Delete(tempDir, true);
        }
    }
}
