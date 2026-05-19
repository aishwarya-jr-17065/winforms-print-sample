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

Uses `System.Drawing.Printing` with a `PrintPreviewDialog`. Rather than stripping HTML to plain text, this approach renders the HTML through the **WebView2 → PDF → raster** pipeline: the hidden WebView2 exports the page to a PDF stream via `PrintToPdfStreamAsync`, every page is rasterised to a `Bitmap` at 150 DPI using the `Windows.Data.Pdf` WinRT API, and the bitmaps are drawn into the `PrintPage` event with `Graphics.DrawImage`. This produces pixel-perfect, CSS-faithful output — images, tables, and styled text all print exactly as they appear in the browser.

| | |
|---|---|
| **Preview** | **Yes** — WinForms built-in `PrintPreviewDialog` control showing the rasterised pages. |
| **Look** | Native Windows controls throughout (preview dialog + system print dialog). |
| **User control** | Printer, copies, page range, orientation, paper size (via the print button inside `PrintPreviewDialog`). |
| **HTML rendering** | **Yes** — rendered via WebView2 → PDF → raster at 150 DPI; full CSS/image fidelity. |

**Pros**
- Pixel-perfect HTML/CSS output — rendered through Chromium's PDF engine.
- Native look and feel end-to-end (WinForms `PrintPreviewDialog` + system print dialog).
- Built-in `PrintPreviewDialog` with zero extra UI dependencies.
- Page margins zeroed out so the rasterised bitmap fills the full printable area.

**Cons**
- Requires WebView2 runtime (for the PDF rasterisation step).
- Raster output — not vector; very large or detailed pages may show slight pixelation at extreme zoom.
- GDI pagination is fixed by the PDF page count; layout cannot be customised further.

**Why GDI Print does not show a separate print-picker dialog:**

`PrintPreviewDialog` is a self-contained WinForms popup. It has its own toolbar that contains a print button. When the user clicks that button, `PrintPreviewDialog` calls `printDocument.Print()` internally and directly — it does **not** create or show a `PrintDialog` first. The job is sent immediately to whichever printer is already set in `printDocument.PrinterSettings` (the system default if you have not changed it). There is no separate printer-selection step unless you wire one up yourself.

---

## 4. Embedded Preview (`PrintPreviewControl` in a custom form)

Hosts `PrintPreviewControl` — a bare WinForms control — inside a custom `PrintPreviewControlForm`. Unlike `PrintPreviewDialog` (which is a self-contained popup that you simply call `ShowDialog()` on), `PrintPreviewControl` is just a control that you embed wherever you like and surround with your own UI. This form adds ± zoom buttons, a zoom percentage label, and a "Print…" button backed by `PrintDialog`.

Like the GDI Print approach, this uses the **WebView2 → PDF → raster** pipeline: the hidden WebView2 exports the HTML to a PDF stream, every page is rasterised to a `Bitmap` at 150 DPI via `Windows.Data.Pdf`, and the bitmaps are drawn in the `PrintPage` event — giving pixel-perfect, CSS-faithful output.

Implements the MS docs "[How to print in Windows Forms using Print Preview](https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview)" `PrintPreviewControl` example.

| | |
|---|---|
| **Preview** | **Yes** — `PrintPreviewControl` renders the rasterised pages in-place with anti-aliasing. |
| **Look** | Fully custom — the surrounding toolbar and buttons are defined by your code. |
| **User control** | Printer, copies, page range (via `PrintDialog`). Zoom controlled with ± buttons. |
| **HTML rendering** | **Yes** — rendered via WebView2 → PDF → raster at 150 DPI; full CSS/image fidelity. |

**Pros**
- Pixel-perfect HTML/CSS output — rendered through Chromium's PDF engine.
- Complete UI freedom — embed the preview panel anywhere in your application layout.
- Can configure `Zoom`, `Columns`, `Rows`, and `UseAntiAlias` programmatically.
- Native look and feel for all controls.

