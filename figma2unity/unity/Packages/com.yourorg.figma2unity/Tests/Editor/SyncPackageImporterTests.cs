using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Figma2Unity.Editor.Importer;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Tests.Editor
{
    public class SyncPackageImporterTests
    {
        [Test]
        public void ValidateSchemaVersion_MajorVersionMatch_ReturnsTrue()
        {
            bool valid = SyncPackageImporter.ValidateSchemaVersion("1.0.0", out string error);
            Assert.IsTrue(valid);
            Assert.IsNull(error);

            valid = SyncPackageImporter.ValidateSchemaVersion("1.5.2", out error);
            Assert.IsTrue(valid);
            Assert.IsNull(error);
        }

        [Test]
        public void ValidateSchemaVersion_MajorVersionMismatch_ReturnsFalse()
        {
            bool valid = SyncPackageImporter.ValidateSchemaVersion("2.0.0", out string error);
            Assert.IsFalse(valid);
            Assert.IsNotNull(error);
            Assert.Contains("major version 1", error);
        }

        [Test]
        public void ParseIRDocument_PolymorphicNodes_DeserializesCorrectSubclasses()
        {
            string json = @"{
                ""schemaVersion"": ""1.0.0"",
                ""metadata"": {
                    ""exportedAt"": ""2026-08-17T12:00:00.000Z"",
                    ""figmaFileName"": ""TestCard""
                },
                ""tokens"": {
                    ""colors"": [
                        { ""id"": ""c1"", ""name"": ""BrandBlue"", ""value"": { ""r"": 0.05, ""g"": 0.6, ""b"": 1.0, ""a"": 1.0 } }
                    ]
                },
                ""rootNodes"": [
                    {
                        ""id"": ""1:1"",
                        ""name"": ""CardFrame"",
                        ""type"": ""FRAME"",
                        ""bounds"": { ""x"": 0, ""y"": 0, ""width"": 300, ""height"": 200 },
                        ""children"": [
                            {
                                ""id"": ""1:2"",
                                ""name"": ""CardTitle"",
                                ""type"": ""TEXT"",
                                ""characters"": ""Hello World"",
                                ""fontSize"": 16.0,
                                ""bounds"": { ""x"": 10, ""y"": 10, ""width"": 280, ""height"": 24 }
                            },
                            {
                                ""id"": ""1:3"",
                                ""name"": ""HeroBanner"",
                                ""type"": ""IMAGE"",
                                ""imageAssetRef"": ""exports/images/1_3@1x.png"",
                                ""bounds"": { ""x"": 0, ""y"": 40, ""width"": 300, ""height"": 160 }
                            },
                            {
                                ""id"": ""1:4"",
                                ""name"": ""ExoticWidget"",
                                ""type"": ""UNSUPPORTED"",
                                ""figmaNodeType"": ""WIDGET"",
                                ""bounds"": { ""x"": 0, ""y"": 0, ""width"": 50, ""height"": 50 }
                            }
                        ]
                    }
                ]
            }";

            IRDocument doc = SyncPackageImporter.ParseIRDocument(json);

            Assert.IsNotNull(doc);
            Assert.AreEqual("1.0.0", doc.schemaVersion);
            Assert.AreEqual("TestCard", doc.metadata.figmaFileName);
            Assert.AreEqual(1, doc.tokens.colors.Count);
            Assert.AreEqual("BrandBlue", doc.tokens.colors[0].name);

            Assert.AreEqual(1, doc.rootNodes.Count);
            Assert.IsInstanceOf<FrameNode>(doc.rootNodes[0]);

            FrameNode frame = doc.rootNodes[0] as FrameNode;
            Assert.AreEqual(3, frame.children.Count);

            Assert.IsInstanceOf<TextNode>(frame.children[0]);
            TextNode textNode = frame.children[0] as TextNode;
            Assert.AreEqual("Hello World", textNode.characters);
            Assert.AreEqual(16.0f, textNode.fontSize);

            Assert.IsInstanceOf<ImageNode>(frame.children[1]);
            ImageNode imageNode = frame.children[1] as ImageNode;
            Assert.AreEqual("exports/images/1_3@1x.png", imageNode.imageAssetRef);

            Assert.IsInstanceOf<UnsupportedNode>(frame.children[2]);
            UnsupportedNode unsupportedNode = frame.children[2] as UnsupportedNode;
            Assert.AreEqual("WIDGET", unsupportedNode.figmaNodeType);
        }

        [Test]
        public void ImportSyncPackage_ValidZipArchive_UnpacksAndParsesSuccessfully()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityTestZip_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, "FixturePackage.f2u.zip");
            string irJson = @"{
                ""schemaVersion"": ""1.0.0"",
                ""metadata"": { ""exportedAt"": ""2026-08-17T12:00:00.000Z"", ""figmaFileName"": ""FixtureScreen"" },
                ""tokens"": { ""colors"": [] },
                ""rootNodes"": [
                    { ""id"": ""2:1"", ""name"": ""RootView"", ""type"": ""FRAME"", ""bounds"": { ""x"": 0, ""y"": 0, ""width"": 1920, ""height"": 1080 } }
                ]
            }";

            // Construct zip file fixture in memory/disk
            using (FileStream zipStream = new FileStream(zipPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                ZipArchiveEntry jsonEntry = archive.CreateEntry("ir-document.json");
                using (StreamWriter writer = new StreamWriter(jsonEntry.Open()))
                {
                    writer.Write(irJson);
                }

                ZipArchiveEntry imageEntry = archive.CreateEntry("exports/images/2_1@1x.png");
                using (StreamWriter writer = new StreamWriter(imageEntry.Open()))
                {
                    writer.Write("MOCK_PNG_DATA");
                }
            }

            string customDestFolder = Path.Combine(tempDir, "Assets_Figma2UnityImports_FixtureScreen");

            SyncPackageImporter.ImportResult result = SyncPackageImporter.ImportSyncPackage(zipPath, customDestFolder);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Document);
            Assert.AreEqual("FixtureScreen", result.Document.metadata.figmaFileName);
            Assert.AreEqual(1, result.Document.rootNodes.Count);
            Assert.AreEqual("RootView", result.Document.rootNodes[0].name);

            // Verify assets copied to destination folder
            string copiedImage = Path.Combine(customDestFolder, "exports", "images", "2_1@1x.png");
            Assert.IsTrue(File.Exists(copiedImage));

            // Clean up temporary files
            Directory.Delete(tempDir, true);
        }
    }
}
