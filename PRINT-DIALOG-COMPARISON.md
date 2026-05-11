# WinForms Print Options — Full Comparison

This document covers all eight print approaches demonstrated in the sample.

---

## 1. System Dialog (`CoreWebView2PrintDialogKind.System`)

The OS-level `PrintDlgEx` dialog. WebView2 uses it only for settings collection, then drives printing through Chromium internally.

| | |
|---|---|
| **Preview** | None — the Windows system dialog has no preview pane by design. |
| **Look** | Native Windows print dialog — consistent with other Win32 apps. |
| **User control** | Printer, copies, page range, orientation, paper size. |
| **Dark theme** | Not affected — it is an OS dialog. |

**Pros**
- Familiar, native OS appearance.
- No WebView2 visibility requirements.
- Works even with a hidden/off-screen WebView2 control.

**Cons**
- No print preview whatsoever.

---

## 2. Browser Dialog (`CoreWebView2PrintDialogKind.Browser`)

Chromium's own print dialog — the same one you see when you press `Ctrl+P` in Edge/Chrome.

| | |
|---|---|
| **Preview** | Full live print preview rendered by Chromium. |
| **Look** | Edge/Chrome browser style — may feel out of place in a native WinForms app. |
| **User control** | All standard print settings plus paper size, margins, headers/footers, background graphics, scale. |
| **Dark theme** | Chromium auto-darkens content based on `prefers-color-scheme`. Fix: set `Profile.PreferredColorScheme = Light` and `DefaultBackgroundColor = White`. |

**Pros**
- Rich print preview — user sees exactly what will be printed.
- More granular settings exposed (margins, backgrounds, scale, etc.).
- Better for HTML/image content where layout accuracy matters.

**Cons**
- Requires the WebView2 control to be **visible and properly sized**; a hidden or 1×1 control produces *"This app doesn't support print preview"*.
- Needs a dedicated visible window (e.g. `BrowserPrintForm`) to host the WebView2.
- Chromium applies system dark mode to page content unless explicitly overridden.
- Dialog looks like a browser, not a native Windows app.

---

## 3. WinForms GDI Print (`PrintDocument` + `PrintPreviewDialog`)

The classic WinForms printing approach using `System.Drawing.Printing`. Entirely independent of WebView2. Renders content via GDI `Graphics` primitives in the `PrintPage` event.

| | |
|---|---|
| **Preview** | **Yes** — WinForms built-in `PrintPreviewDialog` control. |
| **Look** | Native Windows controls throughout (preview dialog + system print dialog). |
| **User control** | Printer, copies, page range, orientation, paper size (via the print button inside `PrintPreviewDialog`). |
| **HTML rendering** | **No** — HTML tags are stripped; the plain text is rendered with `Graphics.DrawString`. Use this approach for non-HTML documents (invoices, reports built with `Graphics` calls). |

**Pros**
- No dependency on WebView2.
- Works everywhere .NET WinForms runs.
- Native look and feel end-to-end.
- Supports fully custom page layout via GDI `Graphics` API.
- Built-in `PrintPreviewDialog` + system print dialog with zero extra dependencies.

**Cons**
- Cannot render HTML/CSS — not suitable for printing web content.
- GDI pagination and layout must be implemented manually.

---

## 4. PDF Print (`CoreWebView2.PrintToPdfAsync`)

Exports the HTML page to a PDF file using Chromium's PDF renderer, then opens it in `PdfPrintForm` — an in-app WebView2 window that renders the PDF natively. The user can review the document and click **Print…** to open the browser print dialog, all without leaving the application.

| | |
|---|---|
| **Preview** | **Yes** — the PDF is shown in a full-size WebView2 pane within the app. |
| **Look** | Chromium's built-in PDF viewer inside a WinForms window. |
| **User control** | Full browser print dialog options; `CoreWebView2PrintSettings` can also control PDF output (paper size, margins, etc.). |
| **Dark theme** | Not applicable — PDF rendering is not affected by color-scheme overrides. |

