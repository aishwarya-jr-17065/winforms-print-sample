using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WinFormsPrintSample;

/// <summary>
/// A preview window that renders HTML in a fully visible WebView2, giving the
/// user a visual preview of the document before printing, and then invokes
/// the native Windows system print dialog (<c>PrintDlgEx</c>) when the user
/// clicks Print.
///
/// <para>
/// The Windows system dialog (<c>CoreWebView2PrintDialogKind.System</c>) has
/// no built-in preview pane. The WebView2 pane in this form acts as the
/// visual preview, while still handing off the actual printer / settings
/// selection to the familiar native OS dialog.
/// </para>
/// </summary>
internal sealed class SystemPrintPreviewForm : Form
{
    private readonly string _html;
    private readonly CoreWebView2Environment _env;
    private readonly WebView2 _webView;
    private readonly Button _btnPrint;
    private readonly Label _lblStatus;
    private bool _ready;

    public SystemPrintPreviewForm(string html, CoreWebView2Environment env)
    {
        _html = html;
        _env = env;

        // ── Form ──────────────────────────────────────────────────────────
        Text = "System Print Preview";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(960, 720);
        MinimumSize = new Size(640, 480);

        // ── Info banner ───────────────────────────────────────────────────
        var pnlInfo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(219, 234, 254),  // light blue
        };
        var lblInfo = new Label
        {
            Text = "ℹ  The WebView2 pane below is a visual preview. Click Print… to open the Windows system print dialog.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(30, 64, 175),
        };
        pnlInfo.Controls.Add(lblInfo);

        // ── Bottom panel ──────────────────────────────────────────────────
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8),
        };

        _lblStatus = new Label
        {
            Text = "Loading…",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SystemColors.GrayText,
        };

        _btnPrint = new Button
        {
            Text = "🖨  Print (System)…",
            Size = new Size(160, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
            Enabled = false,
        };
        _btnPrint.Click += OnPrintClick;

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
        Controls.Add(pnlInfo);

        Load += OnFormLoad;
    }

    // -----------------------------------------------------------------------
    // Form load: initialise WebView2 with the shared environment, then navigate
    // -----------------------------------------------------------------------

    private async void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            await _webView.EnsureCoreWebView2Async(_env);

            // Force light mode to prevent Chromium auto-darkening the preview.
            _webView.DefaultBackgroundColor = Color.White;
            _webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;

            var tcs = new TaskCompletionSource<bool>();
            void Handler(object? s, CoreWebView2NavigationCompletedEventArgs args)
            {
                _webView.CoreWebView2.NavigationCompleted -= Handler;
                tcs.TrySetResult(true);
            }
            _webView.CoreWebView2.NavigationCompleted += Handler;
            _webView.NavigateToString(_html);

            await tcs.Task;

            _ready = true;
            _lblStatus.Text = "Preview ready — click Print… to open the system print dialog.";
            _btnPrint.Enabled = true;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "WebView2 failed to load.";
            MessageBox.Show(
                $"Failed to initialise WebView2:\n\n{ex.Message}",
                "WebView2 Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // -----------------------------------------------------------------------
    // Print button: open the native Windows system print dialog
    // -----------------------------------------------------------------------

    private void OnPrintClick(object? sender, EventArgs e)
    {
        if (!_ready) return;

        // The WebView2 pane above already serves as the visual preview.
        // ShowPrintUI with System opens the OS-level PrintDlgEx dialog for
        // printer selection and settings — the same dialog as the "System Print"
        // button on the main form, but now shown after the user has seen the
        // rendered preview.
        _webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
    }

    // -----------------------------------------------------------------------
    // Cleanup
    // -----------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _webView.Dispose();

        base.Dispose(disposing);
    }
}
