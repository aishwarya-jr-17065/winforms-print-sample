# WebView2 Print Dialogs in WinForms

## System Dialog (`CoreWebView2PrintDialogKind.System`)

The OS-level `PrintDlgEx` dialog. WebView2 uses it only for settings collection, then drives printing through Chromium internally.

| | |
|---|---|
| **Preview** | None — the Windows system dialog has no preview pane by design. It only collects printer/settings and returns them to the caller. |
| **Look** | Native Windows print dialog — consistent with other Win32 apps. |
| **User control** | Printer, copies, page range, orientation, paper size. |
| **Dark theme** | Not affected — it is an OS dialog. |

**Pros**
- Familiar, native OS appearance.
- No WebView2 visibility requirements.
- Works even with a hidden/off-screen WebView2 control.

**Cons**
- No print preview whatsoever.
- Less control over print settings programmatically (settings come back via OS structures).

---

## Browser Dialog (`CoreWebView2PrintDialogKind.Browser`)

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

## MSHTML / WebBrowser Control (Legacy — `WebBrowser.ShowPrintDialog()`)

The old WinForms `WebBrowser` control wraps the MSHTML (Trident) engine — the same engine that powered Internet Explorer.

| | |
|---|---|
| **Preview** | **Was:** IE's own dialog with a live Trident-rendered preview pane. **Now:** Falls through to the plain OS `PrintDlgEx` — no preview, same as System Dialog. |
| **Look** | IE-era print dialog on old Windows; plain OS dialog on modern Windows. |
| **Engine** | MSHTML / Trident — does not support modern CSS (flexbox, grid, CSS variables, etc.). |
| **Dark theme** | Not affected — IE renders in light mode only. |

**Why preview no longer works:**
IE's UI shell (which hosted the preview pane) has been removed from Windows 10 (post-2022 cumulative updates) and Windows 11. The preview was tightly coupled to Trident's rendering pipeline via `IOleCommandTarget`/`OLECMDID_PRINTPREVIEW` — APIs that are no longer functional. `ShowPrintDialog()` now falls through to `PrintDlgEx` directly.

**Pros** *(historical)*
- Previously the only WinForms option for HTML print preview without third-party libraries.

**Cons**
- Preview does **not** work on any currently supported Windows version.
- MSHTML is deprecated; IE is removed from Windows 11 by default.
- Poor rendering of modern HTML/CSS — no flexbox, grid, or CSS custom properties.
- No security updates.

> **Conclusion:** MSHTML offers no advantage over the System dialog on modern Windows.  
> The WebView2 Browser dialog is the **only** path to real print preview in a modern WinForms app.

---

## Quick Comparison

| | System Dialog | Browser Dialog | MSHTML Dialog |
|---|:---:|:---:|:---:|
| Print preview | No | **Yes** | No (broken on modern Windows) |
| Native OS look | Yes | No | No |
| Works with hidden WebView2 | Yes | No | N/A |
| Affected by dark mode | No | Yes (needs override) | No |
| Modern HTML/CSS support | Yes (Chromium) | Yes (Chromium) | No (Trident) |
| Margin / scale control | Basic | Full | Basic |
| Status | Current | **Current / Recommended** | Deprecated |

---

## Recommendation

| Use case | Recommended dialog |
|---|---|
| User wants to see a preview before printing | **Browser** |
| Automated / batch printing, no user interaction | **System** (or `PrintAsync` with no dialog) |
| App must look fully native | **System** |
| Printing complex HTML/images where layout matters | **Browser** |
| Legacy IE compatibility | **Do not use** — MSHTML is deprecated |
