using System.Threading.Tasks;
using UnityUITransformer.App.ViewModels;
using UnityEngine;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class MainViewModelTests
    {
        [Fact]
        public void InitialState_IsDisconnected_And_CanSyncIsFalse()
        {
            var vm = new MainViewModel();

            Assert.False(vm.IsConnected);
            Assert.False(vm.IsConnecting);
            Assert.Equal("Disconnected", vm.ConnectionStatusText);
            Assert.False(vm.CanSync);
            Assert.NotEmpty(vm.LogEntries);
        }

        [Fact]
        public void FigmaUrl_Validation_WorksCorrectly()
        {
            var vm = new MainViewModel();

            Assert.False(vm.IsFigmaUrlValid);

            vm.FigmaUrl = "invalid_url";
            Assert.False(vm.IsFigmaUrlValid);

            vm.FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2";
            Assert.True(vm.IsFigmaUrlValid);
        }

        [Fact]
        public void UnityPath_Validation_WorksCorrectly()
        {
            var vm = new MainViewModel();

            Assert.False(vm.IsUnityPathValid);

            vm.UnityAssetsPath = @"C:\MyProject\Assets\UI";
            Assert.True(vm.IsUnityPathValid);
        }

        [Fact]
        public async Task ConnectCommand_SetsIsConnected_AndUpdatesUser()
        {
            var vm = new MainViewModel();

            vm.ConnectCommand.Execute(null);

            // Wait for simulated async connection
            await Task.Delay(1400);

            Assert.True(vm.IsConnected);
            Assert.False(vm.IsConnecting);
            Assert.Equal("Alex (Design Lead)", vm.ConnectedUser);
            Assert.Contains("Connected", vm.ConnectionStatusText);
        }

        [Fact]
        public async Task CanSync_IsTrue_OnlyWhenAllStepsAreValid()
        {
            var vm = new MainViewModel();
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
            var vm = new MainViewModel
            {
                FigmaUrl = "https://www.figma.com/file/XYZ12345/App-Design?node-id=1:2",
                UnityAssetsPath = @"C:\MyProject\Assets\UI"
            };

            vm.ConnectCommand.Execute(null);
            await Task.Delay(1400);

            Assert.True(vm.CanSync);

            int initialLogCount = vm.LogEntries.Count;
            vm.SyncCommand.Execute(null);

            await Task.Delay(2500);

            Assert.False(vm.IsSyncing);
            Assert.Equal(100, vm.SyncProgress);
            Assert.True(vm.LogEntries.Count > initialLogCount);
        }
    }
}
