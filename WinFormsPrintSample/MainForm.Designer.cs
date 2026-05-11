namespace WinFormsPrintSample;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    // Controls
    private System.Windows.Forms.Label lblHtmlContent;
    private System.Windows.Forms.TextBox txtHtmlContent;
    private System.Windows.Forms.Button btnSystemPrint;
    private System.Windows.Forms.Button btnBrowserPrint;
    private System.Windows.Forms.Button btnMshtmlPrint;
    private System.Windows.Forms.Panel pnlBottom;
    private System.Windows.Forms.Panel pnlBottom2;
    private System.Windows.Forms.Button btnGdiPrint;
    private System.Windows.Forms.Button btnPdfPrint;
    private System.Windows.Forms.Button btnScreenPrint;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _webView?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support – do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // ── Label ────────────────────────────────────────────────────────────
        lblHtmlContent = new System.Windows.Forms.Label
        {
            Text = "HTML Content:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Regular),
            Location = new Point(12, 12),
        };

        // ── TextBox (multiline HTML editor) ──────────────────────────────────
        txtHtmlContent = new System.Windows.Forms.TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 10f),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(12, 36),
            Size = new Size(776, 370),
            Text =
                "<!DOCTYPE html>\r\n" +
                "<html>\r\n" +
                "<head>\r\n" +
                "  <meta charset=\"utf-8\" />\r\n" +
                "  <style>\r\n" +
                "    body { font-family: Arial, sans-serif; margin: 2cm; }\r\n" +
                "    h1   { color: #2c3e50; }\r\n" +
                "    p    { line-height: 1.6; }\r\n" +
                "  </style>\r\n" +
                "</head>\r\n" +
                "<body>\r\n" +
                "  <h1>Hello, World!</h1>\r\n" +
                "  <p>This HTML will be printed using WebView2 (Chromium).</p>\r\n" +
                "</body>\r\n" +
                "</html>",
        };

        // ── Bottom panel (holds the Print button) ─────────────────────────────
        pnlBottom = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8),
        };

        // System Print button — docked right (rightmost)
        btnSystemPrint = new System.Windows.Forms.Button
        {
            Text = "System Print",
            Size = new Size(120, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnSystemPrint.Click += btnSystemPrint_Click;

        // Browser Print button — docked right (middle)
        btnBrowserPrint = new System.Windows.Forms.Button
        {
            Text = "Browser Print",
            Size = new Size(130, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnBrowserPrint.Click += btnBrowserPrint_Click;

        // MSHTML Print button — docked right (leftmost of the three)
        btnMshtmlPrint = new System.Windows.Forms.Button
        {
            Text = "MSHTML Print",
            Size = new Size(130, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnMshtmlPrint.Click += btnMshtmlPrint_Click;

        // Controls are added right-to-left when DockStyle.Right is used:
        // first added = rightmost. So add System first, then Browser, then MSHTML.
        pnlBottom.Controls.Add(btnSystemPrint);
        pnlBottom.Controls.Add(btnBrowserPrint);
        pnlBottom.Controls.Add(btnMshtmlPrint);

        // ── Second row of print buttons ───────────────────────────────────────
        // PDF Print button — rightmost
        btnPdfPrint = new System.Windows.Forms.Button
        {
            Text = "PDF Print",
            Size = new Size(110, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnPdfPrint.Click += btnPdfPrint_Click;

        // GDI Print button
        btnGdiPrint = new System.Windows.Forms.Button
        {
            Text = "GDI Print",
            Size = new Size(110, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnGdiPrint.Click += btnGdiPrint_Click;

        // Screen Print button — captures the form via CopyFromScreen (MS docs approach)
        btnScreenPrint = new System.Windows.Forms.Button
        {
            Text = "Screen Print",
            Size = new Size(120, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnScreenPrint.Click += btnScreenPrint_Click;

        pnlBottom2 = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8),
        };

        // Add right-to-left: first added = rightmost.
        pnlBottom2.Controls.Add(btnPdfPrint);
        pnlBottom2.Controls.Add(btnGdiPrint);
        pnlBottom2.Controls.Add(btnScreenPrint);

        // ── Form ─────────────────────────────────────────────────────────────
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 542);
        MinimumSize = new Size(640, 452);
        Text = "WinForms HTML Print Sample";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblHtmlContent);
        Controls.Add(txtHtmlContent);
        // pnlBottom2 must be added before pnlBottom so that WinForms docking
        // places pnlBottom at the very bottom edge and pnlBottom2 above it.
        Controls.Add(pnlBottom2);
        Controls.Add(pnlBottom);
    }

    #endregion
}
