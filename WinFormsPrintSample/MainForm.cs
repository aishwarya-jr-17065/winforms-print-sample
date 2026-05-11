using System.Diagnostics;
using System.Drawing.Printing;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

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
    // System print with preview — opens a visible WebView2 preview window that
    // lets the user see the rendered HTML, then opens the system print dialog.
    // ---------------------------------------------------------------------------

    private void btnSystemPreviewPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        using var previewForm = new SystemPrintPreviewForm(html, _env!);
        previewForm.ShowDialog(this);
    }

    // ---------------------------------------------------------------------------
    // WinForms GDI print — uses PrintDocument + PrintPreviewDialog.
    // Strips HTML tags and renders the plain text via GDI Graphics.
    // This is the classic WinForms printing path, independent of WebView2.
    // ---------------------------------------------------------------------------

    private void btnGdiPrint_Click(object sender, EventArgs e)
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

        // Strip HTML tags so the GDI renderer sees plain readable text.
        string plainText = Regex.Replace(html, "<[^>]+>", " ");
        plainText = Regex.Replace(plainText, @"\s{2,}", " ").Trim();

        var lines = new Queue<string>(WrapText(plainText, 100));

        var printDoc = new PrintDocument { DocumentName = "WinForms GDI Print" };
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

        using var previewDialog = new PrintPreviewDialog
        {
            Document = printDoc,
            StartPosition = FormStartPosition.CenterParent,
            Width = 900,
            Height = 700,
            Text = "GDI Print Preview — HTML rendered as plain text",
        };
        previewDialog.ShowDialog(this);
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
    // PDF print — generates a PDF from the HTML via WebView2's PrintToPdfAsync,
    // then opens it with the system default PDF viewer so the user can print.
    // ---------------------------------------------------------------------------

    private async void btnPdfPrint_Click(object sender, EventArgs e)
    {
        if (!TryGetHtml(out string html)) return;

        btnPdfPrint.Enabled = false;
        btnPdfPrint.Text = "Generating PDF…";

        try
        {
            var navTask = WaitForNavigationAsync(_webView!);
            _webView!.NavigateToString(html);
            await navTask;

            string tempPdf = Path.Combine(
                Path.GetTempPath(),
                $"WinFormsPrintSample_{Guid.NewGuid():N}.pdf");

            await _webView.CoreWebView2.PrintToPdfAsync(tempPdf, null);

            if (!File.Exists(tempPdf))
            {
                MessageBox.Show(
                    "The PDF could not be generated.",
                    "PDF Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Open the PDF with the system default application (e.g. Edge, Adobe Reader).
            // The user can review the PDF and print from within the viewer.
            Process.Start(new ProcessStartInfo(tempPdf) { UseShellExecute = true });

            // Best-effort cleanup: delete the temp file after 5 minutes, giving the
            // PDF viewer enough time to fully load the file before it is removed.
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
            {
                try { File.Delete(tempPdf); } catch { /* ignore — OS temp dir is cleaned periodically */ }
            });
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
            btnPdfPrint.Text = "PDF Print";
            btnPdfPrint.Enabled = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Shared validation helper
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
