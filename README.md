# ☕ μKM (jitterGang v2)

> *A WPF playground for learning Windows input internals, kernel drivers, and MVVM.*

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D7?style=for-the-badge&logo=windows&logoColor=white)
![Firebase](https://img.shields.io/badge/Firebase-FFCA28?style=for-the-badge&logo=firebase&logoColor=black)

## 🎮 What is this?

JitterGang is a personal project I built to dive deep into WPF, MVVM, and the dark corners of the Windows input stack. It applies customizable jitter patterns to mouse or controller input, scoped to a single target process.

**Is it a cheat?** It's an educational exploration of how input simulation works on Windows, with both a user-mode (`SendInput`) path and a kernel-mode driver path. Treat it as a learning artifact, not a tool for ruining other people's games.

## 💻 Tech Stack

| Layer | Tech |
|---|---|
| Runtime | C# / .NET 8.0 |
| UI | WPF + [WPF-UI](https://github.com/lepoco/wpfui) (Fluent dark theme) |
| Architecture | MVVM via `CommunityToolkit.Mvvm` + DI (`Microsoft.Extensions.DependencyInjection`) |
| Input (user-mode) | Win32 `SendInput` via P/Invoke |
| Input (kernel-mode) | Custom signed-driver loader (kdmapper) + `DeviceIoControl` IOCTL |
| Controllers | XInput + DirectInput via SharpDX |
| Auth & licensing | Firebase Realtime Database with HWID-bound keys |
| Local storage | DPAPI-encrypted license file, JSON settings |

## ✨ Features

- 🎯 **Process targeting** — jitter only activates while a chosen window is in the foreground
- 💪 **Customizable intensity** — independent strength for horizontal jitter and vertical pull-down
- ⚪ **Circle jitter mode** — circular movement pattern with configurable radius
- 🎮 **Controller support** — Xbox / PlayStation / generic DirectInput pads with hot-swap reconnect
- 🔫 **ADS-only mode** — only fires when both primary and secondary actions are held
- ⌨️ **Configurable hotkey** — toggle with F1–F12, Shift, Capslock, or mouse X1/X2
- 🛡️ **Driver with fallback** — kernel-level mouse driver when available, transparent fallback to `SendInput` when it isn't

## 🖼️ Screenshots

[See the app in action →](https://imgur.com/a/z0kKKun)

## 🏗️ Architecture

```
JitterGang/
├── Models/                    # JitterSettings (observable, persisted to JSON)
├── ViewModels/                # MainViewModel, LoginViewModel, LicenseManagerViewModel
├── Services/
│   ├── Input/
│   │   ├── Controllers/       # XInput + DirectInput handlers, auto-detection
│   │   └── MouseDriverService # IOCTL communication with kernel driver
│   ├── Jitter/                # LeftRight, Circle, Smooth, PullDown effects
│   ├── Timer/                 # HighPrecisionTimer + JitterTimer wrapper
│   ├── SettingsService        # JSON persistence
│   ├── FirebaseService        # License verification + admin operations
│   ├── DriverLoaderService    # kdmapper invocation, driver presence check
│   └── DependencyContainer    # Service registration & resolution
└── Views/                     # MainWindow, LoginWindow, LicenseManagerWindow
```

### How the input pipeline works

1. `HighPrecisionTimer` ticks at the user-configured interval (1–100 ms) using a `Stopwatch`-driven loop for sub-millisecond accuracy.
2. On each tick, `JitterService` checks the toggle key, the target process, and the trigger state (mouse buttons or controller triggers).
3. If active, registered `IJitterEffect` implementations (`LeftRightJitter`, `CircleJitter`, `SmoothLeftRightJitter`, `PullDownJitter`) compose a final `(deltaX, deltaY)`.
4. The delta is sent through the kernel driver via `DeviceIoControl` when available; otherwise it falls back to `SendInput`. Failure of the driver path automatically disables it for the rest of the session.

## 🔐 Authentication

Licensing is handled through Firebase Realtime Database. Each license key is bound to a hardware ID derived from CPU, motherboard, BIOS, and OS volume serials (SHA-256 hashed). The license file is encrypted with Windows DPAPI (`ProtectedData.Protect`) scoped to the current user.

> The Firebase service files and the encrypted constants are excluded from this repo (see `.gitignore`). Happy to walk through the implementation in an interview.

## 📝 What I learned

- Building a **fully MVVM-structured WPF app** with constructor-injected services
- Implementing a **sub-millisecond timer** without burning a CPU core
- Communicating with a **kernel-mode driver** from managed code via `SafeFileHandle` + IOCTL
- Designing a system with **graceful degradation** — the app stays functional whether or not the driver loads
- Coordinating **multi-threaded input polling** for XInput and DirectInput devices with reconnect handling
- Encrypting configuration secrets and binding licenses to hardware fingerprints

## 🧠 Files worth a look

- [`Services/Timer/HighPrecisionTimer.cs`](jitterGangs/Services/Timer/HighPrecisionTimer.cs) — `Stopwatch`-based async timer
- [`Services/JitterService.cs`](jitterGangs/Services/JitterService.cs) — main orchestrator with driver/SendInput dual path
- [`Services/Input/MouseDriverService.cs`](jitterGangs/Services/Input/MouseDriverService.cs) — kernel driver IOCTL wrapper
- [`Services/DriverLoaderService.cs`](jitterGangs/Services/DriverLoaderService.cs) — kdmapper-based driver loading
- [`Services/Jitter/JitterTypes.cs`](jitterGangs/Services/Jitter/JitterTypes.cs) — pluggable jitter algorithms
- [`Services/Input/Controllers/ControllerDetector.cs`](jitterGangs/Services/Input/Controllers/ControllerDetector.cs) — XInput/DirectInput auto-detection

## 🚀 Getting Started

1. Grab the latest build from [Releases](https://github.com/yourusername/jitterGangV2/releases).
2. Run the executable as administrator (required for driver loading).
3. Activate with a license key (ping me on Discord for one).
4. Pick a target process, dial in strength/delay, set your toggle hotkey.
5. Hit **Start** and toggle in-game with your chosen key (default: F1).

## ⚠️ Disclaimer

This is a learning project. Using input-modification tools against online services may violate their Terms of Service and get your account banned. Use it on offline targets, test apps, or your own software.

## 📧 Contact

Questions, issues, or just want to chat about WPF / kernel drivers?

[Discord](https://discord.gg/sSd5yjbnjC)

---

*Built with ☕ and questionable sleep schedules by zytka_*
