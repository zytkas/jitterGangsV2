# ☕ µKM (JitterGang v2)

> *Because sometimes your aim just needs a little... help.*

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D7?style=for-the-badge&logo=windows&logoColor=white)

## 🎮 What is this?

JitterGang is my personal project that I built to learn about WPF, MVVM architecture, and Windows input handling. It's an input modification tool that applies customizable jitter patterns to your mouse or controller inputs, targeting specific applications.

**Is this a hack?** Not exactly - it's an educational project that shows how input simulation works in Windows. Think of it as "help in aiming"😉

## 💻 Tech Stack

- **C# / .NET 8.0** - Because modern problems bla bla bla
- **WPF** - For that sleek, dark-themed UI
- **MVVM Architecture** - Because separation of concerns isn't just for therapists
- **Win32 API** - To get down to the metal with Windows input handling (fixed tho, now driver warrior)
- **Dependency Injection** - Services that serve services, it's services all the way down!
- **XInput/DirectInput** - For when you prefer "right input" over poor mnk 
- **Firebase Database** - Cloud-based licensing and authentication system

## ✨ Features

- 🎯 **Process targeting** - Choose which application gets the jitters
- 💪 **Customizable intensity** - From subtle to "had way too much espresso"
- ⚪ **Circle jitter** - For smooth, circular mouse movements
- 🎮 **Controller support** - Xbox\Sony and other DirectInput controllers
- 🔫 **ADS-only mode** - Only active when aiming down sights 
- ⌨️ **Configurable hotkey** - Toggle with your favorite F-key

## 🖼️ Screenshots

Check out the [link](https://imgur.com/a/z0kKKun) to see the application!

## 📝 What I Learned

This project was my playground for:

- Building a **complete MVVM application** with proper separation of concerns
- Creating **high-precision timing** systems (harder than it sounds!)
- Managing **state across multiple threads** without losing my mind
- Working with **native Windows APIs** for input simulation
- Implementing **controller input detection** for multiple controller types
- Designing a **modern, responsive UI** using WPF-UI

## 🏗️ Architecture

```
JitterGang/
├── Models/              # Data structures
├── ViewModels/          # The brains of the operation
├── Services/            # Where the magic happens
│   ├── Input/           # Mouse and controller handling
│   ├── Jitter/          # Various jitter algorithms
│   └── Timer/           # High-precision timer implementation
└── Views/               # The pretty face of the app
```

## 🔐 Authentication System

*Note: The authentication code is intentionally excluded from this repo (check the .gitignore). If you're an employer looking at this, I'd be happy to discuss the Firebase implementation in an interview!*

## 🚀 Getting Started

1. Download the latest .exe from the [Releases](https://github.com/yourusername/jitterGangV2/releases) page
2. Run the application and follow the on-screen instructions
3. Use the default key: `DEMO-1234-5678` to try it out
4. Select your target process and adjust settings
5. Click "Start" and toggle the jitter with your configured hotkey (default: F1)

## 🧠 The Interesting Bits

If you're checking out my code, look at these cool parts:

- `HighPrecisionTimer.cs` - Thread-safe, high-resolution timer implementation
- `JitterTypes.cs` - Different algorithms for mouse movement patterns
- `ControllerDetector.cs` - Auto-detection of various controller types
- `MainViewModel.cs` - The orchestrator of all the madness

## ⚠️ Disclaimer

This was built as a learning exercise. Please use responsibly and be aware that using input modification tools may violate terms of service for some applications.

## 📧 Contact

Found this interesting? Isuess? Need key? Question?

[Discord](https://discord.gg/sSd5yjbnjC)

---

*Built with ☕ and late nights by zytka_*
