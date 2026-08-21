using System.Threading.Tasks;
using UnityUITransformer.App.Services;
using UnityUITransformer.App.ViewModels;
using UnityEngine;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class MainViewModelTests
    {
        private MainViewModel CreateIsolatedViewModel()
        {
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N") + ".dat");
            return new MainViewModel(secureStorageService: new SecureStorageService(tempFile));
        }

        [Fact]
        public void InitialState_IsDisconnected_And_CanSyncIsFalse()
        {
            var vm = CreateIsolatedViewModel();

            Assert.False(vm.IsConnected);
            Assert.False(vm.IsConnecting);
            Assert.Equal("Disconnected", vm.ConnectionStatusText);
            Assert.False(vm.CanSync);
            Assert.NotEmpty(vm.LogEntries);
        }

        [Fact]
        public void FigmaUrl_Validation_WorksCorrectly()
        {
            var vm = CreateIsolatedViewModel();

            Assert.False(vm.IsFigmaUrlValid);

            vm.FigmaUrl = "invalid_url";
            Assert.False(vm.IsFigmaUrlValid);

            vm.FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2";
            Assert.True(vm.IsFigmaUrlValid);
        }

        [Fact]
        public void UnityPath_Validation_WorksCorrectly()
        {
            var vm = CreateIsolatedViewModel();

            Assert.False(vm.IsUnityPathValid);

            vm.UnityAssetsPath = @"C:\MyProject\Assets\UI";
            Assert.True(vm.IsUnityPathValid);
        }

        [Fact]
        public async Task ConnectCommand_SetsIsConnected_AndUpdatesUser()
        {
            var vm = CreateIsolatedViewModel();

            vm.ConnectCommand.Execute(null);

            // Wait for simulated async connection & profile fetch
            await Task.Delay(2500);

            Assert.True(vm.IsConnected);
            Assert.Equal("Figma Developer", vm.ConnectedUser);
            Assert.Contains("Connected", vm.ConnectionStatusText);
        }

        [Fact]
        public async Task CanSync_IsTrue_OnlyWhenAllStepsAreValid()
        {
            var vm = CreateIsolatedViewModel();
            Assert.False(vm.CanSync);

            // Set valid paths
            vm.FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2";
            vm.UnityAssetsPath = @"C:\MyProject\Assets\UI";
            Assert.False(vm.CanSync); // Still false because not connected

            // Connect
            vm.ConnectCommand.Execute(null);
            await Task.Delay(1400);

            Assert.True(vm.CanSync);
        }

        [Fact]
        public async Task SyncCommand_ExecutesPipeline_AndEmitsShimLogs()
        {
            var vm = CreateIsolatedViewModel();
            vm.FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2";
            vm.UnityAssetsPath = @"C:\MyProject\Assets\UI";

            vm.ConnectCommand.Execute(null);
            await Task.Delay(1400);

            Assert.True(vm.CanSync);

            int initialLogCount = vm.LogEntries.Count;
            vm.SyncCommand.Execute(null);

            await Task.Delay(6000);

            Assert.False(vm.IsSyncing);
            Assert.Equal(100, vm.SyncProgress);
            Assert.True(vm.LogEntries.Count > 0);
        }
    }
}
