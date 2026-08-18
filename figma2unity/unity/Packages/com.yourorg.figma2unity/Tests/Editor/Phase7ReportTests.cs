using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Figma2Unity.Editor.Fonts;
using Figma2Unity.Editor.Generator;
using Figma2Unity.Editor.Importer;
using Figma2Unity.Editor.Reporting;
using Figma2Unity.Editor.Schema;

namespace Figma2Unity.Tests.Editor
{
    public class Phase7ReportTests
    {
        [Test]
        public void ImportPipeline_FallbackAndReportGeneration_LogsEventsAndCreatesFiles()
        {
            string packageName = "TestFallbackPackage";
            string tempDir = Path.Combine(Path.GetTempPath(), "Figma2UnityPhase7_" + Guid.NewGuid().ToString("N"));

            try
            {
                var unsupportedNode = new UnsupportedNode
                {
                    id = "unsup-1",
                    name = "Custom3DWidget",
                    type = "UNSUPPORTED",
                    visible = true,
                    bounds = new Bounds { x = 0, y = 0, width = 100, height = 100 }
                };

                var textNode = new TextNode
                {
                    id = "text-99",
                    name = "FallbackTitleLabel",
                    type = "TEXT",
                    fontFamily = "NonExistentFontFamilyPhase7",
                    fontWeight = 700f,
                    characters = "Test String",
                    visible = true,
                    bounds = new Bounds { x = 0, y = 110, width = 200, height = 40 }
                };

                var doc = new IRDocument
                {
                    schemaVersion = "1.0.0",
                    rootNodes = new List<IRNode>
                    {
                        new FrameNode
                        {
                            id = "root-1",
                            name = "MainScreen",
                            type = "FRAME",
                            visible = true,
                            bounds = new Bounds { x = 0, y = 0, width = 400, height = 800 },
                            children = new List<IRNode>
                            {
                                unsupportedNode,
                                textNode
                            }
                        }
                    }
                };

                // 1. Begin logger session
                FigmaImportLogger.BeginSession(packageName, doc.schemaVersion);

                // 2. Generate UXML & USS (triggers UXMLTreeGenerator node processing & rasterization logs)
                var genResult = UIToolkitGenerator.Generate(doc, tempDir, packageName);
                Assert.IsTrue(genResult.Success);

                // 3. Resolve fonts for text nodes (triggers FontResolver missing font log)
                FontResolver.ResolveFontForTextNode(textNode, null);

                // 4. End logger session
                var reportData = FigmaImportLogger.EndSession();

                Assert.IsNotNull(reportData);
                Assert.AreEqual(packageName, reportData.PackageName);
                Assert.IsTrue(reportData.TotalNodesProcessed >= 3); // Root frame + 2 children

                // Assert UnsupportedNode logged in RasterizedNodes
                Assert.IsTrue(reportData.RasterizedNodes.Count >= 1);
                var rasterEntry = reportData.RasterizedNodes.Find(n => n.NodeId == "unsup-1");
                Assert.IsNotNull(rasterEntry);
                Assert.AreEqual("Custom3DWidget", rasterEntry.NodeName);
                Assert.AreEqual("UNSUPPORTED", rasterEntry.NodeType);
                Assert.Contains("Unsupported node type", rasterEntry.Reason);

                // Assert MissingFont logged in MissingFonts
                Assert.IsTrue(reportData.MissingFonts.Count >= 1);
                var fontEntry = reportData.MissingFonts.Find(f => f.NodeId == "text-99");
                Assert.IsNotNull(fontEntry);
                Assert.AreEqual("NonExistentFontFamilyPhase7", fontEntry.FontFamily);
                Assert.AreEqual("FallbackTitleLabel", fontEntry.NodeName);

                // 5. Generate Markdown & HTML Reports
                var reportResult = ReportGenerator.GenerateReports(reportData, tempDir);
                Assert.IsNotNull(reportResult);
                Assert.IsTrue(reportResult.Success);

                // 6. Assert files generated on disk
                Assert.IsTrue(File.Exists(reportResult.MarkdownPath));
                Assert.IsTrue(File.Exists(reportResult.HtmlPath));

                string mdText = File.ReadAllText(reportResult.MarkdownPath);
                Assert.Contains("# Figma2Unity Import Report: TestFallbackPackage", mdText);
                Assert.Contains("unsup-1", mdText);
                Assert.Contains("Custom3DWidget", mdText);
                Assert.Contains("NonExistentFontFamilyPhase7", mdText);

                string htmlText = File.ReadAllText(reportResult.HtmlPath);
                Assert.Contains("<title>Figma2Unity Report - TestFallbackPackage</title>", htmlText);
                Assert.Contains("unsup-1", htmlText);
                Assert.Contains("NonExistentFontFamilyPhase7", htmlText);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}
