using System;
using System.IO;
using UnityUITransformer.App.Services;
using UnityUITransformer.App.ViewModels;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class BindingAndValidationFixTests
    {
        private MainViewModel CreateIsolatedViewModel()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
            return new MainViewModel(secureStorageService: new SecureStorageService(tempFile));
        }

        [Fact]
        public void SyncViewModel_SyncProgressProperty_HasReadWriteAccessors()
        {
            var main = CreateIsolatedViewModel();
            var syncVm = new SyncViewModel(main);

            main.SyncProgress = 50.0;
            Assert.Equal(50.0, syncVm.SyncProgress);

            syncVm.SyncProgress = 75.0;
            Assert.Equal(75.0, main.SyncProgress);
        }

        [Fact]
        public void FigmaUrl_WithoutNodeId_FailsValidation_AndSetsErrorMessage()
        {
            var main = CreateIsolatedViewModel();
            var configVm = new ConfigViewModel(main);

            // URL without node-id
            configVm.FigmaUrl = "https://www.figma.com/file/XYZ12345/Untitled?m=auto&t=123";

            Assert.False(configVm.IsFigmaUrlValid);
            Assert.True(configVm.HasConfigValidationError);
            Assert.True(configVm.HasError);
            Assert.Equal("Invalid Figma URL. A specific node-id is required.", configVm.ErrorMessage);

            // URL with node-id
            configVm.FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2";

            Assert.True(configVm.IsFigmaUrlValid);
            Assert.False(configVm.HasConfigValidationError);
            Assert.False(configVm.HasError);
            Assert.Empty(configVm.ErrorMessage);
        }
    }
}
