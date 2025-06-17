using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JitterGang.Models;
using JitterGang.Services;
using JitterGang.Services.Input;
using JitterGang.Services.Input.Controllers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace JitterGang.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IJitterService _jitterService;
    private readonly IInputInterceptorService _inputInterceptorService;
    private bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))] 
    private bool _isInputSystemReady;

    [ObservableProperty]
    private string _inputSystemStatus = "Checking...";

    public bool CanStart => !IsRunning && IsInputSystemReady;
    public IJitterService JitterService => _jitterService;

    [ObservableProperty]
    private ObservableCollection<string> _processes;

    [ObservableProperty]
    private JitterSettings _settings;

    public MainViewModel(ISettingsService settingsService, IJitterService jitterService, IInputInterceptorService inputInterceptorService)
    {
        _settingsService = settingsService;
        _jitterService = jitterService;
        _inputInterceptorService = inputInterceptorService;

        _processes = new ObservableCollection<string>();
        _settings = new JitterSettings();

        _settings.PropertyChanged += Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(JitterSettings.Strength):
                _jitterService.UpdateStrength(Settings.Strength);
                _jitterService.UpdateJitters();
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.PullDownStrength):
                _jitterService.UpdatePullDownStrength(Settings.PullDownStrength);
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.Delay):
                _jitterService.SetDelay(Settings.Delay);
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.SelectedProcess):
                _jitterService.SetSelectedProcess(Settings.SelectedProcess);
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.ToggleKey):
                int keyCode = ConvertKeyNameToCode(Settings.ToggleKey);
                _jitterService.SetToggleKey(keyCode);
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.IsCircleJitterActive):
                _jitterService.IsCircleJitterActive = Settings.IsCircleJitterActive;
                RestartServiceIfRunning();
                break;

            case nameof(JitterSettings.UseAdsOnly):
                _jitterService.UseAdsOnly = Settings.UseAdsOnly;
                RestartServiceIfRunning();
                break;
        }

        // Сохраняем настройки после каждого изменения
        SaveSettingsAsync().ConfigureAwait(false);
    }

    private void RestartServiceIfRunning()
    {
        if (IsRunning)
        {
            _jitterService.Stop();
            _jitterService.Start();
        }
    }

    [RelayCommand]
    private void TestBinding()
    {
        Logger.Log("=== Testing Property Binding ===");

        // Переключаем состояние для тестирования
        bool oldValue = IsInputSystemReady;

        IsInputSystemReady = false;
        Logger.Log($"Set IsInputSystemReady to false. CanStart: {CanStart}");

        Thread.Sleep(100);

        IsInputSystemReady = true;
        Logger.Log($"Set IsInputSystemReady to true. CanStart: {CanStart}");

        Thread.Sleep(100);

        IsInputSystemReady = oldValue;
        Logger.Log($"Restored IsInputSystemReady to {oldValue}. CanStart: {CanStart}");

        Logger.Log("=== Test Complete ===");
    }


    [RelayCommand]
    private async Task UninstallDriver()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to uninstall the InputInterceptor driver?\n\nThis will require a computer restart and the application will no longer work until the driver is reinstalled.",
                "Confirm Driver Uninstall",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            InputSystemStatus = "Uninstalling driver...";

            bool uninstalled = await _inputInterceptorService.UninstallDriverAsync();
            if (uninstalled)
            {
                InputSystemStatus = "Driver uninstalled. Restart required.";
                IsInputSystemReady = false;

                System.Windows.MessageBox.Show(
                    "InputInterceptor driver has been uninstalled successfully.\n\nPlease restart your computer to complete the removal.",
                    "Driver Uninstalled",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                InputSystemStatus = "Driver uninstallation failed";
                throw new InvalidOperationException(
                    "Failed to uninstall InputInterceptor driver. " +
                    "Please ensure the application is running as administrator.");
            }
        }
        catch (Exception ex)
        {
            InputSystemStatus = $"Uninstall error: {ex.Message}";
            Logger.Log($"Driver uninstallation error: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task Start(CancellationToken token = default)
    {
        try
        {
            if (!IsRunning && IsInputSystemReady)
            {
                ValidateSettings();

                int keyCode = ConvertKeyNameToCode(Settings.ToggleKey);
                _jitterService.SetDelay(Settings.Delay);
                _jitterService.SetToggleKey(keyCode);
                _jitterService.UpdateStrength(Settings.Strength);
                _jitterService.UpdatePullDownStrength(Settings.PullDownStrength);
                _jitterService.SetSelectedProcess(Settings.SelectedProcess);
                _jitterService.UpdateJitters();
                _jitterService.IsCircleJitterActive = Settings.IsCircleJitterActive;
                _jitterService.UseAdsOnly = Settings.UseAdsOnly;

                _jitterService.Start();
                IsRunning = true;
                Logger.Log("Jitter service started with InputInterceptor");
                await SaveSettingsAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error starting jitter: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task Stop(CancellationToken token = default)
    {
        if (IsRunning)
        {
            _jitterService.Stop();
            IsRunning = false;
            await SaveSettingsAsync();
        }
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            Logger.Log("=== Starting MainViewModel Initialization ===");

            // Сначала инициализируем InputInterceptor
            await InitializeInputSystemAsync();
            Logger.Log($"After InputInterceptor init: IsInputSystemReady={IsInputSystemReady}, CanStart={CanStart}");

            var loadedSettings = await _settingsService.LoadSettingsAsync();

            if (loadedSettings != null)
            {
                Settings = loadedSettings;
                Logger.Log("Settings loaded successfully");
            }

            bool isControllerAvailable = ControllerDetector.IsAnyControllerConnected();
            Logger.Log($"Controller available: {isControllerAvailable}");

            if (!isControllerAvailable)
            {
                Settings.UseController = false;
                await SaveSettingsAsync();
            }

            _jitterService.UpdateStrength(Settings.Strength);
            _jitterService.UpdatePullDownStrength(Settings.PullDownStrength);
            _jitterService.IsCircleJitterActive = Settings.IsCircleJitterActive;
            _jitterService.UseAdsOnly = Settings.UseAdsOnly;

            if (Settings.UseController && isControllerAvailable)
            {
                _jitterService.SetUseController(true);
            }

            await RefreshProcessList();

            _isInitialized = true;
            Logger.Log($"=== MainViewModel Initialization Complete === IsInputSystemReady={IsInputSystemReady}, CanStart={CanStart}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error in InitializeAsync: {ex.Message}");
            throw;
        }
    }

    private async Task InitializeInputSystemAsync()
    {
        try
        {
            InputSystemStatus = "Checking InputInterceptor driver...";
            Logger.Log("Starting InputInterceptor initialization...");

            if (!_inputInterceptorService.IsDriverInstalled)
            {
                InputSystemStatus = "Driver not installed. Install required.";
                IsInputSystemReady = false;
                Logger.Log("InputInterceptor driver is not installed");

                throw new InvalidOperationException(
                    "InputInterceptor driver is not installed. " +
                    "Please run the application as administrator to install the driver, " +
                    "then restart your computer.");
            }

            InputSystemStatus = "Initializing InputInterceptor...";
            Logger.Log("Driver is installed, initializing...");

            bool initialized = await _inputInterceptorService.InitializeAsync();
            if (!initialized)
            {
                InputSystemStatus = "Failed to initialize InputInterceptor";
                IsInputSystemReady = false;
                Logger.Log("Failed to initialize InputInterceptor");

                throw new InvalidOperationException(
                    "Failed to initialize InputInterceptor. " +
                    "Please ensure the driver is properly installed and restart the application.");
            }

            InputSystemStatus = "InputInterceptor ready";
            IsInputSystemReady = true;
            Logger.Log($"InputInterceptor successfully initialized. IsInputSystemReady: {IsInputSystemReady}, CanStart: {CanStart}");

            // Принудительно обновляем привязки
            OnPropertyChanged(nameof(IsInputSystemReady));
            OnPropertyChanged(nameof(CanStart));
        }
        catch (Exception ex)
        {
            InputSystemStatus = $"Error: {ex.Message}";
            IsInputSystemReady = false;
            Logger.Log($"InputInterceptor initialization failed: {ex.Message}");

            // Принудительно обновляем привязки даже при ошибке
            OnPropertyChanged(nameof(IsInputSystemReady));
            OnPropertyChanged(nameof(CanStart));
            throw;
        }
    }

    [RelayCommand]
    private async Task InstallDriver()
    {
        try
        {
            InputSystemStatus = "Installing driver...";
            Logger.Log("Starting driver installation...");

            bool result = await _inputInterceptorService.InstallDriverAsync();
            if (result)
            {
                InputSystemStatus = "Driver installed. Restart required.";
                IsInputSystemReady = false; // Остается false до перезагрузки
                Logger.Log("Driver installation successful");

                // Принудительно обновляем привязки
                OnPropertyChanged(nameof(IsInputSystemReady));
                OnPropertyChanged(nameof(CanStart));

                // Показываем сообщение пользователю
                System.Windows.MessageBox.Show(
                    "InputInterceptor driver installed successfully.\n\nPlease restart your computer and the application.",
                    "Installation Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                InputSystemStatus = "Driver installation failed";
                IsInputSystemReady = false;
                Logger.Log("Driver installation failed");

                // Принудительно обновляем привязки
                OnPropertyChanged(nameof(IsInputSystemReady));
                OnPropertyChanged(nameof(CanStart));

                throw new InvalidOperationException(
                    "Failed to install InputInterceptor driver. " +
                    "Please ensure the application is running as administrator.");
            }
        }
        catch (Exception ex)
        {
            InputSystemStatus = $"Installation error: {ex.Message}";
            IsInputSystemReady = false;
            Logger.Log($"Driver installation error: {ex.Message}");

            // Принудительно обновляем привязки
            OnPropertyChanged(nameof(IsInputSystemReady));
            OnPropertyChanged(nameof(CanStart));
            throw;
        }
    }

    private static int ConvertKeyNameToCode(string keyName)
    {
        return keyName switch
        {
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "Shift" => 0x10,
            "Capslock" => 0x14,
            "X1" => 0x05,
            "X2" => 0x06,
            _ => 0x70 // F1 по умолчанию
        };
    }

    [RelayCommand]
    public async Task RefreshProcessList(CancellationToken token = default)
    {
        try
        {
            var processList = await Task.Run(() =>
            {
                return Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            // Проверяем есть ли у процесса главное окно
                            if (p.MainWindowHandle == IntPtr.Zero) return false;

                            // Исключаем системные процессы
                            string[] excludedProcesses = [
                                "svchost", "csrss", "smss", "services", "lsass",
                                "winlogon", "spoolsv", "explorer", "taskmgr",
                                "devenv", "conhost", "RuntimeBroker", "SearchUI",
                                "ShellExperienceHost", "sihost", "ApplicationFrameHost"
                            ];

                            if (excludedProcesses.Contains(p.ProcessName.ToLower())) return false;

                            // Проверяем, что процесс отвечает и имеет заголовок окна
                            return !p.HasExited && !string.IsNullOrEmpty(p.MainWindowTitle);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .Select(p => p.ProcessName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();
            });

            // Создаем новую коллекцию вместо очистки существующей
            Processes = new ObservableCollection<string>(processList);

            // Сохраняем текущий выбранный процесс
            if (!string.IsNullOrEmpty(Settings.SelectedProcess) &&
                Processes.Contains(Settings.SelectedProcess))
            {
                Settings.SelectedProcess = Settings.SelectedProcess;
            }
            else if (Processes.Any())
            {
                Settings.SelectedProcess = Processes.First();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error refreshing process list: {ex.Message}");
            throw;
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveSettingsAsync(Settings);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error saving settings: {ex.Message}");
            throw;
        }
    }

    private void ValidateSettings()
    {
        if (Settings.Strength < 1)
            Settings.Strength = 1;

        if (Settings.Delay < 1)
            Settings.Delay = 1;

        if (Settings.Strength == 0 && Settings.PullDownStrength == 0)
            Settings.Strength = 1;

        if (string.IsNullOrWhiteSpace(Settings.SelectedProcess))
            throw new ArgumentException("Process not selected");
    }

    public async Task UpdateControllerState(bool useController)
    {
        try
        {
            if (useController)
            {
                _jitterService.SetUseController(useController);
            }
            Settings.UseController = useController;
            await SaveSettingsAsync();
        }
        catch (Exception)
        {
            Settings.UseController = false;
            await SaveSettingsAsync();
            throw;
        }
    }

    [RelayCommand]
    private async Task Shutdown(CancellationToken token = default)
    {
        try
        {
            // Останавливаем все активные процессы
            if (IsRunning)
            {
                await StopCommand.ExecuteAsync(null);
            }

            // Сохраняем настройки
            await SaveSettingsAsync();

            // Освобождаем ресурсы
            Cleanup();
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during shutdown: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleController(bool useController)
    {
        try
        {
            if (useController)
            {
                if (!ControllerDetector.IsAnyControllerConnected())
                {
                    Settings.UseController = false;
                    throw new InvalidOperationException("Connect controller to use this feature");
                }

                _jitterService.SetUseController(true);
            }
            else
            {
                _jitterService.SetUseController(false);
            }

            Settings.UseController = useController;
            await SaveSettingsAsync();
        }
        catch (Exception ex)
        {
            Settings.UseController = false;
            await SaveSettingsAsync();
            throw;
        }
    }

    public IEnumerable<string> AvailableKeys { get; } = new[]
    {
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
        "X1", "X2", "Shift", "Capslock"
    };

    public void Cleanup()
    {
        _jitterService.Stop();
        _jitterService.Dispose();
    }
}