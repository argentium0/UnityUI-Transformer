using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Figma2Unity.Editor.VisualRegression;

namespace Figma2Unity.Tests.Runtime
{
    public class VisualRegressionTests
    {
        private GameObject _testGameObject;
        private UIDocument _uiDocument;
        private Camera _testCamera;

        public const float MaxDiffThresholdPercent = 2.0f;

        [SetUp]
        public void SetUp()
        {
            // 1. Ensure a main camera exists in the test scene
            if (Camera.main == null)
            {
                var cameraGo = new GameObject("TestCamera");
                _testCamera = cameraGo.AddComponent<Camera>();
                _testCamera.clearFlags = CameraClearFlags.SolidColor;
                _testCamera.backgroundColor = Color.black;
                _testCamera.tag = "MainCamera";
            }

            // 2. Instantiate GameObject with UIDocument component
            _testGameObject = new GameObject("TestUIDocumentHost");
            _uiDocument = _testGameObject.AddComponent<UIDocument>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
            if (_testCamera != null)
            {
                Object.DestroyImmediate(_testCamera.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator RenderUXMLScreen_CapturesScreenshotAndComparesPixels()
        {
            // Load target generated VisualTreeAsset (UXML) or instantiate test tree
            var uxmlAsset = Resources.Load<VisualTreeAsset>("TestScreen");
            if (uxmlAsset == null)
            {
                uxmlAsset = ScriptableObject.CreateInstance<VisualTreeAsset>();
            }

            _uiDocument.visualTreeAsset = uxmlAsset;

            // Wait for UI Toolkit layout engine computation & render pass
            yield return null;
            yield return new WaitForEndOfFrame();

            // Prepare temporary directory for screenshot artifact & diff heatmap
            string outputFolder = Path.Combine(Application.temporaryCachePath, "Figma2UnityVisualRegression");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string screenshotPath = Path.Combine(outputFolder, "RenderedUI_Capture.png");
            string diffOutputPath = Path.Combine(outputFolder, "RenderedUI_Diff.png");

            // Capture screenshot of rendered scene
            ScreenCapture.CaptureScreenshot(screenshotPath);

            // Wait 1 frame for frame write synchronization
            yield return null;

            Assert.IsNotNull(_uiDocument);
            Assert.IsNotNull(_uiDocument.rootVisualElement);

            // Look for imported Figma reference export image if available
            string referencePath = Path.Combine(Application.dataPath, "Figma2UnityImports", "SyncPackage", "exports", "images", "reference.png");

            // If reference PNG exists, compare captured screenshot with reference
            if (File.Exists(screenshotPath) && File.Exists(referencePath))
            {
                ImageDiffResult diffResult = ImageDiffUtility.CompareImages(screenshotPath, referencePath, MaxDiffThresholdPercent, diffOutputPath);

                if (!diffResult.Passed)
                {
                    Assert.Fail(diffResult.ErrorMessage);
                }
            }
            else
            {
                Assert.IsTrue(Directory.Exists(outputFolder));
            }
        }
    }
}
