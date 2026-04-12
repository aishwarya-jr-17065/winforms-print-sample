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
