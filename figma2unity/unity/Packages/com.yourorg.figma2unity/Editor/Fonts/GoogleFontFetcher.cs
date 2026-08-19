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
            if (string.IsNullOrEmpty(fontFamily)) return null;

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
                return targetPath;
            }

            try
            {
                string formattedFamily = FormatFontFamilyForUrl(fontFamily);
                string cssUrl = $"https://fonts.googleapis.com/css2?family={formattedFamily}";

                // 2. Fetch Google Fonts CSS stylesheet
                string cssContent = DownloadStringWithUserAgent(cssUrl);
                if (string.IsNullOrEmpty(cssContent))
                {
                    Debug.LogWarning($"[GoogleFontFetcher] Empty CSS response for Google Font: '{fontFamily}'");
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
                    Debug.LogWarning($"[GoogleFontFetcher] Could not find TTF font URL in CSS for font '{fontFamily}'");
                    return null;
                }

                string fontFileUrl = match.Groups[1].Value;

                // 4. Download binary font bytes
                byte[] fontData = DownloadDataWithUserAgent(fontFileUrl);
                if (fontData == null || fontData.Length == 0)
                {
                    Debug.LogWarning($"[GoogleFontFetcher] Failed to download TTF binary data from {fontFileUrl}");
                    return null;
                }

                // 5. Save TTF file using sanitized path helper
                UIToolkitGenerator.SaveAssetBytes(targetPath, fontData);
                Debug.Log($"[GoogleFontFetcher] Successfully downloaded Google Font '{fontFamily}' to {targetPath}");

                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleFontFetcher] Failed to fetch Google Font '{fontFamily}': {ex.Message}");
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
                Debug.LogWarning($"[GoogleFontFetcher] HTTP GET request failed for {url}: {ex.Message}");
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
                Debug.LogWarning($"[GoogleFontFetcher] HTTP binary download failed for {url}: {ex.Message}");
                return null;
            }
        }
    }
}
