# WangDesk

一个运行在 Windows 系统托盘的桌宠工具。  
托盘图标是一只动态奔跑的泰迪小狗，奔跑速度会映射当前 CPU 使用率，并提供番茄钟与系统状态查看能力。

![WangDesk 应用图标](src/WangDesk.App/assets/app.png)

## 功能

- 托盘小狗动画速度随 CPU 使用率变化
- 左键点击托盘图标：打开番茄钟弹窗（专注/休息计时）
- 右键点击托盘图标：打开系统状态与设置弹窗（CPU、内存、磁盘、开机自启等）

## 运行环境

- Windows 10/11
- .NET 9 SDK

## 快速开始

```bash
dotnet restore
dotnet run --project src/WangDesk.App/WangDesk.App.csproj
```

## 构建

```bash
dotnet build src/WangDesk.App/WangDesk.App.csproj
```

## 发布

```bash
dotnet publish src/WangDesk.App/WangDesk.App.csproj -c Release -r win-x64 --self-contained false
```

## 配置文件

应用设置保存于：

`%AppData%\WangDesk\settings.json`
