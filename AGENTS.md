# AGENTS.md

## Project Overview

Unity VR experiment project (C#) for neuroscience research. Plays scheduled audio stimuli, collects VR controller responses, and logs trial data, Varjo eye tracking, and EEG via Lab Streaming Layer (LSL) as CSV files.

- **Engine**: Unity 2021.3.27f1 (HDRP)
- **Language**: C# (557 .cs files across Assets/)
- **Target Platform**: Windows Standalone (Varjo VR headset required for full operation)

## Key Paths

| Path | Description |
|---|---|
| `Assets/Scripts/Experiment/` | Core experiment logic (9 scripts) |
| `Assets/*.cs` | Top-level helper scripts (eye tracking, LSL, 2D tracking) |
| `Assets/Plugins/LeapMotion/` | Bundled Leap Motion SDK |
| `Packages/manifest.json` | Unity Package Manager dependencies |
| `ProjectSettings/` | Unity project configuration |
| `ExperimentOutputs/` | Generated session CSV data |

## Cursor Cloud specific instructions

### Platform Limitation

This is a **Windows-only Unity VR project**. The Unity Editor, Varjo SDK, and OpenXR runtime are not available on the Linux cloud VM. Full build/run/play testing requires a Windows machine with Unity 2021.3.27f1 and a Varjo VR headset.

### What CAN be done on the cloud VM

- **C# syntax checking**: .NET 6 SDK is installed at `$HOME/.dotnet`. Run syntax analysis via:
  ```
  export PATH="$HOME/.dotnet:$PATH"
  cd /workspace/.dev-tools/SyntaxChecker && dotnet run -- /workspace/Assets
  ```
- **Code editing**: All C# source files can be edited directly. The core experiment scripts are in `Assets/Scripts/Experiment/`.
- **Package manifest editing**: `Packages/manifest.json` can be updated. The broken machine-specific `file:` paths have been fixed to use local relative paths.
- **Project structure verification**: All Unity scenes, settings, and assets can be inspected.

### What CANNOT be done on the cloud VM

- Unity Editor operations (opening project, Play mode, building)
- Running automated Unity tests (requires Unity Test Runner)
- VR hardware testing (Varjo headset, eye tracking, OpenXR)
- Visual scene inspection or HDRP rendering

### Package Dependencies (gotcha)

The `Packages/manifest.json` originally contained broken Windows-specific `file:` paths for three packages. These have been fixed to use local relative paths pointing to `.tgz` files in the `Packages/` directory:
- `com.threespacelab.imotions.varjointegration` → `file:com.threespacelab.imotions.varjointegration-2.2.0.tgz`
- `com.threespacelab.imotions.core` → `file:com.threespacelab.imotions.core-1.0.4.tgz`
- `com.varjo.xr` → `file:com.varjo.xr-3.5.0.tgz`

The nested `.tgz` files (`com.threespacelab.imotions.core-1.0.4.tgz` and `com.varjo.xr-3.5.0.tgz`) were extracted from the main `com.threespacelab.imotions.varjointegration-2.2.0.tgz` and placed in `Packages/`.

### Environment Setup

- .NET 6 SDK: installed to `$HOME/.dotnet` (add to PATH with `export PATH="$HOME/.dotnet:$PATH"`)
- Syntax checker tool: `/workspace/.dev-tools/SyntaxChecker/` (Roslyn-based C# syntax analysis)
- No additional services, databases, or Docker containers needed
- See `README.md` for full project documentation (experiment configuration, data output format, etc.)
