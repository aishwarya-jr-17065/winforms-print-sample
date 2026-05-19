using System.Drawing.Printing;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WinFormsPrintSample;

/// <summary>
/// Main application form. Provides a multiline HTML editor and a Print button
/// that renders the HTML content using WebView2 (Chromium).
/// </summary>
public partial class MainForm : Form
{
    private WebView2? _webView;
    private CoreWebView2Environment? _env;
    private bool _webViewReady;

    public MainForm()
    {
        InitializeComponent();
        InitializeWebViewAsync();
    }

    // ---------------------------------------------------------------------------
    // WebView2 lifecycle
    // ---------------------------------------------------------------------------

    private async void InitializeWebViewAsync()
    {
        _webView = new WebView2
        {
            Visible = false,      // hidden — used only for rendering / printing
            Size = new Size(1, 1),
            Location = new Point(-1000, -1000),
        };
        Controls.Add(_webView);

        try
        {
            _env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.Combine(Path.GetTempPath(), "WinFormsPrintSample"),
                options: null);

            await _webView.EnsureCoreWebView2Async(_env);
            _webViewReady = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WebView2 runtime not found or failed to initialize.\n\n{ex.Message}\n\n" +
                "Please install the Evergreen WebView2 Runtime from:\n" +
                "https://developer.microsoft.com/microsoft-edge/webview2/",
                "WebView2 Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // ---------------------------------------------------------------------------
    // System print button handler
    // ---------------------------------------------------------------------------

    private async void btnSystemPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnSystemPrint.Enabled = false;
        btnSystemPrint.Text = "Printing…";

        try
        {
            // Load the HTML into the hidden WebView2 and wait for navigation.
            var navTask = WaitForNavigationAsync(_webView!);
            _webView!.NavigateToString(html);
            await navTask;

            // System dialog: OS-level printer/settings picker (no preview).
            _webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while printing:\n\n{ex.Message}",
                "Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnSystemPrint.Text = "System Print";
            btnSystemPrint.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // MSHTML print button handler — opens preview window using WebBrowser control
    // ---------------------------------------------------------------------------

    private void btnMshtmlPrint_Click(object sender, EventArgs e)
    {
        string html = txtHtmlContent.Text;
        if (string.IsNullOrWhiteSpace(html))
        {
            MessageBox.Show(
                "Please enter some HTML content to print.",
                "Empty Content",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var mshtmlForm = new MshtmlPrintForm(html);
        mshtmlForm.ShowDialog(this);
    }

    // ---------------------------------------------------------------------------
    // Browser print button handler — opens preview window
    // ---------------------------------------------------------------------------

    private void btnBrowserPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        // Open a dedicated window with a visible WebView2 so Chromium can
        // render the print preview inside its own browser print dialog.
        using var previewForm = new BrowserPrintForm(html, _env!);
        previewForm.ShowDialog(this);
    }

    // ---------------------------------------------------------------------------
    // WinForms GDI print — uses PrintDocument + PrintPreviewDialog.
    // Rasterises the HTML via the WebView→PDF→raster pipeline so the output
    // is pixel-perfect (full CSS/image fidelity) rather than stripped plain text.
    // ---------------------------------------------------------------------------

    private async void btnGdiPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnGdiPrint.Enabled = false;
        btnGdiPrint.Text    = "Generating PDF…";

        var pages = new List<Bitmap>();
        try
        {
            pages = await RasterizeToPagesAsync(html);

            int pageIndex = 0;
            using var printDoc = new PrintDocument { DocumentName = "WinForms GDI Print" };
            printDoc.BeginPrint += (s, pe) => pageIndex = 0;
            printDoc.PrintPage  += (s, pe) =>
            {
                var bmp    = pages[pageIndex++];
                var bounds = pe.MarginBounds;
                float fit  = Math.Min((float)bounds.Width / bmp.Width, (float)bounds.Height / bmp.Height);
                pe.Graphics!.DrawImage(bmp, new RectangleF(bounds.Left, bounds.Top, bmp.Width * fit, bmp.Height * fit));
                pe.HasMorePages = pageIndex < pages.Count;
            };

            using var previewDialog = new PrintPreviewDialog
            {
                Document      = printDoc,
                StartPosition = FormStartPosition.CenterParent,
                Width         = 900,
                Height        = 700,
                Text          = "GDI Print Preview — WebView → PDF → raster → GDI",
            };
            previewDialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred:\n\n{ex.Message}",
                "GDI Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (var bmp in pages) bmp.Dispose();
            btnGdiPrint.Text    = "GDI Print";
            btnGdiPrint.Enabled = true;
        }
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0) { yield return string.Empty; continue; }
            for (int i = 0; i < line.Length; i += maxChars)
                yield return line.Substring(i, Math.Min(maxChars, line.Length - i));
        }
    }

