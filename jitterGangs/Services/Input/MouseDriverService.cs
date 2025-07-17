using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace JitterGang.Services.Input
{
    public interface IMouseDriverService : IDisposable
    {
        bool IsConnected { get; }
        bool Connect();
        void Disconnect();
        bool SendMouseMovement(int deltaX, int deltaY);
    }

    public class MouseDriverService : IMouseDriverService
    {
        // Windows API constants
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_DEVICE_UNKNOWN = 0x00000022;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_SPECIAL_ACCESS = 0;

        // IOCTL definition
        private static readonly uint MOUSE_REQUEST = CTL_CODE(FILE_DEVICE_UNKNOWN, 0x666, METHOD_BUFFERED, FILE_SPECIAL_ACCESS);

        private SafeFileHandle? _driverHandle;
        private readonly object _lock = new object();

        public bool IsConnected => _driverHandle != null && !_driverHandle.IsInvalid && !_driverHandle.IsClosed;

        // Mouse request structure - matches C++ layout
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct KMOUSE_REQUEST
        {
            public int x;
            public int y;
            public byte button_flags;
        }

        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            ref KMOUSE_REQUEST lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method);
        }

        public bool Connect()
        {
            lock (_lock)
            {
                if (IsConnected)
                    return true;

                try
                {
                    _driverHandle = CreateFile(
                        @"\\.\mousekm",
                        GENERIC_READ | GENERIC_WRITE,
                        0,
                        IntPtr.Zero,
                        OPEN_EXISTING,
                        0,
                        IntPtr.Zero);

                    if (_driverHandle.IsInvalid)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Log($"Failed to connect to mouse driver. Error: {error}");
                        return false;
                    }

                    Logger.Log("Successfully connected to mouse driver");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception connecting to mouse driver: {ex.Message}");
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_driverHandle != null && !_driverHandle.IsInvalid)
                {
                    _driverHandle.Close();
                    _driverHandle = null;
                    Logger.Log("Disconnected from mouse driver");
                }
            }
        }

        public bool SendMouseMovement(int deltaX, int deltaY)
        {
            lock (_lock)
            {
                if (!IsConnected)
                {
                    Logger.Log("Mouse driver not connected");
                    return false;
                }

                try
                {
                    var request = new KMOUSE_REQUEST
                    {
                        x = deltaX,
                        y = deltaY,
                        button_flags = 0
                    };

                    uint bytesReturned = 0;
                    bool result = DeviceIoControl(
                        _driverHandle!,
                        MOUSE_REQUEST,
                        ref request,
                        (uint)Marshal.SizeOf(typeof(KMOUSE_REQUEST)),
                        IntPtr.Zero,
                        0,
                        out bytesReturned,
                        IntPtr.Zero);

                    if (!result)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Log($"DeviceIoControl failed. Error: {error}");
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception sending mouse movement: {ex.Message}");
                    return false;
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ~MouseDriverService()
        {
            Dispose();
        }
    }
}