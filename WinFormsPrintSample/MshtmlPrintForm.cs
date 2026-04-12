namespace WinFormsPrintSample;

/// <summary>
/// Renders HTML using the legacy MSHTML (Trident) WebBrowser control and
/// invokes its print dialog via <c>ShowPrintDialog()</c>.
///
/// HISTORICAL NOTE: In the Internet Explorer era, MSHTML's print dialog
/// included a live preview pane driven by Trident's rendering pipeline —
/// the same preview you saw when pressing Ctrl+P in IE. This made it the
/// only built-in WinForms option for print preview with HTML content.
///
/// CURRENT REALITY: IE's UI shell (including the preview renderer) has been
/// removed on Windows 10 (post-2022 cumulative updates) and Windows 11.
/// <c>ShowPrintDialog()</c> now falls through to the plain OS PrintDlgEx
/// dialog — no preview, identical behaviour to WebView2's System dialog.
///
/// CONCLUSION: On any supported Windows version today, MSHTML offers no
/// advantage over the system dialog. The WebView2 browser dialog
/// (<c>CoreWebView2PrintDialogKind.Browser</c>) is the only path to a real
/// print preview in a modern WinForms application.
///
/// This class is kept for historical comparison only. Do not use it in
/// production — prefer <see cref="BrowserPrintForm"/> instead.
/// </summary>
internal sealed class MshtmlPrintForm : Form
{
    private readonly string _html;
    private readonly WebBrowser _webBrowser;
    private readonly Button _btnPrint;
    private readonly Label _lblStatus;

    public MshtmlPrintForm(string html)
    {
        _html = html;

        // ── Form ─────────────────────────────────────────────────────────
        Text = "MSHTML Print Preview";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(960, 720);
        MinimumSize = new Size(640, 480);

        // ── Warning banner ────────────────────────────────────────────────
        var pnlWarning = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(255, 244, 206),  // amber warning colour
        };
        var lblWarning = new Label
        {
            Text = "⚠  MSHTML (Internet Explorer engine) is deprecated. Print preview is not available on Windows 10 (post-2022) or Windows 11.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 61, 0),
        };
        pnlWarning.Controls.Add(lblWarning);

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
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
            Enabled = false,
        };
        _btnPrint.Click += OnPrintClick;

        pnlBottom.Controls.Add(_btnPrint);
        pnlBottom.Controls.Add(_lblStatus);

        // ── WebBrowser (MSHTML) ───────────────────────────────────────────
        _webBrowser = new WebBrowser
        {
            Dock = DockStyle.Fill,
            ScriptErrorsSuppressed = true,
            WebBrowserShortcutsEnabled = false,
        };
        _webBrowser.DocumentCompleted += OnDocumentCompleted;

        // Controls added in z-order: Fill first, then Top/Bottom panels on top.
        Controls.Add(_webBrowser);
        Controls.Add(pnlBottom);
        Controls.Add(pnlWarning);

        Load += OnFormLoad;
    }

    // -----------------------------------------------------------------------

    private void OnFormLoad(object? sender, EventArgs e)
    {
        // DocumentText setter navigates to "about:blank" first, then sets content.
        // DocumentCompleted fires when the content is fully parsed.
        _webBrowser.DocumentText = _html;
    }

    private void OnDocumentCompleted(object? sender, WebBrowserDocumentCompletedEventArgs e)
    {
        _btnPrint.Enabled = true;
        _lblStatus.Text = "Ready — Print… opens the system dialog. Preview is not available on modern Windows.";
    }

    private void OnPrintClick(object? sender, EventArgs e)
    {
        // ShowPrintDialog() historically opened IE's own print dialog with a
        // live preview pane — the preview was powered by Trident's rendering
        // pipeline, which had direct access to the laid-out document.
        //
        // On Windows 10 (post-2022) and Windows 11 the IE UI shell is gone.
        // The call now falls through to the plain OS PrintDlgEx dialog with
        // no preview — identical to CoreWebView2PrintDialogKind.System.
        //
        // For a real print preview in a modern WinForms app, use
        // BrowserPrintForm (CoreWebView2PrintDialogKind.Browser) instead.
        _webBrowser.ShowPrintDialog();
    }

    // -----------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _webBrowser.Dispose();

        base.Dispose(disposing);
    }
}
