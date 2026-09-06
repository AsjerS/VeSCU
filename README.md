<div align="center">
  <img src="assets/logo.svg" alt="VeSCU Logo" width="112" height="112" />

  ## VeSCU - Versatile Screen Capture Utility

  [![Release](https://img.shields.io/github/v/release/AsjerS/VeSCU?style=flat-square)](https://github.com/AsjerS/VeSCU/releases)
  [![License](https://img.shields.io/github/license/AsjerS/VeSCU?style=flat-square)](LICENSE)
  [![Platform](https://img.shields.io/badge/platform-Windows%2011-0078D4?style=flat-square)](https://github.com/AsjerS/VeSCU/releases)
  [![Runtime](https://img.shields.io/badge/runtime-.NET%2010.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download/dotnet/10.0)
</div>

## Installation

Download a release from [here](https://github.com/AsjerS/VeSCU/releases/latest) and install it.

Choose `x64` if you have an Intel/AMD CPU, and `arm64` if you have a Snapdragon CPU. If you're unsure choose `x64`.

The program requires .NET 10 to run

## Features

- Captures your screen, converts the pixels to a format encoders can understand with custom built functions, and hands it to an encoder of your choice.
- Made to be undisruptive: taking a screenshot does not shift focus, and works with lower CPU priority than your apps and games.
- Out-of-the-box support for JPEG XL, AVIF, WebP, PNG and JPEG.
  - And with a simple configuration edit, it can stream pixels to any encoder executable you have that supports stdin.
- Doesn't perform any unnecessary disk writes.
- Only connects to the internet when you prompt it to.

## Configuration

```toml
[Hotkey]
# the final hotkey results in: Ctrl+Alt+PrintScreen
Key = "PrintScreen"
Ctrl = true
Alt = true
Shift = false
Win = false

[Saving]
# directory to which screenshots should be saved
# this default means C:\Users\YourName\Pictures\Screenshots
Directory = "%USERPROFILE%\\Pictures\\Screenshots"
# the extension to append to screenshot files
Extension = ".jxl"

[Encoder]
# the path to the program used to encode the final image
Path = "cjxl.exe"
# the arguments passed to the encoder, with {Output} being parsed to the final screenshot location
Arguments = "-d 1 - {Output}"
# the format in which pixels should be delivered to the encoder
# valid values are: 'Ppm', 'Png'
InputFormat = "Ppm"
```

## How it works

1. When the program starts up, it checks if the configuration file is valid (not missing keys), and if it isn't, it auto repairs.
2. If the configuration is unusable (unwritable paths or missing encoders), it gives an error and prompt to either close the app or to open the config file to fix it. In the case of a missing encoder it first prompts to auto download the encoder.
3. It binds the hotkey in Windows and makes the tray icon.
4. When the hotkey is pressed or a screen capture is manually initiated, it reads all pixels from the primary stream as a bitmap, hands that bitmap to an intermediary encoder to convert it to a format like PPM/PNG, and streams that into the stdin of the encoder set in the config.

## Contributing

If you want to add a big feature, please make an issue first to discuss if it is in the scope of this project.
