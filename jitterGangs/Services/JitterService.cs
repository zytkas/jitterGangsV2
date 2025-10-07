using JitterGang.libs;
using JitterGang.Services;
using JitterGang.Services.Input;
using JitterGang.Services.Input.Controllers;
using JitterGang.Services.Jitter;
using JitterGang.Services.Timer;
using System.Diagnostics;
using System.Runtime.InteropServices;


public interface IJitterEffect
{
    void ApplyJitter(ref int deltaX, ref int deltaY);
}


public class JitterService : IJitterService, IDisposable
{

    private const int MIN_DELAY = 1;
    private readonly JitterTimer? _jitterTimer;
    private readonly IMouseDriverService _mouseDriverService;
    private readonly List<IJitterEffect> _jitterEffects = new();

    private bool _jitterEnabled;
    private bool _isJitterActivated;
    private bool _toggleKeyPressed;
    private bool _useMouseDriver;
    private int _toggleKey;
    private int _delay;
    private string? _selectedProcessName;
    private ControllerHandler? _controllerHandler;

    public JitterService(IMouseDriverService mouseDriverService)
    {
        _mouseDriverService = mouseDriverService;
        _jitterTimer = new JitterTimer(this);
        _delay = MIN_DELAY; // Default delay
        TryConnectToDriver();
        RebuildJitterPipeline();
    }

    #region Public Properties

    /// <inheritdoc />
    public int Strength { get; private set; }

    /// <inheritdoc />
    public int PullDownStrength { get; private set; }

    /// <inheritdoc />
    public bool UseController { get; private set; }

    /// <inheritdoc />
    public bool IsCircleJitterActive { get; set; }

    /// <inheritdoc />
    public bool UseAdsOnly { get; set; }

    /// <inheritdoc />
    public bool IsRunning => _jitterTimer?.IsRunning ?? false;

    #endregion

    #region Public Configuration Methods

    /// <inheritdoc />
    public void Start()
    {
        _jitterEnabled = true;
        _jitterTimer?.Start(TimeSpan.FromMilliseconds(_delay));
    }

    /// <inheritdoc />
    public void Stop()
    {
        _jitterEnabled = false;
        _isJitterActivated = false;
        _jitterTimer?.Stop();
    }

    /// <inheritdoc />
    public void SetToggleKey(int keyCode) => _toggleKey = keyCode;

    /// <inheritdoc />
    public void SetDelay(int delayMs) => _delay = Math.Max(1, delayMs);

    /// <inheritdoc />
    public void SetSelectedProcess(string processName) => _selectedProcessName = processName;

    /// <inheritdoc />
    public void UpdateStrength(int newStrength)
    {
        if (Strength == newStrength) return;
        Strength = newStrength;
        RebuildJitterPipeline();
    }

    /// <inheritdoc />
    public void UpdatePullDownStrength(int newPullDownStrength)
    {
        if (PullDownStrength == newPullDownStrength) return;
        PullDownStrength = newPullDownStrength;
        RebuildJitterPipeline();
    }

    public void UpdateJitters()
    {
        RebuildJitterPipeline();
    }

    /// <inheritdoc />
    public void SetUseController(bool use)
    {
        if (UseController == use) return;

        try
        {
            _controllerHandler?.Dispose();
            _controllerHandler = null;

            if (use)
            {
                if (!ControllerDetector.IsAnyControllerConnected())
                {
                    throw new InvalidOperationException("Controller not connected.");
                }
                _controllerHandler = (ControllerHandler)ControllerDetector.DetectController();
                _controllerHandler.StartPolling();
            }
            UseController = use;
            RebuildJitterPipeline();
        }
        catch
        {
            UseController = false;
            RebuildJitterPipeline();
            throw;
        }
    }

    #endregion

