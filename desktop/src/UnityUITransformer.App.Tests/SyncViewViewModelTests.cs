using System;
using System.IO;
using UnityUITransformer.App.Services;
using UnityUITransformer.App.ViewModels;
using Xunit;

namespace UnityUITransformer.App.Tests
{
    public class SyncViewViewModelTests
    {
        private MainViewModel CreateIsolatedViewModel()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
            return new MainViewModel(secureStorageService: new SecureStorageService(tempFile));
        }

        [Fact]
        public void OpenFolderCommand_CanExecute_WhenNotSyncing()
        {
            var vm = CreateIsolatedViewModel();

            Assert.True(vm.OpenFolderCommand.CanExecute(null));

            vm.IsSyncing = true;
            Assert.False(vm.OpenFolderCommand.CanExecute(null));
        }

        [Fact]
        public void ResetCommand_ResetsCurrentStepIndexToStep2()
        {
            var vm = CreateIsolatedViewModel();
            vm.CurrentStepIndex = 3;

            Assert.Equal(3, vm.CurrentStepIndex);
            Assert.True(vm.ResetCommand.CanExecute(null));

            vm.ResetCommand.Execute(null);

            Assert.Equal(2, vm.CurrentStepIndex);
        }
    }
}
