using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JitterGang.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace jitterGangs.Admin
{
    public partial class LicenseManagerViewModel : ObservableObject
    {
        private readonly IFirebaseService _adminService;

        [ObservableProperty]
        private ObservableCollection<LicenseViewModel> _licenses = new();

        [ObservableProperty]
        private string _statusText = string.Empty;

        [ObservableProperty]
        private string _totalLicenses = string.Empty;

        [ObservableProperty]
        private LicenseViewModel _selectedLicense;

        public LicenseManagerViewModel(IFirebaseService adminService)
        {
            _adminService = adminService;
        }

        public async Task InitializeAsync()
        {
            await LoadLicensesAsync();
        }

        [RelayCommand]
        private async Task LoadLicensesAsync()
        {
            try
            {
                StatusText = "Loading licenses...";
                var licenses = await _adminService.GetAllLicensesAsync();

                Licenses.Clear();
                foreach (var license in licenses)
                {
                    Licenses.Add(new LicenseViewModel(license));
                }

                UpdateStatus();
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task GenerateLicenseAsync()
        {
            try
            {
                StatusText = "Generating new license...";
                var newLicense = await _adminService.GenerateLicenseAsync();

                // Copy to clipboard
                Clipboard.SetText(newLicense);
                StatusText = "New license generated and copied to clipboard";

                await LoadLicensesAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Error generating license: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RevokeLicenseAsync(LicenseViewModel license)
        {
            if (license == null) return;

            var result = await ShowConfirmationDialog(
                $"Are you sure you want to revoke license {license.Key}?",
                "Confirm Revoke");

            if (result)
            {
                try
                {
                    StatusText = "Revoking license...";
                    if (await _adminService.RevokeLicenseAsync(license.Key))
                    {
                        await LoadLicensesAsync();
                    }
                    else
                    {
                        StatusText = "Failed to revoke license";
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task ResetHwidAsync(LicenseViewModel license)
        {
            if (license == null) return;

            var result = await ShowConfirmationDialog(
                $"Are you sure you want to reset HWID for license {license.Key}?",
                "Confirm Reset");

            if (result)
            {
                try
                {
                    StatusText = "Resetting HWID...";
                    if (await _adminService.ResetHWIDAsync(license.Key))
                    {
                        await LoadLicensesAsync();
                    }
                    else
                    {
                        StatusText = "Failed to reset HWID";
                    }
                }
                catch (Exception ex)
                {
                    StatusText = $"Error: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private void CopyLicenseKey(LicenseViewModel license)
        {
            if (license == null) return;

            Clipboard.SetText(license.Key);
            StatusText = "License key copied to clipboard";
        }

        [RelayCommand]
        private void Close()
        {
            // The view will need to handle this by subscribing to this event
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CloseRequested;

        private void UpdateStatus()
        {
            var activeCount = Licenses.Count(l => l.IsValid);
            TotalLicenses = $"Total: {Licenses.Count} (Active: {activeCount})";
            StatusText = "Ready";
        }

        // This would be handled by a service in a real implementation
        private Task<bool> ShowConfirmationDialog(string message, string title)
        {
            // This is a placeholder. In a real implementation, we'd use a dialog service
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }
    }
}