**Pros**
- High-fidelity PDF output — identical to what Chromium would print.
- Preview and print entirely within the application — no external viewer needed.
- Temp file is automatically cleaned up when the viewer form is closed.

**Cons**
- Two-step workflow: generate PDF → open in-app viewer → print.
- The in-app viewer uses Chromium's browser print dialog (not the native OS dialog).

---

## 5. Screen Print (`Graphics.CopyFromScreen` + `PrintDocument`)

Captures the entire running form as a bitmap using `Graphics.CopyFromScreen`, then prints it via the standard WinForms `PrintDocument` + `PrintPreviewDialog`. This is the approach described directly in the [Microsoft WinForms printing documentation](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-windows-form). Entirely independent of WebView2.

| | |
|---|---|
| **Preview** | **Yes** — WinForms built-in `PrintPreviewDialog` control, showing the captured screenshot. |
| **Look** | Native Windows controls throughout (preview dialog + system print dialog). |
| **User control** | Printer, copies, page range, orientation, paper size (via the print button inside `PrintPreviewDialog`). |
| **What is printed** | A pixel-exact screenshot of the form at the moment the button is clicked — includes all visible UI elements (HTML editor, buttons, labels). Not suitable for printing only the HTML content. |

**Pros**
- No dependency on WebView2.
- Works everywhere .NET WinForms runs.
- Native look and feel end-to-end.
- Simplest way to get a "what you see is what you get" printout of any form.
- Built-in `PrintPreviewDialog` + system print dialog with zero extra dependencies.
- The bitmap is automatically scaled to fit the printable area.

**Cons**
- Prints the entire form UI — not just the HTML content — so buttons and editor chrome appear on the printout.
- Screenshot quality depends on screen DPI; high-DPI displays may produce a larger, higher-quality bitmap.
- Not suitable when you want to print only the rendered HTML without UI chrome.

---

## 6. Direct / Silent Print (`PrintDocument.Print()` with no UI)

Strips HTML to plain text, creates a `PrintDocument`, and calls `Print()` directly — no preview dialog, no print dialog. The document is sent immediately to the default system printer. Uses a `StringReader` + line-by-line `DrawString` pagination, directly mirroring the MS docs "[How to print a text document](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-text-document)" `StreamReader` example.

| | |
|---|---|
| **Preview** | None — output goes straight to the default printer. |
| **Look** | No UI shown during printing; a brief confirmation message appears after. |
| **User control** | None — printer is selected from the OS default. |
| **HTML rendering** | **No** — HTML tags are stripped; plain text is rendered line-by-line. |

**Pros**
- Zero UI to dismiss — ideal for automated or batch printing.
- No dependency on WebView2.
- Works everywhere .NET WinForms runs.
- Faithful recreation of the MS docs `StreamReader` text-document example.

**Cons**
- No way for the user to select a different printer or adjust settings without code changes.
- Cannot render HTML/CSS.
- If the default printer is not configured, printing will silently fail or throw.

---

## 7. Embedded Preview (`PrintPreviewControl` in a custom form)

Hosts `PrintPreviewControl` — a bare WinForms control — inside a custom `PrintPreviewControlForm`. Unlike `PrintPreviewDialog` (which is a self-contained popup that you simply call `ShowDialog()` on), `PrintPreviewControl` is just a control that you embed wherever you like and surround with your own UI. This form adds ± zoom buttons, a zoom percentage label, and a "Print…" button backed by `PrintDialog`.

Implements the MS docs "[How to print in Windows Forms using Print Preview](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview)" `PrintPreviewControl` example.

| | |
|---|---|
| **Preview** | **Yes** — `PrintPreviewControl` renders the document in-place with anti-aliasing. |
| **Look** | Fully custom — the surrounding toolbar and buttons are defined by your code. |
| **User control** | Printer, copies, page range (via `PrintDialog`). Zoom controlled with ± buttons. |
| **HTML rendering** | **No** — HTML tags are stripped; plain text is rendered line-by-line. |