    // ---------------------------------------------------------------------------
    // PDF print — generates a PDF via WebView2's PrintToPdfAsync, then displays
    // it in an in-app WebView2 PDF viewer (PdfPrintForm) so the user can
    // review and print without leaving the application.
    // ---------------------------------------------------------------------------

    private async void btnPdfPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnPdfPrint.Enabled = false;
        btnPdfPrint.Text = "Generating PDF…";

        string? tempPdf = null;
        try
        {
            var navTask = WaitForNavigationAsync(_webView!);
            _webView!.NavigateToString(html);
            await navTask;

            // Ask WebView2 to produce the PDF as a stream — no file path is given
            // to the browser; we handle writing the bytes to a temp file ourselves.
            using var pdfStream = await _webView.CoreWebView2.PrintToPdfStreamAsync(null);

            if (pdfStream == null || pdfStream.Length == 0)
            {
                MessageBox.Show(
                    "The PDF could not be generated.",
                    "PDF Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            tempPdf = Path.Combine(
                Path.GetTempPath(),
                $"WinFormsPrintSample_{Guid.NewGuid():N}.pdf");

            using (var fileStream = File.Create(tempPdf))
            {
                await pdfStream.CopyToAsync(fileStream);
            }

            // Open the PDF inside the app — PdfPrintForm hosts a visible WebView2
            // that renders the PDF and lets the user print via the browser dialog.
            // PdfPrintForm deletes the temp file when it is closed.
            using var pdfForm = new PdfPrintForm(tempPdf, _env!);
            pdfForm.ShowDialog(this);
            tempPdf = null;   // ownership transferred; PdfPrintForm handles cleanup
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while generating the PDF:\n\n{ex.Message}",
                "PDF Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            // Clean up the temp file only if PdfPrintForm was never shown
            // (i.e. an exception occurred before ShowDialog was called).
            if (tempPdf is not null)
            {
                try { File.Delete(tempPdf); } catch { /* ignore */ }
            }

            btnPdfPrint.Text = "PDF Print";
            btnPdfPrint.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Screen Print — captures the form as a bitmap using Graphics.CopyFromScreen
    // (the approach described in the Microsoft WinForms printing docs), then shows
    // PrintPreviewDialog so the user can review and print via the system dialog.
    // ---------------------------------------------------------------------------

    private void btnScreenPrint_Click(object sender, EventArgs e)
    {
        // CopyFromScreen captures whatever is at the form's screen coordinates.
        // If the form is minimized it will capture something unrelated, so restore it first.
        if (this.WindowState == FormWindowState.Minimized)
            this.WindowState = FormWindowState.Normal;

        // Capture the entire form as a bitmap, exactly as described in:
        // https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-windows-form
        var formSize = this.Size;
        var screenshot = new Bitmap(formSize.Width, formSize.Height);
        using (var memG = Graphics.FromImage(screenshot))
        {
            memG.CopyFromScreen(this.Location.X, this.Location.Y, 0, 0, formSize);
        }

        using var printDoc = new PrintDocument { DocumentName = "WinForms Screen Print" };
        printDoc.PrintPage += (s, pe) =>
        {
            // Scale the captured bitmap to fit within the printable margin bounds.
            var bounds = pe.MarginBounds;
            float scaleX = (float)bounds.Width  / screenshot.Width;
            float scaleY = (float)bounds.Height / screenshot.Height;
            float scale  = Math.Min(scaleX, scaleY);
            var destRect = new RectangleF(
                bounds.Left,
                bounds.Top,
                screenshot.Width  * scale,
                screenshot.Height * scale);
            pe.Graphics!.DrawImage(screenshot, destRect);
            pe.HasMorePages = false;
        };

        using var previewDialog = new PrintPreviewDialog
        {
            Document = printDoc,
            StartPosition = FormStartPosition.CenterParent,
            Width  = 900,
            Height = 700,
            Text   = "Screen Print Preview — form captured via CopyFromScreen",
        };

        // ShowDialog is synchronous — ShowDialog returns only after the dialog
        // (and any printing triggered from within it) has fully completed.
        // Disposing the bitmap in a finally block therefore covers both the
        // preview-only and print-then-close cases without risk of early disposal.
        try
        {
            previewDialog.ShowDialog(this);
        }
        finally
        {
            screenshot.Dispose();
        }
    }

    // ---------------------------------------------------------------------------
    // Direct / Silent GDI print — rasterises HTML via WebView→PDF→raster and
    // calls PrintDocument.Print() directly — no preview, no dialog.
    // The OS sends output to the system default printer immediately.
    //
    // This demonstrates calling PrintDocument.Print() without any UI, which is
    // the pattern used in both:
    //   • https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-windows-form
    //   • https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-text-document
    // ---------------------------------------------------------------------------

    private async void btnDirectPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnDirectPrint.Enabled = false;
        btnDirectPrint.Text    = "Generating PDF…";

        var pages = new List<Bitmap>();
        try
        {
            pages = await RasterizeToPagesAsync(html);

            int pageIndex = 0;
            using var printDoc = new PrintDocument { DocumentName = "Direct GDI Print" };
            printDoc.BeginPrint += (s, pe) => pageIndex = 0;
            printDoc.PrintPage  += (s, pe) =>
            {
                var bmp    = pages[pageIndex++];
                var bounds = pe.MarginBounds;
                float fit  = Math.Min((float)bounds.Width / bmp.Width, (float)bounds.Height / bmp.Height);
                pe.Graphics!.DrawImage(bmp, new RectangleF(bounds.Left, bounds.Top, bmp.Width * fit, bmp.Height * fit));
                pe.HasMorePages = pageIndex < pages.Count;
            };

            // Print() goes straight to the default printer — no dialog, no preview.
            printDoc.Print();
            MessageBox.Show(
                "Document sent to the default printer.",
                "Direct Print",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while printing:\n\n{ex.Message}",
                "Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (var bmp in pages) bmp.Dispose();
            btnDirectPrint.Text    = "Direct Print";
            btnDirectPrint.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Embedded Preview — uses PrintPreviewControl (not PrintPreviewDialog).
    // PrintPreviewControl is a bare WinForms control that can be embedded inside
    // any form, letting you build a fully custom preview UI around it.
    // Rasterises the HTML via the WebView→PDF→raster pipeline for pixel-perfect
    // output.
    //
    // This demonstrates the PrintPreviewControl approach from:
    //   https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview
    // ---------------------------------------------------------------------------

    private async void btnEmbeddedPreview_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnEmbeddedPreview.Enabled = false;
        btnEmbeddedPreview.Text    = "Generating PDF…";

        var pages = new List<Bitmap>();
        try
        {
            pages = await RasterizeToPagesAsync(html);

            int pageIndex = 0;
            var printDoc = new PrintDocument { DocumentName = "Embedded Preview — GDI" };
            printDoc.BeginPrint += (s, pe) => pageIndex = 0;
            printDoc.PrintPage  += (s, pe) =>
            {
                var bmp    = pages[pageIndex++];
                var bounds = pe.MarginBounds;
                float fit  = Math.Min((float)bounds.Width / bmp.Width, (float)bounds.Height / bmp.Height);
                pe.Graphics!.DrawImage(bmp, new RectangleF(bounds.Left, bounds.Top, bmp.Width * fit, bmp.Height * fit));
                pe.HasMorePages = pageIndex < pages.Count;
            };

            // PrintPreviewControlForm owns and disposes printDoc (in its Dispose override).
            var previewForm = new PrintPreviewControlForm(printDoc);
            try
            {
                previewForm.ShowDialog(this);
            }
            finally
            {
                previewForm.Dispose();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred:\n\n{ex.Message}",
                "Embedded Preview Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (var bmp in pages) bmp.Dispose();
            btnEmbeddedPreview.Text    = "Embedded Preview";
            btnEmbeddedPreview.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // GDI PDF Print — generates a PDF from the current HTML content via
    // WebView2's PrintToPdfStreamAsync, then rasterises every page and shows
    // them in PrintPreviewDialog so the user can review and send to any printer.
    //
    // This is the WebView→PDF→raster→GDI path: it produces pixel-perfect output
    // (full CSS/image fidelity via Chromium) while still routing through the
    // WinForms printing stack, giving the host app full control over the printer
    // dialog and page settings — unlike the PDF Print button which stays entirely
    // inside the in-app WebView2 viewer.
    // ---------------------------------------------------------------------------

    private async void btnPrintPdfFile_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnPrintPdfFile.Enabled = false;
        btnPrintPdfFile.Text    = "Generating PDF…";

        var pages = new List<Bitmap>();
        try
        {
            pages = await RasterizeToPagesAsync(html);

            int pageIndex = 0;
            using var printDoc = new PrintDocument { DocumentName = "GDI PDF Print" };
            printDoc.BeginPrint += (s, pe) => pageIndex = 0;
            printDoc.PrintPage  += (s, pe) =>
            {
                var bmp    = pages[pageIndex++];
                var bounds = pe.MarginBounds;
                float fit  = Math.Min((float)bounds.Width / bmp.Width, (float)bounds.Height / bmp.Height);
                pe.Graphics!.DrawImage(bmp, new RectangleF(bounds.Left, bounds.Top, bmp.Width * fit, bmp.Height * fit));
                pe.HasMorePages = pageIndex < pages.Count;
            };

            using var previewDialog = new PrintPreviewDialog
            {
                Document      = printDoc,
                StartPosition = FormStartPosition.CenterParent,
                Width         = 900,
                Height        = 700,
                Text          = "GDI PDF Print Preview — WebView → PDF → raster → GDI",
            };
            previewDialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred:\n\n{ex.Message}",
                "GDI PDF Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (var bmp in pages) bmp.Dispose();
            btnPrintPdfFile.Text    = "GDI PDF Print";
            btnPrintPdfFile.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Shared helper: render HTML in WebView2, export as PDF stream, rasterise
    // every page at 150 DPI via Windows.Data.Pdf and return the page bitmaps.
    // The caller is responsible for disposing each Bitmap in the returned list.
    // ---------------------------------------------------------------------------

    private async Task<List<Bitmap>> RasterizeToPagesAsync(string html)
    {
        string? tempPdf = null;
        try
        {
            // Step 1: render the HTML in the hidden WebView2 and export as PDF.
            var navTask = WaitForNavigationAsync(_webView!);
            _webView!.NavigateToString(html);
            await navTask;

            using var pdfStream = await _webView.CoreWebView2.PrintToPdfStreamAsync(null);

            if (pdfStream == null || pdfStream.Length == 0)
                throw new InvalidOperationException("The PDF could not be generated.");

            tempPdf = Path.Combine(
                Path.GetTempPath(),
                $"WinFormsPrintSample_{Guid.NewGuid():N}.pdf");

            using (var fileStream = File.Create(tempPdf))
            {
                await pdfStream.CopyToAsync(fileStream);
            }

            // Step 2: rasterise every page with the Windows.Data.Pdf WinRT API.
            var storageFile = await StorageFile.GetFileFromPathAsync(tempPdf);
            var pdfDoc      = await PdfDocument.LoadFromFileAsync(storageFile);

            // Render at 150 DPI for crisp preview.
            // WinRT reports page sizes in DIPs (96 dpi equivalent), so we scale up.
            const double renderDpi   = 150.0;
            const double dipsPerInch = 96.0;
            double       scale       = renderDpi / dipsPerInch;

            var pages = new List<Bitmap>();
            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using var page = pdfDoc.GetPage(i);

                var renderOptions = new PdfPageRenderOptions
                {
                    DestinationWidth  = (uint)(page.Size.Width  * scale),
                    DestinationHeight = (uint)(page.Size.Height * scale),
                };

                using var ras = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(ras, renderOptions);

                // PNG is fully decoded by Bitmap's constructor; the stream can be
                // disposed once construction returns.
                ras.Seek(0);
                using var dotNetStream = ras.AsStreamForRead();
                pages.Add(new Bitmap(dotNetStream));
            }

            return pages;
        }
        finally
        {
            if (tempPdf is not null)
            {
                try { File.Delete(tempPdf); } catch { /* ignore */ }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // Print Dialog — shows the standard Windows printer-picker (PrintDialog) so the
    // user can choose a printer, number of copies, and page range; then drives
    // PrintDocument.Print() with those settings.  No preview is shown — the output
    // goes straight to whichever printer the user selected.  HTML is rasterised
    // via the WebView→PDF→raster pipeline for pixel-perfect output.
    //
    // PrintDialog is distinct from the OS "System Print" dialog shown by WebView2:
    // here the host app owns the PrintDocument and all rendering logic, giving it
    // full control over fonts, layout, and pagination.
    //
    // Demonstrates the PrintDialog usage described in:
    //   https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-display-print-dialog
    // ---------------------------------------------------------------------------

    private async void btnPrintDialog_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnPrintDialog.Enabled = false;
        btnPrintDialog.Text    = "Generating PDF…";

        var pages = new List<Bitmap>();
        try
        {
            pages = await RasterizeToPagesAsync(html);

            int pageIndex = 0;
            using var printDoc = new PrintDocument { DocumentName = "PrintDialog GDI Print" };
            printDoc.BeginPrint += (s, pe) => pageIndex = 0;
            printDoc.PrintPage  += (s, pe) =>
            {
                var bmp    = pages[pageIndex++];
                var bounds = pe.MarginBounds;
                float fit  = Math.Min((float)bounds.Width / bmp.Width, (float)bounds.Height / bmp.Height);
                pe.Graphics!.DrawImage(bmp, new RectangleF(bounds.Left, bounds.Top, bmp.Width * fit, bmp.Height * fit));
                pe.HasMorePages = pageIndex < pages.Count;
            };

            using var printDialog = new PrintDialog
            {
                Document         = printDoc,
                AllowSomePages   = true,
                AllowCurrentPage = true,
            };

            if (printDialog.ShowDialog(this) != DialogResult.OK) return;

            printDoc.Print();
            MessageBox.Show(
                $"Document sent to \"{printDoc.PrinterSettings.PrinterName}\".",
                "PrintDialog — Sent to Printer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while printing:\n\n{ex.Message}",
                "Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (var bmp in pages) bmp.Dispose();
            btnPrintDialog.Text    = "Print Dialog";
            btnPrintDialog.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Page Setup Dialog — shows PageSetupDialog so the user can configure paper
    // size, orientation, and margins before printing; then opens PrintPreviewDialog
    // so the user can review the layout with those settings applied.
    //
    // PageSetupDialog is one of the three standard WinForms printing dialogs
    // (alongside PrintDialog and PrintPreviewDialog).  Showing it first lets the
    // user tune the page layout before committing to a print.
    //
    // Demonstrates the PageSetupDialog usage described in:
    //   https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-create-standard-windows-forms-print-jobs
    // ---------------------------------------------------------------------------

    private void btnPageSetup_Click(object sender, EventArgs e)
    {
        string html = txtHtmlContent.Text;
        if (string.IsNullOrWhiteSpace(html))
        {
            MessageBox.Show(
                "Please enter some HTML content to print.",
                "Empty Content",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        string plainText = Regex.Replace(html, "<[^>]+>", " ");
        plainText = Regex.Replace(plainText, @"\s{2,}", " ").Trim();

        // BeginPrint rebuilds the queue before each print job; the empty initial
        // value satisfies the compiler's definite-assignment rule for the closure.
        Queue<string> lines = new Queue<string>();

        using var printDoc = new PrintDocument { DocumentName = "PageSetup GDI Print" };

        // 100 characters per line matches the width used by the other GDI methods.
        // Re-build the queue at the start of every print job so the preview pass
        // and the subsequent actual-print pass both receive all pages.
        printDoc.BeginPrint += (s, pe) =>
            lines = new Queue<string>(WrapText(plainText, 100));

        printDoc.PrintPage += (s, pe) =>
        {
            using var font = new Font("Courier New", 10f);
            float lineHeight = font.GetHeight(pe.Graphics!);
            float y = pe.MarginBounds.Top;

            while (lines.Count > 0 && y + lineHeight <= pe.MarginBounds.Bottom)
            {
                pe.Graphics!.DrawString(lines.Dequeue(), font, Brushes.Black, pe.MarginBounds.Left, y);
                y += lineHeight;
            }

            pe.HasMorePages = lines.Count > 0;
        };

        // Step 1: PageSetupDialog — user tunes paper size, orientation, margins.
        using var pageSetupDialog = new PageSetupDialog
        {
            Document         = printDoc,
            AllowMargins     = true,
            AllowOrientation = true,
            AllowPaper       = true,
        };

        if (pageSetupDialog.ShowDialog(this) != DialogResult.OK) return;

        // Step 2: PrintPreviewDialog — user reviews the layout with the chosen
        // page settings and can click its own Print button to send to a printer.
        using var previewDialog = new PrintPreviewDialog
        {
            Document      = printDoc,
            StartPosition = FormStartPosition.CenterParent,
            Width         = 900,
            Height        = 700,
            Text          = "Print Preview — custom page settings applied",
        };
        previewDialog.ShowDialog(this);
    }

    // ---------------------------------------------------------------------------
    private bool TryGetHtml(out string html)
    {
        html = string.Empty;

        if (!_webViewReady || _webView is null)
        {
            MessageBox.Show(
                "WebView2 is not ready yet. Please wait a moment and try again.",
                "Not Ready",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        html = txtHtmlContent.Text;
        if (string.IsNullOrWhiteSpace(html))
        {
            MessageBox.Show(
                "Please enter some HTML content to print.",
                "Empty Content",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    // ---------------------------------------------------------------------------
    // Helper: await NavigationCompleted on the WebView
    // ---------------------------------------------------------------------------

    private static Task WaitForNavigationAsync(WebView2 webView)
    {
        var tcs = new TaskCompletionSource<bool>();

        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            webView.CoreWebView2.NavigationCompleted -= Handler;
            tcs.TrySetResult(true);
        }

        webView.CoreWebView2.NavigationCompleted += Handler;
        return tcs.Task;
    }
}
