# μKM (JitterGang v2)

A Windows desktop application for input automation, built as a study in WPF, MVVM, and low-level Windows input handling. The application applies configurable jitter patterns to mouse or controller input, scoped to a single target process, with both a user-mode (`SendInput`) and a kernel-mode driver path.

![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat-square&logo=.net&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D7?style=flat-square&logo=windows&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=flat-square&logo=firebase&logoColor=black)

---

## Overview

JitterGang demonstrates a complete WPF application architecture with dependency injection, observable view models, persisted settings, hardware-bound licensing, and a hybrid input pipeline that gracefully degrades from kernel-level injection to standard Win32 calls. It was developed as a personal project to explore the practical boundaries between managed code and native Windows internals.

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | C# / .NET 8.0 |
| UI framework | WPF with [WPF-UI](https://github.com/lepoco/wpfui) (Fluent design) |
| Architecture | MVVM via `CommunityToolkit.Mvvm`; dependency injection via `Microsoft.Extensions.DependencyInjection` |
| User-mode input | Win32 `SendInput` through P/Invoke |
| Kernel-mode input | Custom signed-driver loader (kdmapper) with `DeviceIoControl` IOCTL communication |
| Controller input | XInput and DirectInput via SharpDX |
| Authentication | Firebase Realtime Database with HWID-bound license keys |
| Local storage | DPAPI-encrypted license file; JSON-serialized settings |

## Features

- **Process-scoped activation.** Jitter is applied only while a user-selected window holds foreground focus.
- **Independent jitter axes.** Separate strength controls for horizontal jitter and vertical pull-down compensation.
- **Multiple jitter algorithms.** Linear, smoothed (with acceleration ramp), and circular patterns, composable at runtime.
- **Controller support.** Auto-detection of XInput and DirectInput devices, with reconnect handling on disconnect.
- **ADS-only mode.** Activation gated on simultaneous primary and secondary input.
- **Configurable hotkey.** Toggle binding to F1–F12, Shift, Capslock, or mouse X1/X2.
- **Driver with transparent fallback.** Kernel-level mouse driver when available, automatic fallback to `SendInput` when the driver fails to load or signal.

## Architecture

```
JitterGang/
├── Models/                       Observable settings model, persisted to JSON
├── ViewModels/                   MainViewModel, LoginViewModel, LicenseManagerViewModel
├── Services/
│   ├── Input/
│   │   ├── Controllers/          XInput and DirectInput handlers with auto-detection
│   │   └── MouseDriverService    IOCTL wrapper for the kernel driver
│   ├── Jitter/                   Pluggable IJitterEffect implementations
│   ├── Timer/                    Stopwatch-based high-precision timer
│   ├── SettingsService           JSON persistence layer
│   ├── FirebaseService           License verification and admin operations
│   ├── DriverLoaderService       kdmapper invocation and driver presence checks
│   └── DependencyContainer       Service registration and resolution
└── Views/                        MainWindow, LoginWindow, LicenseManagerWindow
```

### Input pipeline

1. `HighPrecisionTimer` ticks at the user-configured interval (1–100 ms) using a `Stopwatch`-driven async loop for sub-millisecond accuracy.
2. On each tick, `JitterService` evaluates the toggle key state, target process foreground status, and trigger state (mouse buttons or controller triggers).
3. When activation conditions are met, all registered `IJitterEffect` implementations contribute to a final `(deltaX, deltaY)` vector.
4. The resulting movement is dispatched through the kernel driver via `DeviceIoControl` when available; otherwise it falls back to `SendInput`. A driver failure during runtime disables the kernel path for the remainder of the session and logs the transition.

## Authentication and Licensing

Licensing is handled through Firebase Realtime Database. Each license key is bound to a hardware fingerprint derived from the processor ID, motherboard serial, BIOS serial, and OS volume serial, hashed with SHA-256. The local license file is encrypted with Windows DPAPI (`ProtectedData.Protect`) and scoped to the current user.

The Firebase service implementation and encrypted configuration constants are excluded from this repository (see `.gitignore`).

## Notable Implementation Details

- [`Services/Timer/HighPrecisionTimer.cs`](jitterGangs/Services/Timer/HighPrecisionTimer.cs) — Cooperative high-resolution timer that yields between ticks rather than busy-waiting.
- [`Services/JitterService.cs`](jitterGangs/Services/JitterService.cs) — Main orchestrator coordinating the timer, input pipeline, and driver/SendInput dual path.
- [`Services/Input/MouseDriverService.cs`](jitterGangs/Services/Input/MouseDriverService.cs) — Thread-safe IOCTL wrapper around the kernel driver, using `SafeFileHandle` for resource management.
- [`Services/DriverLoaderService.cs`](jitterGangs/Services/DriverLoaderService.cs) — kdmapper-based driver loader with presence detection.
- [`Services/Jitter/JitterTypes.cs`](jitterGangs/Services/Jitter/JitterTypes.cs) — Composable jitter algorithms behind a single `IJitterEffect` abstraction.
- [`Services/Input/Controllers/ControllerDetector.cs`](jitterGangs/Services/Input/Controllers/ControllerDetector.cs) — Unified detection layer over XInput and DirectInput.

## Getting Started

1. Download the latest build from the [Releases](https://github.com/yourusername/jitterGangV2/releases) page.
2. Run the executable with administrator privileges (required for driver loading).
3. Activate the application with a valid license key.
4. Select a target process, configure strength and delay, and set the toggle hotkey.
5. Press **Start** and toggle activation in the target application using the configured key (default: F1).

## Disclaimer

This project was developed as a learning exercise. Using input-modification tools against online services may violate their Terms of Service and result in account penalties. Use it against offline targets, test environments, or software you own.

## Contact

[Discord](https://discord.gg/sSd5yjbnjC)

---

Developed by zytka_
