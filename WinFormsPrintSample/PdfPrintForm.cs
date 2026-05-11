using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WinFormsPrintSample;

/// <summary>
/// Opens a generated PDF inside a visible WebView2 so the user can review and
/// print it without leaving the application. Chromium's built-in PDF renderer
/// displays the document; clicking Print… opens the browser print dialog.
/// The temporary PDF file is deleted when this form is closed.
/// </summary>
internal sealed class PdfPrintForm : Form
{
    private readonly string _pdfPath;
    private readonly CoreWebView2Environment _env;
    private readonly WebView2 _webView;
    private readonly Button _btnPrint;
    private readonly Label _lblStatus;
    private bool _ready;

    public PdfPrintForm(string pdfPath, CoreWebView2Environment env)
    {
        _pdfPath = pdfPath;
        _env = env;

        // ── Form ──────────────────────────────────────────────────────────
        Text = "PDF Preview";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(960, 720);
        MinimumSize = new Size(640, 480);

        // ── Bottom panel ──────────────────────────────────────────────────
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8),
        };

        _lblStatus = new Label
        {
            Text = "Loading PDF…",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SystemColors.GrayText,
        };

        _btnPrint = new Button
        {
            Text = "🖨  Print…",
            Size = new Size(110, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
            Enabled = false,
        };
        _btnPrint.Click += OnPrintClick;

        // Controls added right-to-left when using DockStyle.Right: button first, then label.
        pnlBottom.Controls.Add(_btnPrint);
        pnlBottom.Controls.Add(_lblStatus);

        // ── WebView2 (fills remaining area) ───────────────────────────────
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
        };

        Controls.Add(_webView);
        Controls.Add(pnlBottom);

        Load += OnFormLoad;
    }

    // -----------------------------------------------------------------------
    // Form load: initialise WebView2 with the shared environment, navigate to PDF
    // -----------------------------------------------------------------------

    private async void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            await _webView.EnsureCoreWebView2Async(_env);

            var tcs = new TaskCompletionSource<bool>();
            void Handler(object? s, CoreWebView2NavigationCompletedEventArgs args)
            {
                _webView.CoreWebView2.NavigationCompleted -= Handler;
                tcs.TrySetResult(true);
            }
            _webView.CoreWebView2.NavigationCompleted += Handler;

            // Navigate to the PDF using a file:// URI — WebView2 renders PDFs natively.
            _webView.CoreWebView2.Navigate(new Uri(_pdfPath).AbsoluteUri);

            await tcs.Task;

            _ready = true;
            _lblStatus.Text = "PDF ready — click Print… to open the print dialog.";
            _btnPrint.Enabled = true;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Failed to load PDF.";
            MessageBox.Show(
                $"Failed to load the PDF:\n\n{ex.Message}",
                "PDF Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // -----------------------------------------------------------------------
    // Print button: open Chromium's browser print dialog
    // -----------------------------------------------------------------------

    private void OnPrintClick(object? sender, EventArgs e)
    {
        if (!_ready) return;

        // The WebView2 is visible and full-size, so Chromium renders the print
        // preview correctly — showing the actual PDF content.
        _webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
    }

    // -----------------------------------------------------------------------
    // Cleanup: delete the temp PDF file when the form is closed
    // -----------------------------------------------------------------------

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        try { File.Delete(_pdfPath); } catch { /* ignore — best effort */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _webView.Dispose();

        base.Dispose(disposing);
    }
}
