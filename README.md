# HyperLauncher

A sleek, custom-built Minecraft launcher for Windows with a dark monochrome UI, GitHub auto-updating, drag-and-drop custom builds, and playtime tracking.

---

## Features

- **Home Dashboard** — Personalised welcome screen with your username, a version selector, and at-a-glance build counts
- **GitHub Auto-Updater** — Automatically fetches and installs stable builds from a configured GitHub repository
- **Custom Builds via Drag & Drop** — Drop any `.zip` file onto the launcher window to install a custom build instantly
- **Versions Manager** — Browse all installed builds with folder paths, install dates, and tracked playtime
- **Settings Panel** — Configure your username, fullscreen mode, close-on-launch behaviour, and builds folder path
- **Frameless Window** — Custom titlebar with draggable window, minimise and close controls
- **Custom Version Builder** *(coming soon)* — Build and package your own Minecraft versions directly inside the launcher using a custom editor
- **Theme Editor** *(coming soon)* — Fully customise the launcher's colours, accents, and style with a built-in visual theme editor
- **Dark Monochrome Theme** — Fully custom WPF styles: scrollbars, list boxes, text boxes, and buttons all themed consistently

---

## Requirements

- Windows 10 or later
- .NET / WPF runtime (framework version depends on your project target)
- Internet connection (for GitHub auto-updater)

---

## Getting Started

1. Clone or download the repository
2. Open the solution in Visual Studio
3. Build and run the project
4. On first launch, set your username in **Settings → Profile Settings**
5. To install stable builds, use the **Versions** page and trigger the GitHub auto-updater
6. To install a custom build, drag and drop a `.zip` file anywhere onto the launcher window

---

## Project Structure

```
MinecraftLauncher/
├── MainWindow.xaml          # Main UI layout and all WPF styles
├── MainWindow.xaml.cs       # Code-behind (event handlers, logic)
├── PercentageToWidthConverter.cs        
├── AssemblyInfo.cs          # Assembly theme configuration
└── Resources/
    └── banner.jpg
    └── logo.png            
```

---

## UI Pages

| Page | Description |
|------|-------------|
| **Home** | Welcome banner, version picker, PLAY button, build count cards |
| **Versions** | GitHub auto-updater controls + full list of installed builds with Launch/Delete actions |
| **Settings** | Username, fullscreen toggle, close-after-launch toggle, builds folder path |

---

## Settings

| Setting | Description |
|---------|-------------|
| Username | In-game display name (max 16 characters), with optional persistence between sessions |
| Fullscreen mode | Launches games in fullscreen by default |
| Close after launch | Closes the launcher automatically when a game starts |
| Builds folder | Path to the directory where build `.zip` files are stored (default: `Builds/`) |

---

## Custom Builds

Drag and drop any `.zip` file onto the launcher to add a custom build. Custom builds appear in the Versions list alongside stable builds and support the same Launch/Delete actions.

---

## Upcoming Features

### 🛠️ Custom Version Builder
Build your own Minecraft versions directly from within the launcher. Configure version properties, select components, and package them into a deployable build — no external tools required.

### 🎨 Custom Theme Editor
Personalise the look of HyperLauncher with a built-in theme editor. Adjust colours, accents, backgrounds, and typography to create your own launcher aesthetic beyond the default dark monochrome style.

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you'd like to change.

---

*made with 💞*
