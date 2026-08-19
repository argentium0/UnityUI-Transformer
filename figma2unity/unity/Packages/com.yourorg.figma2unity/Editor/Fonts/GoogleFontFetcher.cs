using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using Figma2Unity.Editor.Generator;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Figma2Unity.Editor.Fonts
{
    public static class GoogleFontFetcher
    {
        private const string UserAgentHeader = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        public static string FormatFontFamilyForUrl(string fontFamily)
        {
            if (string.IsNullOrEmpty(fontFamily)) return string.Empty;
            string clean = fontFamily.Trim();
            return Uri.EscapeDataString(clean).Replace("%20", "+");
        }

        public static string GetCleanFontName(string fontFamily)
        {
            if (string.IsNullOrEmpty(fontFamily)) return "DefaultFont";
            return Regex.Replace(fontFamily, @"\s+", "");
        }

        public static string FetchGoogleFont(string fontFamily, string outputDirectory = null)
        {
            if (string.IsNullOrEmpty(fontFamily))
            {
                Debug.LogWarning("[Figma2Unity FontPipeline] FetchGoogleFont called with null or empty fontFamily.");
                return null;
            }

            string cleanFontName = GetCleanFontName(fontFamily);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = Path.Combine("Assets", "Figma2UnityImports", "Fonts");
            }

            string expectedFileName = $"{cleanFontName}.ttf";
            string targetPath = Path.Combine(outputDirectory, expectedFileName).Replace('\\', '/');

            // 1. Return immediately if font TTF already exists on disk
            if (File.Exists(targetPath))
            {
                Debug.Log($"[Figma2Unity FontPipeline] Font file already exists on disk at '{targetPath}'. Skipping download.");
                return targetPath;
            }

            Debug.Log($"[Figma2Unity FontPipeline] Missing font detected: '{fontFamily}' (Clean: '{cleanFontName}'). Initiating Google Fonts download...");

            try
            {
                string formattedFamily = FormatFontFamilyForUrl(fontFamily);
                string cssUrl = $"https://fonts.googleapis.com/css2?family={formattedFamily}";

                Debug.Log($"[Figma2Unity FontPipeline] Pinging Google Fonts API URL: '{cssUrl}'");

                // 2. Fetch Google Fonts CSS stylesheet
                string cssContent = DownloadStringWithUserAgent(cssUrl);
                if (string.IsNullOrEmpty(cssContent))
                {
                    Debug.LogError($"[Figma2Unity FontPipeline] Empty CSS response received from Google Fonts API for '{fontFamily}' at '{cssUrl}'");
                    return null;
                }

                // 3. Extract TTF download URL from @font-face declaration
                Match match = Regex.Match(cssContent, @"url\((https://fonts\.gstatic\.com/[^)]+\.(?:ttf|otf))\)");
                if (!match.Success)
                {
                    // Fallback search for any HTTP font URL in CSS
                    match = Regex.Match(cssContent, @"url\((https?://[^)]+\.(?:ttf|otf))\)");
                }

                if (!match.Success)
                {
                    Debug.LogError($"[Figma2Unity FontPipeline] Could not extract TTF font URL from CSS for font '{fontFamily}'. CSS content snippet:\n{cssContent.Substring(0, Math.Min(200, cssContent.Length))}");
                    return null;
                }

                string fontFileUrl = match.Groups[1].Value;
                Debug.Log($"[Figma2Unity FontPipeline] TTF URL extracted: '{fontFileUrl}'. Downloading binary data...");

                // 4. Download binary font bytes
                byte[] fontData = DownloadDataWithUserAgent(fontFileUrl);
                if (fontData == null || fontData.Length == 0)
                {
                    Debug.LogError($"[Figma2Unity FontPipeline] Failed to download binary font data from '{fontFileUrl}'");
                    return null;
                }

                Debug.Log($"[Figma2Unity FontPipeline] Downloaded {fontData.Length} bytes of TTF data. Saving to disk at '{targetPath}'...");

                // 5. Save TTF file using sanitized path helper
                UIToolkitGenerator.SaveAssetBytes(targetPath, fontData);

                bool fileSaved = File.Exists(targetPath);
                if (fileSaved)
                {
                    Debug.Log($"[Figma2Unity FontPipeline] TTF saved successfully to disk: '{targetPath}' (Size: {new FileInfo(targetPath).Length} bytes)");
                }
                else
                {
                    Debug.LogError($"[Figma2Unity FontPipeline] File.WriteAllBytes failed! Target file does not exist at '{targetPath}' after saving!");
                    return null;
                }

                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Figma2Unity FontPipeline] Exception during Google Font fetch for '{fontFamily}': {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private static string DownloadStringWithUserAgent(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = UserAgentHeader;
                request.Timeout = 10000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Figma2Unity FontPipeline] HTTP GET request failed for '{url}': {ex.Message}");
                return null;
            }
        }

        private static byte[] DownloadDataWithUserAgent(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = UserAgentHeader;
                request.Timeout = 15000;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var ms = new MemoryStream())
                {
                    response.GetResponseStream().CopyTo(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Figma2Unity FontPipeline] HTTP binary download failed for '{url}': {ex.Message}");
                return null;
            }
        }
    }
}
