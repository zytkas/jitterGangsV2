using System.IO;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Threading;

namespace jitterGangs
{
    public partial class App : Application
    {
        private readonly string _driversPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers");
        private static readonly object _driverLock = new object();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DependencyContainer.Initialize();
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            _ = Task.Run(async () => await PerformStartupEACBypassAsync());

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _ = Task.Run(async () => await PerformShutdownEACBypassAsync());

            base.OnExit(e);
        }


        private async Task PerformStartupEACBypassAsync()
        {
            if (!IsRunningAsAdministrator())
            {
                Logger.Log("EAC Bypass: Not running as administrator, skipping driver operations");
                return;
            }

            try
            {
                lock (_driverLock)
                {
                    Logger.Log("=== Starting EAC Bypass (Startup) ===");
                    StopInterceptionService();
                    Thread.Sleep(2000);
                    RenameDriversForEAC();
                    StartInterceptionService();

                    Logger.Log("=== EAC Bypass (Startup) Complete ===");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass startup error: {ex.Message}");
            }
        }
        private async Task PerformShutdownEACBypassAsync()
        {
            if (!IsRunningAsAdministrator())
            {
                Logger.Log("EAC Bypass: Not running as administrator, skipping driver restore");
                return;
            }

            try
            {
                lock (_driverLock)
                {
                    Logger.Log("=== Starting EAC Bypass (Shutdown) ===");
                    StopInterceptionService();
                    Thread.Sleep(3000);
                    RestoreDriverNames();

                    Logger.Log("=== EAC Bypass (Shutdown) Complete ===");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass shutdown error: {ex.Message}");
            }
        }

        private void StopInterceptionService()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses()
                    .Where(p => p.ProcessName.ToLower().Contains("interception") ||
                               p.ProcessName.ToLower().Contains("inputinterceptor"))
                    .ToList();

                foreach (var process in processes)
                {
                    try
                    {
                        Logger.Log($"Terminating process: {process.ProcessName} (PID: {process.Id})");
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to kill process {process.ProcessName}: {ex.Message}");
                    }
                }
                try
                {
                    var service = ServiceController.GetServices()
                        .FirstOrDefault(s => s.ServiceName.ToLower().Contains("interception"));

                    if (service != null && service.Status == ServiceControllerStatus.Running)
                    {
                        Logger.Log($"Stopping service: {service.ServiceName}");
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Service stop error: {ex.Message}");
                }

                Logger.Log("Interception processes stopped");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error stopping interception service: {ex.Message}");
            }
        }


        private void StartInterceptionService()
        {
            try
            {
                Logger.Log("Service will be started by InputInterceptor when needed");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error starting interception service: {ex.Message}");
            }
        }
        private void RenameDriversForEAC()
        {
            try
            {
                if (IsFileInUse(Path.Combine(_driversPath, "mouse.sys")) ||
                    IsFileInUse(Path.Combine(_driversPath, "keyboard.sys")))
                {
                    Logger.Log("EAC Bypass: Driver files are in use, waiting...");
                    Thread.Sleep(5000);
                }

                RenameDriverSafely("mouse.sys", "mouse_temp.sys");
                RenameDriverSafely("keyboard.sys", "keyboard_temp.sys");

                Logger.Log("EAC Bypass: Drivers successfully renamed for EAC bypass");
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass: Failed to rename drivers - {ex.Message}");
            }
        }

        private void RestoreDriverNames()
        {
            try
            {
                RestoreDriverSafely("mouse_temp.sys", "mouse.sys");
                RestoreDriverSafely("keyboard_temp.sys", "keyboard.sys");

                Logger.Log("EAC Bypass: Driver names successfully restored");
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass: Failed to restore drivers - {ex.Message}");
            }


        private void RenameDriverSafely(string originalName, string tempName)
        {
            string originalPath = Path.Combine(_driversPath, originalName);
            string tempPath = Path.Combine(_driversPath, tempName);

            try
            {
                if (File.Exists(originalPath) && !File.Exists(tempPath))
                {
                    if (IsFileInUse(originalPath))
                    {
                        Logger.Log($"EAC Bypass: {originalName} is in use, force unlocking...");

                        // Пытаемся принудительно освободить файл
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(1000);

                        if (IsFileInUse(originalPath))
                        {
                            Logger.Log($"EAC Bypass: Cannot rename {originalName} - file is locked");
                            return;
                        }
                    }

                    File.Move(originalPath, tempPath);
                    Logger.Log($"EAC Bypass: {originalName} -> {tempName}");
                }
                else if (!File.Exists(originalPath))
                {
                    Logger.Log($"EAC Bypass: {originalName} not found (already renamed?)");
                }
                else if (File.Exists(tempPath))
                {
                    Logger.Log($"EAC Bypass: {tempName} already exists");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass: Error renaming {originalName}: {ex.Message}");
            }
        }

        private void RestoreDriverSafely(string tempName, string originalName)
        {
            string tempPath = Path.Combine(_driversPath, tempName);
            string originalPath = Path.Combine(_driversPath, originalName);

            try
            {
                if (File.Exists(tempPath) && !File.Exists(originalPath))
                {
                    if (IsFileInUse(tempPath))
                    {
                        Logger.Log($"EAC Bypass: {tempName} is in use, force unlocking...");

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(1000);

                        if (IsFileInUse(tempPath))
                        {
                            Logger.Log($"EAC Bypass: Cannot restore {tempName} - file is locked");
                            return;
                        }
                    }

                    File.Move(tempPath, originalPath);
                    Logger.Log($"EAC Bypass: {tempName} -> {originalName}");
                }
                else if (!File.Exists(tempPath))
                {
                    Logger.Log($"EAC Bypass: {tempName} not found (already restored?)");
                }
                else if (File.Exists(originalPath))
                {
                    Logger.Log($"EAC Bypass: {originalName} already exists");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"EAC Bypass: Error restoring {tempName}: {ex.Message}");
            }
        }

        private bool IsFileInUse(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                MessageBox.Show(exception.Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved();
        }
    }
}