using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.VisualRegression
{
    public class ImageDiffResult
    {
        public bool Passed;
        public float DifferencePercentage;
        public int TotalPixels;
        public int DifferingPixels;
        public string DiffImagePath;
        public string ErrorMessage;
    }

    public static class ImageDiffUtility
    {
        public static ImageDiffResult CompareImages(string capturePath, string referencePath, float maxDiffThresholdPercent = 2.0f, string outputDiffPath = null)
        {
            var result = new ImageDiffResult();

            if (!File.Exists(capturePath))
            {
                result.Passed = false;
                result.ErrorMessage = $"Captured image file not found at path: {capturePath}";
                return result;
            }

            if (!File.Exists(referencePath))
            {
                result.Passed = false;
                result.ErrorMessage = $"Reference image file not found at path: {referencePath}";
                return result;
            }

            byte[] captureBytes = File.ReadAllBytes(capturePath);
            byte[] referenceBytes = File.ReadAllBytes(referencePath);

            Texture2D texCapture = new Texture2D(2, 2);
            Texture2D texReference = new Texture2D(2, 2);

            if (!texCapture.LoadImage(captureBytes) || !texReference.LoadImage(referenceBytes))
            {
                result.Passed = false;
                result.ErrorMessage = "Failed to decode PNG image textures.";
                return result;
            }

            int width = Math.Min(texCapture.width, texReference.width);
            int height = Math.Min(texCapture.height, texReference.height);
            int totalPixels = width * height;

            if (totalPixels == 0)
            {
                result.Passed = false;
                result.ErrorMessage = "Invalid image dimensions (0x0).";
                return result;
            }

            Color[] pixelsCapture = texCapture.GetPixels(0, 0, width, height);
            Color[] pixelsReference = texReference.GetPixels(0, 0, width, height);

            Texture2D texDiff = new Texture2D(width, height);
            Color[] pixelsDiff = new Color[totalPixels];

            int diffCount = 0;
            float thresholdPerChannel = 0.05f; // Color tolerance

            for (int i = 0; i < totalPixels; i++)
            {
                Color c1 = pixelsCapture[i];
                Color c2 = pixelsReference[i];

                float diffR = Math.Abs(c1.r - c2.r);
                float diffG = Math.Abs(c1.g - c2.g);
                float diffB = Math.Abs(c1.b - c2.b);
                float diffA = Math.Abs(c1.a - c2.a);

                if (diffR > thresholdPerChannel || diffG > thresholdPerChannel || diffB > thresholdPerChannel || diffA > thresholdPerChannel)
                {
                    diffCount++;
                    // Highlight differing pixels with bright red overlay
                    pixelsDiff[i] = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                }
                else
                {
                    // Dim matching background pixels for visual context
                    float gray = (c1.r + c1.g + c1.b) / 3.0f * 0.3f;
                    pixelsDiff[i] = new Color(gray, gray, gray, 1.0f);
                }
            }

            float diffPercentage = ((float)diffCount / totalPixels) * 100.0f;

            result.TotalPixels = totalPixels;
            result.DifferingPixels = diffCount;
            result.DifferencePercentage = diffPercentage;
            result.Passed = diffPercentage <= maxDiffThresholdPercent;

            if (!result.Passed)
            {
                result.ErrorMessage = $"Visual regression check FAILED: {diffPercentage:F2}% differing pixels exceeds target threshold of {maxDiffThresholdPercent:F2}% ({diffCount}/{totalPixels} pixels diff).";
            }
            else
            {
                result.ErrorMessage = $"Visual regression check PASSED: {diffPercentage:F2}% differing pixels is within target threshold of {maxDiffThresholdPercent:F2}%.";
            }

            if (!string.IsNullOrEmpty(outputDiffPath))
            {
                texDiff.SetPixels(pixelsDiff);
                texDiff.Apply();
                byte[] diffPngBytes = texDiff.EncodeToPNG();
                string dir = Path.GetDirectoryName(outputDiffPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(outputDiffPath, diffPngBytes);
                result.DiffImagePath = outputDiffPath;
            }

            return result;
        }
    }
}
