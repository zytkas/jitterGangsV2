using InputInterceptorNS;

namespace JitterGang.Services.Input;

public class InputInterceptorService : IInputInterceptorService
{
    private MouseHook? _mouseHook;
    private bool _isInitialized;
    private bool _leftMousePressed;
    private bool _rightMousePressed;
    private readonly object _lockObject = new();

    public bool IsDriverInstalled => InputInterceptor.CheckDriverInstalled();
    public bool IsInitialized => _isInitialized && _mouseHook?.IsInitialized == true;
    public bool CanSimulateInput => _mouseHook?.CanSimulateInput == true;
    public bool IsLeftMouseButtonPressed => _leftMousePressed;
    public bool IsRightMouseButtonPressed => _rightMousePressed;

    public async Task<bool> InitializeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                lock (_lockObject)
                {
                    Logger.Log("=== InputInterceptor Initialization Start ===");

                    if (_isInitialized)
                    {
                        Logger.Log("Already initialized, returning true");
                        return true;
                    }

                    if (!IsDriverInstalled)
                    {
                        Logger.Log("InputInterceptor driver is not installed");
                        return false;
                    }

                    Logger.Log("Driver is installed, calling InputInterceptor.Initialize()...");
                    if (!InputInterceptor.Initialize())
                    {
                        Logger.Log("Failed to initialize InputInterceptor");
                        return false;
                    }

                    Logger.Log("Creating MouseHook...");
                    _mouseHook = new MouseHook(MouseCallback);
                    Thread.Sleep(100);

                    _isInitialized = _mouseHook.IsInitialized;
                    Logger.Log($"MouseHook.IsInitialized: {_mouseHook.IsInitialized}");
                    Logger.Log($"MouseHook.CanSimulateInput: {_mouseHook.CanSimulateInput}");

                    if (_isInitialized)
                    {
                        Logger.Log("InputInterceptor successfully initialized");
                    }
                    else
                    {
                        Logger.Log("InputInterceptor initialization failed - MouseHook not initialized");
                        _mouseHook?.Dispose();
                        _mouseHook = null;
                    }

                    Logger.Log($"=== InputInterceptor Initialization End === Result: {_isInitialized}");
                    return _isInitialized;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"InputInterceptor initialization error: {ex.Message}");
                return false;
            }
        });
    }

    public async Task<bool> InstallDriverAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!InputInterceptor.CheckAdministratorRights())
                {
                    Logger.Log("Administrator rights required for driver installation");
                    return false;
                }

                Logger.Log("Installing InputInterceptor driver...");
                bool result = InputInterceptor.InstallDriver();

                if (result)
                {
                    Logger.Log("InputInterceptor driver installed successfully. Restart required.");
                }
                else
                {
                    Logger.Log("Failed to install InputInterceptor driver");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"Driver installation error: {ex.Message}");
                return false;
            }
        });
    }

    public async Task<bool> UninstallDriverAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!InputInterceptor.CheckAdministratorRights())
                {
                    Logger.Log("Administrator rights required for driver uninstallation");
                    return false;
                }

                Logger.Log("Uninstalling InputInterceptor driver...");
                bool result = InputInterceptor.UninstallDriver();

                if (result)
                {
                    Logger.Log("InputInterceptor driver uninstalled successfully");
                }
                else
                {
                    Logger.Log("Failed to uninstall InputInterceptor driver");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"Driver uninstallation error: {ex.Message}");
                return false;
            }
        });
    }

    public bool MoveCursorBy(int deltaX, int deltaY)
    {
        try
        {
            lock (_lockObject)
            {
                if (!CanSimulateInput)
                {
                    return false;
                }

                // Используем MoveCursorBy для относительного движения мыши
                return _mouseHook!.MoveCursorBy(deltaX, deltaY, useWinAPI: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error moving cursor: {ex.Message}");
            return false;
        }
    }

    private void MouseCallback(ref MouseStroke mouseStroke)
    {
        try
        {
            switch (mouseStroke.State)
            {
                case MouseState.LeftButtonDown:
                    _leftMousePressed = true;
                    break;
                case MouseState.LeftButtonUp:
                    _leftMousePressed = false;
                    break;
                case MouseState.RightButtonDown:
                    _rightMousePressed = true;
                    break;
                case MouseState.RightButtonUp:
                    _rightMousePressed = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Mouse callback error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            lock (_lockObject)
            {
                Logger.Log("=== InputInterceptor Service Disposal Start ===");

                if (_mouseHook != null)
                {
                    Logger.Log("Disposing MouseHook...");
                    _mouseHook.Dispose();
                    _mouseHook = null;
                }

                _isInitialized = false;

                // Принудительная очистка ресурсов
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Дополнительная задержка для освобождения kernel-ресурсов
                Thread.Sleep(1000);

                Logger.Log("=== InputInterceptor Service Disposal Complete ===");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error disposing InputInterceptor service: {ex.Message}");
        }
    }
}