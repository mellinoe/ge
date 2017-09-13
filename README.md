# Game Engine

This repository contains the source code and base assets for the game engine and editor used to build [Crazy Core](https://github.com/mellinoe/CrazyCore), a simple, open source 3D game. The game engine and editor are both supported on Windows, Linux, and macOS, and are built with .NET Core.

See the [Crazy Core](https://github.com/mellinoe/CrazyCore) repository for a more complete view of how the engine can be used.

## Features

### Graphics
* Full 3D rendering system, with support for Direct3D 11 and OpenGL backends
* Forward-rendered pipeline with support for real-time directional shadows, particle systems, transparent objects, and a simple immediate-mode GUI.
* Uses [Veldrid](https://github.com/mellinoe/veldrid) (v1) and [ImGui.NET](https://github.com/mellinoe/ImGui.NET)

### Physics
* Real-time 3D physics via BEPU Physics
* Various configurable physics shapes
* Customizable 

### Audio
* Positional audio
* Supports XAudio2 and OpenAL backends

### Editor
* Cross-platform editor application
* 3D scene view with editing widgets
* Asset management and serialization
* Live component value editing

### Misc
* Unity Engine-like component system
* Plugin system
* Builds self-contained executable packages for publication per-platform