**Pros**
- Complete UI freedom — embed the preview panel anywhere in your application layout.
- Can configure `Zoom`, `Columns`, `Rows`, and `UseAntiAlias` programmatically.
- No dependency on WebView2.
- Works everywhere .NET WinForms runs.
- Native look and feel for all controls.

**Cons**
- You must build the surrounding toolbar yourself (`PrintPreviewDialog` gives you this for free).
- Cannot render HTML/CSS.

**Key difference from GDI Print (approach #3):**

| | GDI Print | Embedded Preview |
|---|---|---|
| Preview container | `PrintPreviewDialog` (complete dialog) | `PrintPreviewControl` (raw control, custom form) |
| Toolbar | Built-in | You build it |
| Embedding | Popup dialog only | Anywhere in your form |

---

## 8. MSHTML / WebBrowser Control (Legacy — `WebBrowser.ShowPrintDialog()`)

The old WinForms `WebBrowser` control wraps the MSHTML (Trident) engine — the same engine that powered Internet Explorer.

| | |
|---|---|
| **Preview** | **Was:** IE's own dialog with a live Trident-rendered preview pane. **Now:** Falls through to the plain OS `PrintDlgEx` — no preview, same as the System dialog. |
| **Look** | IE-era print dialog on old Windows; plain OS dialog on modern Windows. |
| **Engine** | MSHTML / Trident — does not support modern CSS (flexbox, grid, CSS variables, etc.). |
| **Dark theme** | Not affected — IE renders in light mode only. |

**Why preview no longer works:** IE's UI shell has been removed from Windows 10 (post-2022) and Windows 11. `ShowPrintDialog()` now falls through to `PrintDlgEx` directly.

**Pros** *(historical)*
- Previously the only WinForms option for HTML print preview without third-party libraries.

**Cons**
- Preview does **not** work on any currently supported Windows version.
- MSHTML is deprecated; IE is removed from Windows 11 by default.
- Poor rendering of modern HTML/CSS.
- No security updates.

> **Conclusion:** MSHTML offers no advantage over the System dialog on modern Windows.

---

## Quick Comparison

| | System | Browser | GDI | PDF | Screen | Direct | Embedded Preview | MSHTML |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** | **Yes** | **Yes** | **Yes** | No | **Yes** | No |
| Native OS print dialog | Yes | No | Yes | No | Yes | No | Yes | No |
| Works with hidden WebView2 | Yes | No | N/A | No | N/A | N/A | N/A | N/A |
| Affected by dark mode | No | Yes* | No | No | No | No | No | No |
| Modern HTML/CSS support | Yes | Yes | No | Yes | No | No | No | No |
| No user interaction needed | No | No | No | No | No | **Yes** | No | No |
| PDF output | No | No | No | **Yes** | No | No | No | No |
| Margin / scale control | Basic | Full | Manual | Programmatic | Auto-scale | None | Manual | Basic |
| Status | Current | **Recommended** | Current | Current | Current | Current | Current | Deprecated |

\* Dark mode override is applied (`PreferredColorScheme = Light`).

---

## Recommendations

| Use case | Recommended approach |
|---|---|
| User wants the richest preview + most print settings | **Browser** |
| App must look fully native end-to-end | **GDI Print** (non-HTML) or **System** |
| Generate a PDF and print within the app | **PDF Print** |
| Printing complex HTML/images where layout fidelity matters | **Browser** or **PDF Print** |
| Quick "print what I see on screen" / form screenshot | **Screen Print** |
| Batch / silent / automated printing (no user interaction) | **Direct Print** |
| Custom print preview UI embedded inside your form | **Embedded Preview** (`PrintPreviewControl`) |
| Legacy IE compatibility | **Do not use** — MSHTML is deprecated |
