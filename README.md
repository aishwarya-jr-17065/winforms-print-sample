# WinForms HTML Print Sample

A .NET 8 Windows Forms application that prints HTML content using the **Microsoft Edge WebView2** (Chromium) runtime. It demonstrates **seven** different print approaches side-by-side and documents their trade-offs.

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
3. Click one of the seven print buttons:

### Row 1 — WebView2 & native alternatives (new options)

| Button | Behaviour |
|---|---|
| **System + Preview** | Opens `SystemPrintPreviewForm` — a visible WebView2 acts as the visual preview; clicking **Print (System)…** opens the native Windows `PrintDlgEx` dialog. The only way to combine a preview *and* the system dialog. |
| **Silent Print** | Calls `CoreWebView2.PrintAsync(null)` — sends the rendered HTML directly to the default printer with no dialog whatsoever. |
| **GDI Print** | Uses the classic WinForms `PrintDocument` + `PrintPreviewDialog` (GDI / `System.Drawing`) — entirely independent of WebView2. Shows the HTML source rendered as plain text in the built-in WinForms print-preview window. |
| **PDF Print** | Calls `CoreWebView2.PrintToPdfAsync()` to export the HTML as a full-fidelity PDF, then opens the PDF in the system default viewer (e.g. Edge, Adobe Reader) where the user can review and print. |

### Row 2 — Original three options

| Button | Behaviour |
|---|---|
| **System Print** | Loads HTML into a hidden WebView2, opens the native Windows `PrintDlgEx` dialog. No print preview. |
| **Browser Print** | Opens `BrowserPrintForm` — a dedicated window with a full-size visible WebView2. Click **Print…** inside to open Chromium's browser print dialog, which includes a live print preview. |
| **MSHTML Print** | Opens `MshtmlPrintForm` — uses the legacy `WebBrowser` (MSHTML/Trident) control. Historically showed IE's print preview; on Windows 10 (post-2022) and Windows 11 the IE UI shell is gone so this behaves identically to the system dialog with no preview. **Provided for comparison only.** |

> See [PRINT-DIALOG-COMPARISON.md](PRINT-DIALOG-COMPARISON.md) for a full comparison of all seven approaches including pros, cons, and recommendations.

## Print Method Comparison (Summary)

| | System Dialog | System + Preview | Browser Dialog | Silent Print | GDI Print | PDF Print | MSHTML Dialog |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** (WebView2 pane) | **Yes** (Chromium dialog) | No | **Yes** (WinForms GDI) | **Yes** (PDF viewer) | No |
| Native OS print dialog | Yes | **Yes** | No | No | Yes (via `PrintDialog`) | Via PDF viewer | No |
| Modern HTML/CSS | Yes | Yes | Yes | Yes | No (plain text) | **Yes** | No |
| No dialog / silent | No | No | No | **Yes** | No | No | No |
| PDF output | No | No | No | No | No | **Yes** | No |
| Status | Current | **Current / Recommended** | **Current / Recommended** | Current | Current | Current | Deprecated |

## Project Structure

```
WinFormsPrintSample/
├── Program.cs                   # Entry point
├── MainForm.cs                  # Code-behind: WebView2 init + all seven print handlers
├── MainForm.Designer.cs         # UI layout (label, TextBox, seven print buttons in two rows)
├── BrowserPrintForm.cs          # Preview window using visible WebView2 + browser dialog
├── SystemPrintPreviewForm.cs    # Preview window using visible WebView2 + system dialog
├── MshtmlPrintForm.cs           # Legacy WebBrowser (MSHTML/Trident) print window
└── WinFormsPrintSample.csproj
PRINT-DIALOG-COMPARISON.md      # Detailed comparison of all seven print approaches
```

## Key Implementation Details

- **Target framework**: `net8.0-windows10.0.19041.0`
- **WebView2 package**: `Microsoft.Web.WebView2` (latest stable release)
- **System printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)` — native OS dialog, no preview. The WebView2 control can remain hidden.
- **System + Preview**: `SystemPrintPreviewForm` — a visible WebView2 pane shows the rendered HTML as a preview; the **Print (System)…** button calls `ShowPrintUI(CoreWebView2PrintDialogKind.System)`. This is the **only built-in way to pair a visual preview with the Windows system dialog**.
- **Browser printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)` — Chromium's own dialog with live preview. Requires the WebView2 control to be **visible and properly sized**; a hidden/1×1 control produces *"This app doesn't support print preview"*.
- **Silent printing**: `CoreWebView2.PrintAsync(null)` — prints directly to the default printer with no dialog. Returns a `CoreWebView2PrintStatus` indicating success or failure.
- **GDI printing**: `PrintDocument` + `PrintPreviewDialog` — the classic WinForms approach using `System.Drawing`. Independent of WebView2; renders the HTML source as plain wrapped text via `Graphics.DrawString`. Demonstrates the built-in WinForms preview control.
- **PDF printing**: `CoreWebView2.PrintToPdfAsync(filePath, null)` — saves a high-fidelity PDF to a temp file, then launches the system default PDF handler via `Process.Start`. The user prints from the PDF viewer.
- **Dark mode**: `Profile.PreferredColorScheme = Light` + `DefaultBackgroundColor = White` are set on any visible WebView2 to prevent Chromium from auto-darkening page content when the system is in dark mode.
- **Shared environment**: All WebView2 instances share the same `CoreWebView2Environment` so they run in the same Chromium process group.
