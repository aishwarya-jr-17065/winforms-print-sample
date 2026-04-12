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
3. Click **Print** — the app loads the HTML into a hidden WebView2 instance, then calls `CoreWebView2.PrintAsync()` which opens the native Windows print dialog.
4. Because WebView2 uses Chromium's high-DPI rendering pipeline, the printout matches what a modern browser produces — pixel-perfect, no blurriness.

## Project Structure

```
WinFormsPrintSample/
├── Program.cs               # Entry point
├── MainForm.cs              # Code-behind: WebView2 init + print logic
├── MainForm.Designer.cs     # UI layout (label, TextBox, Print button)
└── WinFormsPrintSample.csproj
```

## Key Implementation Details

- **Target framework**: `net8.0-windows10.0.19041.0`
- **WebView2 package**: `Microsoft.Web.WebView2` (latest stable release)
- **Printing**: `CoreWebView2.PrintAsync(null)` — shows the system print dialog with full Chromium rendering fidelity.
- A hidden `WebView2` control is added to the form at startup and initialised asynchronously; it is only used for rendering and printing — it is never visible to the user.
