using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WinFormsPrintSample;

/// <summary>
/// Main application form. Provides a multiline HTML editor and a Print button
/// that renders the HTML content with high fidelity using WebView2 (Chromium).
/// </summary>
public partial class MainForm : Form
{
    private WebView2? _webView;
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
            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: Path.Combine(Path.GetTempPath(), "WinFormsPrintSample"),
                options: null);

            await _webView.EnsureCoreWebView2Async(env);
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
    // Print button handler
    // ---------------------------------------------------------------------------

    private async void btnPrint_Click(object sender, EventArgs e)
    {
        if (!_webViewReady || _webView is null)
        {
            MessageBox.Show(
                "WebView2 is not ready yet. Please wait a moment and try again.",
                "Not Ready",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

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

        btnPrint.Enabled = false;
        btnPrint.Text = "Printing…";

        try
        {
            // Load the HTML into the (hidden) WebView2 and wait for it to finish.
            var navTask = WaitForNavigationAsync(_webView);
            _webView.NavigateToString(html);
            await navTask;

            // Show the system print dialog so the user can choose printer / settings.
            // CoreWebView2.PrintAsync renders via Chromium, giving pixel-perfect output.
            await _webView.CoreWebView2.PrintAsync(printSettings: null);
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
            btnPrint.Text = "Print";
            btnPrint.Enabled = true;
        }
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
