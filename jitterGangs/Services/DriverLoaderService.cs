using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace jitterGangs.Services
{
    public interface IDriverLoaderService
    {
        Task<bool> EnsureDriverLoadedAsync();
        bool IsDriverLoaded();
    }

    public class DriverLoaderService : IDriverLoaderService
    {
        private const string DRIVER_DEVICE_NAME = @"\\.\mousekm";
        private const string KDMAPPER_NAME = "mapper.exe";
        private const string DRIVER_NAME = "druver.sys";
        private const string RESOURCES_FOLDER = "Resources";

        public async Task<bool> EnsureDriverLoadedAsync()
        {
            try
            {
                if (IsDriverLoaded())
                {
                    Logger.Log("Driver is already loaded");
                    return true;
                }

                Logger.Log("Driver not loaded, attempting to load...");

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string resourcesPath = Path.Combine(basePath, RESOURCES_FOLDER);
                string kdmapperPath = Path.Combine(resourcesPath, KDMAPPER_NAME);
                string driverPath = Path.Combine(resourcesPath, DRIVER_NAME);

                if (!File.Exists(kdmapperPath))
                {
                    Logger.Log($"kdmapper not found at: {kdmapperPath}");
                    return false;
                }

                if (!File.Exists(driverPath))
                {
                    Logger.Log($"Driver not found at: {driverPath}");
                    return false;
                }

                return await LoadDriverWithKdmapper(kdmapperPath, driverPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading driver: {ex.Message}");
                return false;
            }
        }

        public bool IsDriverLoaded()
        {
            try
            {
                using (var handle = CreateFile(
                    DRIVER_DEVICE_NAME,
                    GENERIC_READ,
                    0,
                    nint.Zero,
                    OPEN_EXISTING,
                    0,
                    nint.Zero))
                {
                    return handle != null && !handle.IsInvalid;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> LoadDriverWithKdmapper(string kdmapperPath, string driverPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = kdmapperPath,
                    Arguments = $"\"{driverPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" 
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        Logger.Log("Failed to start kdmapper process");
                        return false;
                    }

                    // Read output for logging
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    Logger.Log($"kdmapper output: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger.Log($"kdmapper error: {error}");
                    }

                    await Task.Delay(1000); 

                    bool loaded = IsDriverLoaded();
                    Logger.Log(loaded ? "Driver loaded successfully" : "Driver failed to load");
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error running kdmapper: {ex.Message}");
                return false;
            }
        }

        // P/Invoke declarations
        private const uint GENERIC_READ = 0x80000000;
        private const uint OPEN_EXISTING = 3;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            nint lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            nint hTemplateFile);
    }
}