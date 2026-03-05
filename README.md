# WangDesk

A Windows system tray desktop pet with Pomodoro timer and quick tools.

WangDesk is a lightweight Windows productivity app that lives in the **system tray** and shows an animated **desktop pet**. It combines a **Pomodoro timer**, quick device status checks, and practical utilities for focused work sessions.

<p align="center">
  <img src="src/WangDesk.App/assets/app.png" alt="WangDesk app icon" width="160" />
</p>

## Why WangDesk

- Keep focus with an always-ready Pomodoro workflow.
- Monitor key system metrics (CPU, memory, disk) from tray UI.
- Get a playful desktop companion that reflects current device activity.

## Features

- Animated desktop pet in the Windows system tray.
- Pomodoro timer window for focus and break sessions.
- Quick system status panel for CPU, memory, and disk usage.
- Tray-based quick tools and settings.
- Optional auto-start on Windows boot.

## Project Description

Windows system tray desktop pet with Pomodoro timer, device status and quick tools.

## Target Platform

- Windows 10 / Windows 11
- .NET 9 SDK

## Quick Start

```bash
dotnet restore
dotnet run --project src/WangDesk.App/WangDesk.App.csproj
```

## Build

```bash
dotnet build src/WangDesk.App/WangDesk.App.csproj
```

## Publish

```bash
dotnet publish src/WangDesk.App/WangDesk.App.csproj -c Release -r win-x64 --self-contained false
```

## Configuration

Settings file location:

`%AppData%\\WangDesk\\settings.json`

## Screenshots

<p align="center">
  <img src="docs/image.png" alt="WangDesk screenshot" width="150" />
</p>

## SEO Keywords

`windows desktop pet`, `system tray app`, `pomodoro timer`, `windows productivity`, `tray utility`, `.NET 9`, `focus timer`