    public void HandleShakeTimerTick()
    {
        UpdateToggleState();

        if (!_isJitterActivated || !_jitterEnabled || !IsTargetProcessActive())
        {
            return;
        }

        try
        {
            if (ShouldApplyJitter())
            {
                ApplyJitter();
                Thread.Sleep(1);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during jitter tick: {ex.Message}");
            throw new InvalidOperationException("Error processing jitter tick.", ex);
        }
    }

    #region Private Helper Methods

    private void UpdateToggleState()
    {
        bool isToggleKeyDown = (NativeMethods.GetAsyncKeyState(_toggleKey) & 0x8000) != 0;
        if (isToggleKeyDown && !_toggleKeyPressed)
        {
            _isJitterActivated = !_isJitterActivated;
            _toggleKeyPressed = true;
        }
        else if (!isToggleKeyDown)
        {
            _toggleKeyPressed = false;
        }
    }

    private void RebuildJitterPipeline()
    {
        _jitterEffects.Clear();
        _jitterEffects.Add(new LeftRightJitter(Strength));
        if (UseController) _jitterEffects.Add(new SmoothLeftRightJitter(Strength));
        if (IsCircleJitterActive) _jitterEffects.Add(new CircleJitter(Strength));
        _jitterEffects.Add(new PullDownJitter(PullDownStrength));
    }


    private bool ShouldApplyJitter()
    {
        bool primaryAction;
        bool secondaryAction;

        if (UseController)
        {
            if (_controllerHandler == null)
            {
                throw new InvalidOperationException("Controller handler is not initialized.");
            }
            primaryAction = _controllerHandler.IsRightTriggerPressed;
            secondaryAction = _controllerHandler.IsLeftTriggerPressed;
        }
        else
        {
            primaryAction = (NativeMethods.GetAsyncKeyState(Win32Constants.VK_LBUTTON) & 0x8000) != 0;
            secondaryAction = (NativeMethods.GetAsyncKeyState(Win32Constants.VK_RBUTTON) & 0x8000) != 0;
        }

        return UseAdsOnly ? (primaryAction && secondaryAction) : primaryAction;
    }

    private void ApplyJitter()
    {
        for (int i = 0; i < 15; i++)
        {
            int totalDeltaX = 0;
            int totalDeltaY = 0;

            // Каждый раз заново применяем все эффекты
            foreach (var effect in _jitterEffects)
            {
                effect.ApplyJitter(ref totalDeltaX, ref totalDeltaY);
            }

            if (totalDeltaX != 0 || totalDeltaY != 0)
            {
                SendMouseInput(totalDeltaX, totalDeltaY);
            }
        }
    }

    private void SendMouseInput(int deltaX, int deltaY)
    {
        if (_useMouseDriver && _mouseDriverService.IsConnected)
        {
            if (!_mouseDriverService.SendMouseMovement(deltaX, deltaY))
            {
                _useMouseDriver = false;
                Logger.Log("Driver failed, falling back to SendInput");
            }
        }
        else
        {
                SendInputFallback(deltaX, deltaY);
        }
    }


    private void SendInputFallback(int deltaX, int deltaY)
    {
            var inputs = new INPUT[1];
            inputs[0].Type = Win32Constants.INPUT_MOUSE;
            inputs[0].Mi.Dx = deltaX;
            inputs[0].Mi.Dy = deltaY;
            inputs[0].Mi.DwFlags = Win32Constants.MOUSEEVENTF_MOVE;
            inputs[0].Mi.MouseData = 0;
            inputs[0].Mi.Time = 0;
            inputs[0].Mi.DwExtraInfo = IntPtr.Zero;
            uint result = NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result == 0)
            {
                Logger.Log($"SendInput failed with error: {Marshal.GetLastWin32Error()}");
            }
    }

    private bool IsTargetProcessActive()
    {
        if (string.IsNullOrEmpty(_selectedProcessName)) return false;

        IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
        if (NativeMethods.GetWindowThreadProcessId(foregroundWindow, out int foregroundProcessId) == 0)
        {
            return false;
        }

        var processes = Process.GetProcessesByName(_selectedProcessName);
        return processes.Any(p => p.Id == foregroundProcessId);
    }

    private void TryConnectToDriver()
    {
        try
        {
            _useMouseDriver = _mouseDriverService.Connect();
            Logger.Log(_useMouseDriver ? "Mouse driver connected - using kernel-level input" 
                : "Mouse driver not available - using standard SendInput");
        }
        catch (Exception ex)
        {
            _useMouseDriver = false;
            Logger.Log($"Error connecting to mouse driver: {ex.Message}");
        }
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        _jitterTimer?.Dispose();
        _controllerHandler?.Dispose();
        _mouseDriverService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
