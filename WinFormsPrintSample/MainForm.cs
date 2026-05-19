using System.Drawing.Printing;
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
            // Zero out printer margins so the rasterised bitmap fills the full printable
            // area; the HTML content already carries its own visual margins.
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
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
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
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
