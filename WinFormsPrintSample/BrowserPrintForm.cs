using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WinFormsPrintSample;

/// <summary>
/// A preview window that renders HTML in a fully visible WebView2 and lets
/// the user invoke Chromium's browser print dialog (which includes a live
/// print preview). Because the WebView2 here is visible and properly sized,
/// Chromium can render the preview — unlike a hidden/off-screen control.
/// </summary>
internal sealed class BrowserPrintForm : Form
{
    private readonly string _html;
    private readonly CoreWebView2Environment _env;
    private readonly WebView2 _webView;
    private readonly Button _btnPrint;
    private readonly Label _lblStatus;
    private bool _ready;
    private bool _afterprintHooked;

    public BrowserPrintForm(string html, CoreWebView2Environment env)
    {
        _html = html;
        _env = env;

        // ── Form properties ───────────────────────────────────────────────
        Text = "Browser Print Preview";
        Size = new Size(960, 720);
        MinimumSize = new Size(640, 480);
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;
        KeyDown += OnFormKeyDown;

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
            Text = "🖨  Print…",
            Size = new Size(110, 36),
            Dock = DockStyle.Right,   // dock keeps button visible regardless of panel width
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
            BackColor = Color.White,   // shown before CoreWebView2 initialises; prevents dark-mode flash
        };

        Controls.Add(_webView);      // added first — behind the bottom panel
        Controls.Add(pnlBottom);

        // ── Wire up load event ────────────────────────────────────────────
        Load += OnFormLoad;
    }

    // -----------------------------------------------------------------------
    // Form load: initialise WebView2 with the shared environment, then navigate
    // -----------------------------------------------------------------------

    private async void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            // Reuse the environment (and its user-data folder) from MainForm so
            // both WebView2 instances share the same Chromium process group.
            await _webView.EnsureCoreWebView2Async(_env);

            // Force light mode regardless of the system theme.
            // DefaultBackgroundColor covers the area under HTML content;
            // PreferredColorScheme stops Chromium auto-darkening the content itself.
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

            // Inject a window.onafterprint listener so that when the browser
            // print dialog is closed (after printing or cancellation) the host
            // is notified via the WebView2 message channel.
            if (!_afterprintHooked)
            {
                _afterprintHooked = true;
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            }
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.onafterprint = () => window.chrome.webview.postMessage('afterprint');");

            _ready = true;
            _lblStatus.Text = "Ready — click Print… to open the browser print dialog.";
            _btnPrint.Enabled = true;

            // Automatically open the browser print dialog once content is loaded.
            _webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
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
    // Print button: open Chromium's browser print dialog (includes preview)
    // -----------------------------------------------------------------------

    private void OnPrintClick(object? sender, EventArgs e)
    {
        if (!_ready) return;

        // Because _webView is visible and full-size, Chromium can render the
        // print preview correctly — no "This app doesn't support print preview".
        _webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
    }

    // -----------------------------------------------------------------------
    // WebMessage handler: close the form when window.onafterprint fires
    // -----------------------------------------------------------------------

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "afterprint" && IsHandleCreated && !IsDisposed)
        {
            // Marshal back to the UI thread in case the event arrives off-thread.
            // BeginInvoke(Close);
        }
    }

    // -----------------------------------------------------------------------
    // Key handler: Escape closes the full-screen form
    // -----------------------------------------------------------------------

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    // -----------------------------------------------------------------------
    // Cleanup
    // -----------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_webView.CoreWebView2 is not null)
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;

            _webView.Dispose();
        }

        base.Dispose(disposing);
    }
}
