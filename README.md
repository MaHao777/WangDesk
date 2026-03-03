# WangDesk

一个运行在 Windows 系统托盘的桌宠工具。  
托盘图标是一只动态奔跑的泰迪小狗，奔跑速度会映射当前 CPU 使用率，并提供番茄钟与系统状态查看能力。

<p align="center">
  <img src="src/WangDesk.App/assets/app.png" alt="WangDesk 应用图标" width="160" />
</p>

## 开发目的

本项目让那些在外工作或者学习的用户可以实时在电脑桌面上看到自家宠物的可爱卡通形象（目前只有泰迪狗的形象），同时提供番茄钟和系统状态查询等功能来让该程序兼具一些实用功能。

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

## 演示图片

<p align="center">
  <img src="docs/image.png" alt="WangDesk 演示图片" width="150" />
</p>
