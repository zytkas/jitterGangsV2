using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JitterGang.Services;
using jitterGangs.Admin;
using System.Windows;

namespace jitterGangs
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IFirebaseService _userService;

        [ObservableProperty]
        private string _licenseKey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public LoginViewModel(IFirebaseService userService)
        {
            _userService = userService;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (await Task.Run(() => _userService.IsLicenseValid()))
                {
                    OpenMainWindow();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                return false;
            }
        }

        [RelayCommand]
        private async Task ActivateAsync()
        {
            try
            {
                var key = LicenseKey.Trim();
                StatusMessage = "Verifying...";

                // Check if it's an admin key
                if (await _userService.IsAdminKeyAsync(key))
                {
                    OpenAdminWindow();
                    return;
                }

                // Check if it's a valid license
                var result = await _userService.VerifyLicenseAsync(key);
                if (result.IsValid)
                {
                    _userService.SaveLicense(key);
                    OpenMainWindow();
                }
                else
                {
                    StatusMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Close()
        {
            Application.Current.Shutdown();
        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Close the login window
            Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
        }

        private void OpenAdminWindow()
        {
            var adminWindow = new LicenseManagerWindow();
            adminWindow.Show();

            // Close the login window
            Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
        }
    }
}