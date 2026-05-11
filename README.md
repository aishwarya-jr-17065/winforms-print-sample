# WinForms HTML Print Sample

A .NET 8 Windows Forms application that prints HTML content using the **Microsoft Edge WebView2** (Chromium) runtime. It demonstrates **eight** different print approaches side-by-side and documents their trade-offs — covering every technique described in the Microsoft WinForms printing documentation.

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
3. Click one of the eight print buttons arranged in three rows:

### Row 3 — MS docs approaches (GDI, no WebView2 required)

| Button | MS Docs article | Behaviour |
|---|---|---|
| **Direct Print** | [How to print a text document](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-text-document) | Strips HTML to plain text, then calls `PrintDocument.Print()` **directly** — no dialog, no preview. Output goes straight to the default system printer. Uses `StringReader` + line-by-line `DrawString` pagination, mirroring the MS docs `StreamReader` pattern. |
| **Embedded Preview** | [How to print using Print Preview](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview) | Opens `PrintPreviewControlForm` — a custom form that **embeds `PrintPreviewControl`** (not a dialog). Shows the rendered text in a resizable panel with ± zoom buttons and a "Print…" button. This is the complement to `PrintPreviewDialog`. |

### Row 2 — Classic WinForms GDI + PDF options

| Button | Behaviour |
|---|---|
| **GDI Print** | Uses the classic WinForms `PrintDocument` + `PrintPreviewDialog` (GDI / `System.Drawing`) — entirely independent of WebView2. Shows the HTML source rendered as plain text in the built-in WinForms print-preview dialog. |
| **PDF Print** | Calls `CoreWebView2.PrintToPdfAsync()` to export the HTML as a full-fidelity PDF, then displays it in an in-app WebView2 PDF viewer where the user can review and print without leaving the application. |
| **Screen Print** | Captures the entire main form as a bitmap using `Graphics.CopyFromScreen` (the approach described in the [Microsoft WinForms printing docs](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-windows-form)), then shows it in `PrintPreviewDialog`. Prints an exact screenshot of whatever is currently displayed on screen. |

### Row 1 — WebView2 / Chromium options

| Button | Behaviour |
|---|---|
| **System Print** | Loads HTML into a hidden WebView2, opens the native Windows `PrintDlgEx` dialog. No print preview. |
| **Browser Print** | Opens `BrowserPrintForm` — a dedicated window with a full-size visible WebView2. Click **Print…** inside to open Chromium's browser print dialog, which includes a live print preview. |
| **MSHTML Print** | Opens `MshtmlPrintForm` — uses the legacy `WebBrowser` (MSHTML/Trident) control. Historically showed IE's print preview; on Windows 10 (post-2022) and Windows 11 the IE UI shell is gone so this behaves identically to the system dialog with no preview. **Provided for comparison only.** |

> See [PRINT-DIALOG-COMPARISON.md](PRINT-DIALOG-COMPARISON.md) for a full comparison of all eight approaches including pros, cons, and recommendations.

## Print Method Comparison (Summary)

| | System | Browser | GDI | PDF | Screen | Direct | Embedded Preview | MSHTML |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** (Chromium) | **Yes** (dialog) | **Yes** (in-app) | **Yes** (dialog) | No | **Yes** (embedded) | No |
| Native OS print dialog | Yes | No | Yes | No | Yes | No (silent) | Yes | No |
| Modern HTML/CSS | Yes | Yes | No | **Yes** | No | No | No | No |
| No dialog / silent | No | No | No | No | No | **Yes** | No | No |
| PDF output | No | No | No | **Yes** | No | No | No | No |
| Status | Current | **Recommended** | Current | Current | Current | Current | Current | Deprecated |

## Project Structure

```
WinFormsPrintSample/
├── Program.cs                      # Entry point
├── MainForm.cs                     # Code-behind: WebView2 init + all eight print handlers
├── MainForm.Designer.cs            # UI layout (label, TextBox, eight print buttons in three rows)
├── BrowserPrintForm.cs             # Preview window using visible WebView2 + browser dialog
├── PdfPrintForm.cs                 # In-app PDF viewer using visible WebView2 + browser dialog
├── MshtmlPrintForm.cs              # Legacy WebBrowser (MSHTML/Trident) print window
├── PrintPreviewControlForm.cs      # Custom form embedding PrintPreviewControl (not a dialog)
└── WinFormsPrintSample.csproj
PRINT-DIALOG-COMPARISON.md         # Detailed comparison of all eight print approaches
```

## Key Implementation Details

- **Target framework**: `net8.0-windows10.0.19041.0`
- **WebView2 package**: `Microsoft.Web.WebView2` (latest stable release)
- **System printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)` — native OS dialog, no preview. The WebView2 control can remain hidden.
- **Browser printing**: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)` — Chromium's own dialog with live preview. Requires the WebView2 control to be **visible and properly sized**; a hidden/1×1 control produces *"This app doesn't support print preview"*.
- **GDI printing**: `PrintDocument` + `PrintPreviewDialog` — the classic WinForms approach using `System.Drawing`. Independent of WebView2; renders the HTML source as plain wrapped text via `Graphics.DrawString`. Demonstrates the built-in WinForms preview dialog.
- **PDF printing**: `CoreWebView2.PrintToPdfAsync(filePath, null)` — saves a high-fidelity PDF to a temp file, then opens it in `PdfPrintForm` — a visible WebView2 window that renders the PDF natively. The user clicks **Print…** to open the browser print dialog from within the app. The temp file is deleted when the viewer form is closed.
- **Screen printing**: `Graphics.CopyFromScreen` captures the entire form as a `Bitmap`, which is then passed to `PrintDocument.PrintPage` and displayed in `PrintPreviewDialog`. The bitmap is scaled to fit the printable margin bounds and disposed when the preview dialog closes. Implements the [MS docs CopyFromScreen approach](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-windows-form).
- **Direct print**: `PrintDocument.Print()` is called with no dialog — output goes straight to the default printer. Uses a `StringReader` + line-by-line `DrawString` pagination, exactly mirroring the [MS docs StreamReader pattern](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-text-document).
- **Embedded preview**: `PrintPreviewControl` is embedded inside `PrintPreviewControlForm` — a custom form with ± zoom buttons and a "Print…" button. Unlike `PrintPreviewDialog` (which is a self-contained popup), `PrintPreviewControl` is a raw WinForms control you place inside any form, giving full control over the surrounding UI. Demonstrates the [MS docs PrintPreviewControl approach](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview).
- **Shared environment**: All WebView2 instances share the same `CoreWebView2Environment` so they run in the same Chromium process group.
