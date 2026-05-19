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

**Why GDI Print does not show a separate print-picker dialog:**

`PrintPreviewDialog` is a self-contained WinForms popup. It has its own toolbar that contains a print button. When the user clicks that button, `PrintPreviewDialog` calls `printDocument.Print()` internally and directly — it does **not** create or show a `PrintDialog` first. The job is sent immediately to whichever printer is already set in `printDocument.PrinterSettings` (the system default if you have not changed it). There is no separate printer-selection step unless you wire one up yourself.

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

---

## 9. The WinRT / Modern Windows Print Dialog (`Windows.Graphics.Printing.PrintManager`)

The dialog shown in the screenshot below is **not** the classic Win32 `PrintDlgEx` dialog. It is the **WinRT `PrintManager`** print UI — the same modern print sheet you see when you press `Ctrl+P` in Microsoft Edge, Trident/Zoho Mail, or any UWP/WinUI 3 app. It shows printer, orientation, copies, colour mode, pages, and collate options in a dark-themed flyout with a live preview pane on the right.

> **This dialog is not shown by any of the eight approaches in this sample, and it cannot be trivially added to a WinForms app.** Here is why, and what would be needed to support it.

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

