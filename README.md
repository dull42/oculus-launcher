# Oculus Launcher

A simple tool that fixes Oculus VR headset detection by temporarily blocking Meta's API endpoints via Windows Firewall during Oculus client startup.

## The Problem

Some Oculus/Meta Quest headsets fail to be detected when Meta's cloud API interferes during initialization. The workaround is to temporarily block outbound connections to `graph.oculus.com` and `www.oculus.com` while starting the Oculus client.

## What This App Does

Click **Launch** and the app will:

1. Close the Oculus client if it's already running
2. Resolve the current IPs for Meta's API domains
3. Create a temporary Windows Firewall rule blocking those IPs
4. Start the Oculus client

When you're done, click **Stop** to remove the firewall rule and restore normal API access.

The firewall rule is automatically cleaned up if the app is closed or crashes.

## Download

Go to [Releases](../../releases) and download `OculusLauncher.exe`. No installation required.

## Usage

1. Download `OculusLauncher.exe`
2. Double-click to run (a UAC prompt will appear - admin is required for firewall access)
3. Click **Launch** — this blocks Meta's API, starts Oculus, and your headset gets detected
4. Click **Stop** — this removes the firewall rules so Oculus can function normally

You need to click Stop after Oculus has started, otherwise Oculus features that require the API won't work.

If the app can't find your Oculus client automatically, use the **Browse** button to set the path manually. This setting is saved across sessions.

## Supported Oculus Installations

The app auto-detects both old and new Oculus installs:

- **Meta Horizon** (`C:\Program Files\Meta Horizon\...`)
- **Oculus** (`C:\Program Files\Oculus\...`)
- Both `OculusClient.exe` and `client.exe` executables

## Building From Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd OculusLauncher
dotnet publish -c Release
```

The output will be a single `OculusLauncher.exe` in `bin\Release\net8.0-windows\win-x64\publish\`.

## Credits

The firewall fix approach is based on the PowerShell script by [Sprockee](https://gist.github.com/thesprockee/7e87036fc59fde56dbfb74729b42a2e2). This app wraps that logic into a simple GUI.
