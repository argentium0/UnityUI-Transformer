using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UnityUITransformer.App.Services
{
    public class SecureStorageService
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("UnityUI-Transformer-DPAPI-Entropy-v1");
        private readonly string _filePath;

        public SecureStorageService(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                _filePath = customPath;
            }
            else
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string folder = Path.Combine(localAppData, "UnityUITransformer");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                _filePath = Path.Combine(folder, "session.dat");
            }
        }

        public void SaveSessionToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(token);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_filePath, encryptedBytes);
            }
            catch (Exception ex)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"[DPAPI SECURITY ERROR] Failed to encrypt session token: {ex.Message}");
            }
        }

        public string? LoadSessionToken()
        {
            try
            {
                if (!File.Exists(_filePath)) return null;

                byte[] encryptedBytes = File.ReadAllBytes(_filePath);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }

        public void ClearSessionToken()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    ShimLogSink.RaiseLog(ShimLogLevel.Info, $"[DPAPI SECURE PURGE] Deleted session token file at: {_filePath}");
                }
            }
            catch (Exception ex)
            {
                ShimLogSink.RaiseLog(ShimLogLevel.Error, $"[DPAPI PURGE ERROR] Failed to delete session token file: {ex.Message}");
            }
        }

        public void ClearSession()
        {
            ClearSessionToken();
        }
    }
}
