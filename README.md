# Jellyfin.Plugin.Jellysweep

<img src="logo.png" alt="Jellysweep Plugin Logo" width="20%">

## ✨ Features

Adds an indicator to media items in Jellyfin if they are marked for deletion by Jellysweep.

## 📋 Table of Contents

- [Jellyfin.Plugin.Jellysweep](#jellyfinpluginjellysweep)
  - [✨ Features](#-features)
  - [📋 Table of Contents](#-table-of-contents)
  - [📱 Supported Devices](#-supported-devices)
  - [📦 Installation](#-installation)
    - [Requirements](#requirements)
    - [Install Plugin](#install-plugin)
  - [🚀 Usage](#-usage)
  - [🛠️ Development](#️-development)
    - [Building](#building)
    - [Packaging](#packaging)
  - [📜 License](#-license)

## 📱 Supported Devices

This plugin works by injecting custom JavaScript into Jellyfin's web interface. It is compatible with:

- ✅ **Jellyfin Web UI** (browser access)
- ✅ **Jellyfin Android App** (uses embedded web UI)
- ✅ **Jellyfin iOS App** (uses embedded web UI)
- ✅ **Jellyfin Desktop Apps** (Flatpak, etc. - uses embedded web UI)
- ⏳️ **Streamyfin** (work in progress)
- ❌ **Android TV App** (uses native interface, cannot be modified)
- ❌ **Other native apps** that don't use the web interface

## 📦 Installation

### Requirements

This plugin requires the following plugins to be installed as well:

- [Jellyfin-JavaScript-Injector](https://github.com/n00bcodr/Jellyfin-JavaScript-Injector)
- [jellyfin-plugin-file-transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation) (optional, but recommended)
-

### Install Plugin

1. Open your Jellyfin server's admin dashboard
2. Navigate to **Plugins** → **Catalog**
3. Click the **Add Repository** button
4. Add this repository URL:
   ```
   https://raw.githubusercontent.com/jon4hz/jellyfin-plugin-jellysweep/main/manifest.json
   ```
5. Find **Jellysweep** in the plugin catalog and install it
6. Restart your Jellyfin server
7. Enable the plugin in **Plugins** → **My Plugins**

## 🚀 Usage

1. Configure a secure API key in your jellysweep config
2. Add the jellyfin URL and API key in the plugin settings in Jellyfin

## 🛠️ Development

### Building

```bash
dotnet build
```

### Packaging

```bash
make package
```

The plugin follows Jellyfin's plugin architecture and integrates with the session management system to track playback state and automatically pause content when timers expire.

## 📜 License

GPLv3
