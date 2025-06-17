namespace JitterGang.Services.Input;

public interface IInputInterceptorService : IDisposable
{
    bool IsDriverInstalled { get; }
    bool IsInitialized { get; }
    bool CanSimulateInput { get; }
    Task<bool> InitializeAsync();
    Task<bool> InstallDriverAsync();
    Task<bool> UninstallDriverAsync();
    bool MoveCursorBy(int deltaX, int deltaY);
    bool IsLeftMouseButtonPressed { get; }
    bool IsRightMouseButtonPressed { get; }
}