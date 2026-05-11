# WinForms Print Options — Full Comparison

This document covers all five print approaches demonstrated in the sample.

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

## 5. MSHTML / WebBrowser Control (Legacy — `WebBrowser.ShowPrintDialog()`)

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

| | System | Browser | GDI | PDF | MSHTML |
|---|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** | **Yes** | **Yes** | No |
| Native OS print dialog | Yes | No | Yes | No | No |
| Works with hidden WebView2 | Yes | No | N/A | No | N/A |
| Affected by dark mode | No | Yes* | No | No | No |
| Modern HTML/CSS support | Yes | Yes | No | Yes | No |
| No user interaction needed | No | No | No | No | No |
| PDF output | No | No | No | **Yes** | No |
| Margin / scale control | Basic | Full | Manual | Programmatic | Basic |
| Status | Current | **Recommended** | Current | Current | Deprecated |

\* Dark mode override is applied (`PreferredColorScheme = Light`).

---

## Recommendations

| Use case | Recommended approach |
|---|---|
| User wants the richest preview + most print settings | **Browser** |
| App must look fully native end-to-end | **GDI Print** (non-HTML) or **System** |
| Generate a PDF and print within the app | **PDF Print** |
| Printing complex HTML/images where layout fidelity matters | **Browser** or **PDF Print** |
| Legacy IE compatibility | **Do not use** — MSHTML is deprecated |
