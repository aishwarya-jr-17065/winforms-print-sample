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
    private System.Windows.Forms.Button btnPrint;
    private System.Windows.Forms.Panel pnlBottom;

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
                "  <p>This HTML will be printed with high fidelity using WebView2 (Chromium).</p>\r\n" +
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

        btnPrint = new System.Windows.Forms.Button
        {
            Text = "Print",
            Size = new Size(100, 36),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Font = new Font("Segoe UI", 10f),
        };
        // Position within the bottom panel (right-aligned)
        btnPrint.Location = new Point(pnlBottom.Width - btnPrint.Width - 12, 8);
        btnPrint.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        btnPrint.Click += btnPrint_Click;

        pnlBottom.Controls.Add(btnPrint);

        // ── Form ─────────────────────────────────────────────────────────────
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 490);
        MinimumSize = new Size(640, 400);
        Text = "WinForms HTML Print Sample";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblHtmlContent);
        Controls.Add(txtHtmlContent);
        Controls.Add(pnlBottom);
    }

    #endregion
}
