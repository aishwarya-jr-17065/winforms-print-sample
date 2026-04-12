# WinForms HTML Print Sample

A .NET 8 Windows Forms application that prints HTML content with **high fidelity** using the **Microsoft Edge WebView2** (Chromium) runtime — eliminating the blurry output produced by older GDI-based approaches.

## Requirements

| Requirement | Details |
|---|---|
| OS | Windows 10 (version 19041+) / Windows 11 |
| .NET | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| WebView2 Runtime | [Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) |

> The Evergreen WebView2 Runtime is pre-installed on Windows 11 and most up-to-date Windows 10 machines.

## Build

```shell
# Windows
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
3. Choose one of two print modes:
   - **System Print** — loads the HTML into a hidden WebView2 instance and calls `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)`, which opens the native Windows (OS-level) print dialog. No print preview is shown; the user picks a printer and settings directly.
   - **Browser Print** — opens a dedicated **Browser Print Preview** window containing a fully visible, properly sized WebView2. Once the page has loaded, the user can click **🖨 Print…** inside that window, which triggers `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)` — Chromium's own browser print dialog complete with a live print preview. The preview window must be visible and correctly sized so that Chromium can render the preview (a hidden or off-screen WebView2 would produce an error instead of a preview).
4. Because WebView2 uses Chromium's high-DPI rendering pipeline, the printout matches what a modern browser produces — pixel-perfect, no blurriness.

## Project Structure

```
WinFormsPrintSample/
├── Program.cs               # Entry point
├── MainForm.cs              # Code-behind: WebView2 init + System/Browser print logic
├── MainForm.Designer.cs     # UI layout (label, TextBox, System Print & Browser Print buttons)
├── BrowserPrintForm.cs      # Dedicated preview window for the browser print dialog
└── WinFormsPrintSample.csproj
```

## Key Implementation Details

- **Target framework**: `net8.0-windows10.0.19041.0`
- **WebView2 package**: `Microsoft.Web.WebView2` (latest stable release)
- **System Print**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)` — opens the native Windows (OS-level) printer/settings dialog. A small hidden `WebView2` control is added to the form at startup and initialised asynchronously; it is only used for rendering and printing and is never visible to the user. Note: `CoreWebView2.PrintAsync(null)` silently prints without any dialog and must **not** be used when user interaction is needed.
- **Browser Print**: A separate `BrowserPrintForm` window hosts a **fully visible**, correctly sized `WebView2`. This is required because Chromium cannot render print previews inside a hidden or off-screen control — it produces an error message instead. Once the page is loaded, `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)` opens Chromium's built-in print dialog with a live preview.
