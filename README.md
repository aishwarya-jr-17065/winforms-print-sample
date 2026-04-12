# WinForms HTML Print Sample

A .NET 8 Windows Forms application that prints HTML content using the **Microsoft Edge WebView2** (Chromium) runtime. It demonstrates three different print dialog approaches side-by-side and documents their trade-offs.

## Requirements

| Requirement | Details |
|---|---|
| OS | Windows 10 (version 19041+) / Windows 11 |
| .NET | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| WebView2 Runtime | [Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) |

> The Evergreen WebView2 Runtime is pre-installed on Windows 11 and most up-to-date Windows 10 machines.

## Build

```shell
dotnet build WinFormsPrintSample/WinFormsPrintSample.csproj -c Release
```

## Run

```shell
dotnet run --project WinFormsPrintSample/WinFormsPrintSample.csproj
```

Or launch the compiled executable:

```
WinFormsPrintSample\bin\Release\net8.0-windows10.0.19041.0\WinFormsPrintSample.exe
```

## How It Works

1. The main window contains a **multiline text box** pre-filled with a sample HTML document.
2. Paste or type any HTML you want to print into the text box.
3. Click one of the three print buttons:

| Button | Behaviour |
|---|---|
| **System Print** | Loads HTML into a hidden WebView2, opens the native Windows `PrintDlgEx` dialog. No print preview. |
| **Browser Print** | Opens `BrowserPrintForm` — a dedicated window with a full-size visible WebView2. Click **Print…** inside to open Chromium's browser print dialog, which includes a live print preview. |
| **MSHTML Print** | Opens `MshtmlPrintForm` — uses the legacy `WebBrowser` (MSHTML/Trident) control. Historically showed IE's print preview; on Windows 10 (post-2022) and Windows 11 the IE UI shell is gone so this behaves identically to the system dialog with no preview. **Provided for comparison only.** |

> See [PRINT-DIALOGS.md](PRINT-DIALOGS.md) for a full comparison of all three approaches including pros, cons, and recommendations.

## Print Dialog Comparison (Summary)

| | System Dialog | Browser Dialog | MSHTML Dialog |
|---|:---:|:---:|:---:|
| Print preview | No | **Yes** | No (broken on modern Windows) |
| Native OS look | Yes | No | No |
| Modern HTML/CSS | Yes (Chromium) | Yes (Chromium) | No (Trident) |
| Dark mode fix needed | No | Yes | No |
| Status | Current | **Recommended** | Deprecated |

## Project Structure

```
WinFormsPrintSample/
├── Program.cs               # Entry point
├── MainForm.cs              # Code-behind: WebView2 init + all three print handlers
├── MainForm.Designer.cs     # UI layout (label, TextBox, three print buttons)
├── BrowserPrintForm.cs      # Preview window using visible WebView2 + browser dialog
├── MshtmlPrintForm.cs       # Legacy WebBrowser (MSHTML/Trident) print window
└── WinFormsPrintSample.csproj
PRINT-DIALOGS.md             # Detailed comparison of all three print approaches
```

## Key Implementation Details

- **Target framework**: `net8.0-windows10.0.19041.0`
- **WebView2 package**: `Microsoft.Web.WebView2` (latest stable release)
- **System printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)` — native OS dialog, no preview. The WebView2 control can remain hidden.
- **Browser printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)` — Chromium's own dialog with live preview. Requires the WebView2 control to be **visible and properly sized**; a hidden/1×1 control produces *"This app doesn't support print preview"*.
- **Dark mode**: `Profile.PreferredColorScheme = Light` + `DefaultBackgroundColor = White` are set on the `BrowserPrintForm` WebView2 to prevent Chromium from auto-darkening page content when the system is in dark mode.
- **Shared environment**: Both WebView2 instances (`MainForm` hidden + `BrowserPrintForm` visible) share the same `CoreWebView2Environment` so they run in the same Chromium process group.
- `CoreWebView2.PrintAsync(null)` silently prints without any dialog — useful for automated/batch printing but must **not** be used when user interaction is needed.
