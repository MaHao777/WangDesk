# WangDesk

[English](README.md) | [简体中文](README.zh-CN.md)

一个带有番茄钟和快捷工具的 Windows 系统托盘桌宠应用。

WangDesk 是一个轻量级 Windows 效率工具，常驻在**系统托盘**中，并显示一个带动画效果的**桌面宠物**。它把**番茄钟**、设备状态查看和一些实用小工具结合在一起，适合专注工作场景。

<p align="center">
  <img src="src/WangDesk.App/assets/app.png" alt="WangDesk 应用图标" width="160" />
</p>

## 为什么选择 WangDesk

- 提供随手可用的番茄钟专注流程。
- 在托盘界面中快速查看 CPU、内存、磁盘等状态。
- 用更轻松的桌宠形态反馈当前设备活动。

## 功能特性

- 带动画效果的系统托盘桌宠。
- 用于专注和休息循环的番茄钟窗口。
- CPU、内存、磁盘占用的快速状态面板。
- 基于托盘的快捷工具和设置入口。
- 可选开机自启动。

## 项目说明

一个集桌面宠物、番茄钟、设备状态监控和快捷工具于一体的 Windows 托盘应用。

## 目标平台

- Windows 10 / Windows 11

## 开发环境要求

- .NET 9 SDK

## 快速开始

### 普通 Windows 用户（推荐）

1. 使用 `Build-Setup.cmd` 生成的 `setup.exe`，或者直接下载 release 中提供的安装包。
2. 双击 `setup.exe`。
3. 按安装向导完成安装。
4. 安装完成后，从开始菜单或桌面快捷方式启动 WangDesk。
5. 这个安装包不需要额外安装 .NET。

### 便携版

1. 使用 `Package-WangDesk.cmd` 生成的 `WangDesk-win-x64.zip`，或者直接下载 release 中提供的同名压缩包。
2. 解压压缩包到任意目录。
3. 双击 `WangDesk.App.exe`。
4. 如果没有看到主窗口，请到右下角时钟附近的系统托盘查看 WangDesk 图标。

### 从源码直接启动

1. 在项目根目录双击 `Start-WangDesk.cmd`。
2. 如果 WangDesk 已经构建完成，会直接启动。
3. 第一次运行时，如果本机已安装 `.NET 9 SDK`，脚本会自动完成构建。

### 开发者

```bash
dotnet restore
dotnet run --project src/WangDesk.App/WangDesk.App.csproj
```

## 构建

```bash
dotnet build src/WangDesk.App/WangDesk.App.csproj
```

## 发布

双击 `Build-Setup.cmd` 即可生成可直接分发给普通用户的 Windows 安装包。

安装包输出：

- `artifacts\installers\WangDesk-Setup.exe`
- `artifacts\installers\setup.exe`

双击 `Package-WangDesk.cmd` 即可生成便携式自包含版本。

输出文件：

- `artifacts\packages\WangDesk-win-x64\`
- `artifacts\packages\WangDesk-win-x64.zip`

## 配置

设置文件位置：

`%AppData%\\WangDesk\\settings.json`

## 截图

<p align="center">
  <img src="docs/image.png" alt="WangDesk 截图" width="150" />
</p>

## 关键词

`windows 桌宠`, `系统托盘应用`, `番茄钟`, `Windows 效率工具`, `托盘工具`, `.NET 9`, `专注计时`