**Cons**
- Requires WebView2 runtime (for the PDF rasterisation step).
- You must build the surrounding toolbar yourself (`PrintPreviewDialog` gives you this for free).
- Raster output — same pixelation caveat as GDI Print at extreme zoom.

**How Embedded Preview shows the print-picker dialog:**

`PrintPreviewControlForm` has a custom "Print…" button. When clicked, `OnPrintClick` explicitly creates a `new PrintDialog { Document = _printDocument, AllowSomePages = true }` and calls `dlg.ShowDialog(this)`. This opens the native Win32 `PrintDlgEx` dialog — the OS-level printer picker — where the user can choose a printer, set copies, page range, etc. Only after the user clicks OK does the code call `_printDocument.Print()`.

This is the direct opposite of how `PrintPreviewDialog` works: because you build the toolbar yourself you are free to call `PrintDialog.ShowDialog()` before printing, whereas `PrintPreviewDialog`'s built-in print button skips that step entirely.

**Key difference from GDI Print (approach #3):**

| | GDI Print | Embedded Preview |
|---|---|---|
| Preview container | `PrintPreviewDialog` (complete dialog) | `PrintPreviewControl` (raw control, custom form) |
| Toolbar | Built-in | You build it |
| Embedding | Popup dialog only | Anywhere in your form |
| Printer-picker dialog | ❌ None — prints to default immediately | ✅ `PrintDialog.ShowDialog()` before printing |

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

| | System | Browser | GDI | Embedded Preview | MSHTML |
|---|:---:|:---:|:---:|:---:|:---:|
| Print preview | No | **Yes** | **Yes** | **Yes** | No |
| Native OS print dialog | Yes | No | Yes | Yes | Yes |
| Works with hidden WebView2 | Yes | No | N/A | N/A | N/A |
| Affected by dark mode | No | Yes* | No | No | No |
| Modern HTML/CSS support | Yes | Yes | **Yes** (raster) | **Yes** (raster) | No |
| No user interaction needed | No | No | No | No | No |
| PDF output | No | No | No | No | No |
| Margin / scale control | Basic | Full | Auto-scale | Auto-scale | Basic |
| Status | Current | **Recommended** | Current | Current | Deprecated |

\* Dark mode override is applied (`PreferredColorScheme = Light`).

---

## Recommendations

| Use case | Recommended approach |
|---|---|
| User wants the richest preview + most print settings | **Browser** |
| App must look fully native end-to-end | **GDI Print** or **System** |
| Printing complex HTML/images where layout fidelity matters | **Browser** or **GDI Print** |
| Custom print preview UI embedded inside your form | **Embedded Preview** (`PrintPreviewControl`) |
| Legacy IE compatibility | **Do not use** — MSHTML is deprecated |

---

## 6. The WinRT / Modern Windows Print Dialog (`Windows.Graphics.Printing.PrintManager`)

The dialog shown in the screenshot below is **not** the classic Win32 `PrintDlgEx` dialog. It is the **WinRT `PrintManager`** print UI — the same modern print sheet you see when you press `Ctrl+P` in Microsoft Edge, Trident/Zoho Mail, or any UWP/WinUI 3 app. It shows printer, orientation, copies, colour mode, pages, and collate options in a dark-themed flyout with a live preview pane on the right.

> **This dialog is not shown by any of the five approaches in this sample, and it cannot be trivially added to a WinForms app.** Here is why, and what would be needed to support it.

### Why WinForms cannot use `PrintManager` out of the box

| Blocker | Detail |
|---|---|
| **`CoreWindow` / `CoreApplicationView` required** | `PrintManager.GetForCurrentView()` looks up the `PrintManager` for the current UWP `CoreApplicationView`. WinForms has no `CoreApplicationView`; calling this API from a WinForms process throws `System.Exception: Element not found`. |
| **HWND interop only partially helps** | Desktop apps (Win32, WinForms, WPF) can bypass `GetForCurrentView()` by using the `PrintManagerInterop` COM interface (`Windows.Graphics.Printing.PrintManagerInterop`) together with `IInitializeWithWindow`. `PrintManagerInterop.GetForWindow(hwnd)` gives you a `PrintManager` tied to a specific HWND. This makes it possible to show the print UI flyout. However, showing the flyout is only one half of the problem. |
| **Content pipeline requires XAML `UIElement`** | Once the flyout is visible, Windows fires `PrintTaskRequested`. The app must create a `PrintTask` backed by a `Windows.Graphics.Printing.PrintDocument` (a WinRT type, completely separate from `System.Drawing.Printing.PrintDocument`). That WinRT `PrintDocument` fires three callbacks — `Paginate`, `GetPreviewPage`, and `AddPages` — each of which must supply a **XAML `UIElement`** to be rendered into the preview and onto the page. WinForms uses GDI and Win32 rendering; it has no XAML `UIElement` objects to provide. |
| **No built-in WinRT print support in WinForms** | The `System.Drawing.Printing` stack (used by `PrintDocument`, `PrintPreviewDialog`, `PrintDialog`) is entirely separate from the WinRT print pipeline. There is no official bridge between the two. |

### Can it be supported — and how?

Yes, technically, but it requires significant additional infrastructure:

1. **Use `PrintManagerInterop` to show the flyout.**  
   Call `PrintManagerInterop.GetForWindow(this.Handle)`, subscribe to `PrintTaskRequested`, then call `ShowPrintUIForWindowAsync(this.Handle)`. This part works in a WinForms app.

2. **Feed content through the WinRT pipeline.**  
   In the `PrintTaskRequested` handler you create a `PrintTask`. The task's source must implement the WinRT `IPrintDocumentSource` interface. The only practical way to produce XAML `UIElement` content in a WinForms process is to either:
   - **Host a XAML Island** (`Microsoft.Xaml.Controls.XamlIsland` / `WindowsXamlManager`) inside the WinForms app. Render your content as XAML and let it flow through the WinRT callbacks. This is the approach used by WinUI 3 desktop apps, but it pulls in a significant XAML hosting dependency.
   - **Rasterise to `BitmapImage`**. Convert each print page to a bitmap (e.g. via `Graphics.CopyFromScreen`, GDI, or the WebView→PDF→raster pipeline already used in this sample), then wrap each bitmap in a XAML `Image` element and supply it in `GetPreviewPage` / `AddPages`. This avoids XAML layout but still requires a `WindowsXamlManager` to be initialised so that WinRT can create XAML nodes.

3. **Why this is not done in this sample.**  
   - Hosting a XAML Island adds `Microsoft.WindowsAppSDK` or WinUI 3 runtime dependencies.  
   - The `WindowsXamlManager` must be initialised on the UI thread before any XAML object is created.  
   - The rasterise-and-feed approach duplicates the existing PDF→raster pipeline but routes the output through a completely different (and much more complex) channel just to show a different print dialog.  
   - The end result for the user — selecting a printer and printing pages — is identical to using `PrintDialog` (Win32) or `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)`, which are already demonstrated in this sample with zero extra dependencies.

### Summary

| | Win32 `PrintDialog` | WinRT `PrintManager` flyout |
|---|---|---|
| Works in WinForms out of the box | ✅ Yes | ❌ No |
| Requires HWND interop | No | Yes (`PrintManagerInterop`) |
| Requires XAML / WinUI hosting | No | Yes (for preview content) |
| Shows live print preview | No | Yes |
| Official WinForms support | ✅ | ❌ Not supported |
| Feasible with extra work | — | Yes, with XAML Islands + significant effort |

> **Recommendation:** For a native modern-looking print picker in a WinForms app today, the closest practical option is the **Browser dialog** (approach #2) — Chromium's print sheet rendered inside a WebView2 window — which gives a live preview and a rich settings panel without any XAML hosting complexity.

