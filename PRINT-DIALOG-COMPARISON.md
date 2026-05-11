# WinForms Print Options — Full Comparison

This document covers all seven print approaches demonstrated in the sample.

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

## 2. System Dialog + Custom Preview (`SystemPrintPreviewForm`)

A `SystemPrintPreviewForm` window holds a **fully visible WebView2** that renders the HTML. The user sees the rendered page as a visual preview; clicking **Print (System)…** opens the native Windows `PrintDlgEx` dialog.

| | |
|---|---|
| **Preview** | **Yes** — the WebView2 pane in the form shows the fully rendered HTML. |
| **Look** | Preview is Chromium-rendered; the print dialog is native Windows. |
| **User control** | Printer, copies, page range, orientation, paper size (via the OS dialog). |
| **Dark theme** | Override applied: `PreferredColorScheme = Light` + `DefaultBackgroundColor = White`. |

**Pros**
- The **only** built-in way to give users a visual preview *and* the familiar Windows system print dialog.
- Native OS dialog for printer/settings selection.
- Chromium-quality HTML rendering in the preview.

**Cons**
- Requires a dedicated visible window to host the WebView2.
- The "preview" is the form itself, not a pane inside the print dialog.

---

## 3. Browser Dialog (`CoreWebView2PrintDialogKind.Browser`)

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

## 4. Silent Print — No Dialog (`CoreWebView2.PrintAsync(null)`)

Prints directly to the default printer using default settings, with no dialog shown at all.

| | |
|---|---|
| **Preview** | None. |
| **Look** | No UI at all — prints silently. |
| **User control** | None at the time of printing; settings can be passed programmatically via `CoreWebView2PrintSettings`. |
| **Dark theme** | Not applicable — no UI. |

**Pros**
- Ideal for automated / batch printing scenarios.
- Fastest path from HTML to paper.
- Fully programmable settings via `CoreWebView2PrintSettings`.

**Cons**
- No user interaction or confirmation.
- No preview.
- Requires a printer to be installed and available; returns a `CoreWebView2PrintStatus` error otherwise.

---

## 5. WinForms GDI Print (`PrintDocument` + `PrintPreviewDialog`)

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

## 6. PDF Print (`CoreWebView2.PrintToPdfAsync`)

Exports the HTML page to a PDF file using Chromium's PDF renderer, then opens the file in the system default PDF application (e.g. Edge, Adobe Reader) where the user can review and print.

| | |
|---|---|
| **Preview** | **Yes** — the PDF viewer shows the document before the user chooses to print. |
| **Look** | PDF viewer's native UI. |
| **User control** | Full PDF-viewer print options; `CoreWebView2PrintSettings` can control PDF output (paper size, margins, etc.). |
| **Dark theme** | Override applied on the hidden WebView2 used for rendering. |

**Pros**
- High-fidelity PDF output — identical to what Chromium would print.
- The PDF file can be saved, shared, or archived in addition to printing.
- User gets a full preview inside their familiar PDF viewer.

**Cons**
- Requires a PDF viewer to be installed (Edge is always present on Windows 10/11).
- Two-step workflow: generate PDF → open viewer → print.
- The PDF is saved to a temp file; cleanup is not automatic.

---

## 7. MSHTML / WebBrowser Control (Legacy — `WebBrowser.ShowPrintDialog()`)

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

| | System | System + Preview | Browser | Silent | GDI | PDF | MSHTML |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** | **Yes** | No | **Yes** | **Yes** | No |
| Native OS print dialog | Yes | **Yes** | No | No | Yes | Via viewer | No |
| Works with hidden WebView2 | Yes | N/A | No | Yes | N/A | Yes | N/A |
| Affected by dark mode | No | Yes* | Yes* | No | No | No | No |
| Modern HTML/CSS support | Yes | Yes | Yes | Yes | No | Yes | No |
| No user interaction needed | No | No | No | **Yes** | No | No | No |
| PDF output | No | No | No | No | No | **Yes** | No |
| Margin / scale control | Basic | Basic | Full | Programmatic | Manual | Programmatic | Basic |
| Status | Current | **Recommended** | **Recommended** | Current | Current | Current | Deprecated |

\* Dark mode override is applied (`PreferredColorScheme = Light`).

---

## Recommendations

| Use case | Recommended approach |
|---|---|
| User wants a preview before printing with the system dialog | **System + Preview** |
| User wants the richest preview + most print settings | **Browser** |
| Automated / batch printing, no user interaction | **Silent Print** |
| App must look fully native end-to-end | **GDI Print** (non-HTML) or **System** |
| Save or share the document as a PDF and optionally print | **PDF Print** |
| Printing complex HTML/images where layout fidelity matters | **Browser** or **PDF Print** |
| Legacy IE compatibility | **Do not use** — MSHTML is deprecated |
