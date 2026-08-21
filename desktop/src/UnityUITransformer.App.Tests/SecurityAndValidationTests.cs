using System;
using System.IO;
using UnityUITransformer.App.Services;
using UnityUITransformer.App.ViewModels;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class SecurityAndValidationTests
    {
        public SecurityAndValidationTests()
        {
            new SecureStorageService().ClearSessionToken();
        }
        [Fact]
        public void SecureStorageService_EncryptsAndDecryptsTokenSuccessfully()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            string tempFile = Path.Combine(tempFolder, "test_session.dat");

            try
            {
                var storage = new SecureStorageService(tempFile);
                string testToken = "sb-access-token-secret-12345";

                storage.SaveSessionToken(testToken);
                Assert.True(File.Exists(tempFile));

                string? restoredToken = storage.LoadSessionToken();
                Assert.Equal(testToken, restoredToken);

                storage.ClearSessionToken();
                Assert.False(File.Exists(tempFile));
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }

        [Fact]
        public void ManualPdfGenerator_GeneratesValidPdfFile()
        {
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"TestManual_{Guid.NewGuid():N}.pdf");

            try
            {
                ManualPdfGenerator.GeneratePdfManual(tempPdfPath);

                Assert.True(File.Exists(tempPdfPath));
                byte[] pdfBytes = File.ReadAllBytes(tempPdfPath);
                Assert.True(pdfBytes.Length > 500);

                string header = System.Text.Encoding.UTF8.GetString(pdfBytes, 0, 8);
                Assert.StartsWith("%PDF-1.4", header);
            }
            finally
            {
                if (File.Exists(tempPdfPath))
                {
                    File.Delete(tempPdfPath);
                }
            }
        }

        private MainViewModel CreateIsolatedViewModel()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
            return new MainViewModel(secureStorageService: new SecureStorageService(tempFile));
        }

        [Fact]
        public void ValidationGuardrail_FailsWhenFigmaUrlIsInvalid()
        {
            var vm = CreateIsolatedViewModel();
            vm.CurrentStepIndex = 2;
            vm.FigmaUrl = "https://example.com/invalid-link";
            vm.UnityAssetsPath = Environment.CurrentDirectory;

            vm.ContinueToSyncCommand.Execute(null);

            Assert.True(vm.HasConfigValidationError);
            Assert.Equal("Invalid Figma URL. Please ensure it contains a specific node-id.", vm.ConfigValidationError);
            Assert.Equal(2, vm.CurrentStepIndex); // Should NOT proceed to Step 3
        }

        [Fact]
        public void ValidationGuardrail_FailsWhenUnityDirectoryDoesNotExist()
        {
            var vm = CreateIsolatedViewModel();
            vm.CurrentStepIndex = 2;
            vm.FigmaUrl = "https://figma.com/file/XYZ12345/AppUI?node-id=1:2";
            vm.UnityAssetsPath = @"C:\NonExistentPath\Directory_" + Guid.NewGuid().ToString("N");

            Assert.False(vm.HasConfigValidationError);
            vm.ContinueToSyncCommand.Execute(null);

            Assert.True(vm.HasConfigValidationError);
            Assert.Equal("Please select a valid local Unity directory.", vm.ConfigValidationError);
            Assert.Equal(2, vm.CurrentStepIndex); // Should NOT proceed to Step 3
        }
    }
}
