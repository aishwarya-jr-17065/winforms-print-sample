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
    private System.Windows.Forms.Button btnGdiPrint;
    private System.Windows.Forms.Button btnEmbeddedPreview;
    private System.Windows.Forms.GroupBox gbOtherPrintOptions;
    private System.Windows.Forms.Panel pnlOtherOptions;

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
            Text = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <style>
    body   { font-family: Arial, sans-serif; margin: 2cm; font-size: 11pt; }
    h1     { color: #2c3e50; border-bottom: 2px solid #2c3e50; padding-bottom: 6px; }
    h2     { color: #34495e; margin-top: 1.4em; }
    p      { line-height: 1.7; text-align: justify; }
    figure { margin: 1.2em 0; text-align: center; }
    figcaption { font-size: 0.9em; color: #666; margin-top: 6px; font-style: italic; }
    table  { border-collapse: collapse; width: 100%; margin: 1em 0; }
    th, td { border: 1px solid #bdc3c7; padding: 8px 12px; text-align: left; }
    th     { background: #2c3e50; color: white; }
    tr:nth-child(even) { background: #f2f3f4; }
    ul     { line-height: 1.8; }
  </style>
</head>
<body>
  <h1>Annual Business Review Report &mdash; FY 2025</h1>
  <p><strong>Prepared by:</strong> Strategy &amp; Analytics Division &nbsp;|&nbsp; <strong>Date:</strong> December 2025</p>

  <h2>1. Executive Summary</h2>
  <p>This report provides a comprehensive analysis of organisational performance for fiscal year 2025. Overall, the company achieved record revenue growth of 28% year-on-year, driven by strong demand in the North and East regions. Operational efficiency improved across all business units, with cost-per-unit falling by 11% compared to the prior year. The leadership team is confident that the strategic initiatives launched in Q3 will yield further improvements throughout FY 2026.</p>
  <p>Customer satisfaction scores reached an all-time high of 4.7 out of 5 in Q4, reflecting the impact of the service-quality programme introduced in January. Net Promoter Score (NPS) rose from 42 to 61 over the course of the year, placing the company in the top quartile of its industry peer group.</p>

  <figure>
    <img src=""data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI1MjAiIGhlaWdodD0iMTgwIj48cmVjdCB3aWR0aD0iNTIwIiBoZWlnaHQ9IjE4MCIgZmlsbD0iIzRhOTBkOSIgcng9IjgiLz48dGV4dCB4PSIyNjAiIHk9Ijc1IiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMjIiIGZvbnQtd2VpZ2h0PSJib2xkIiBmaWxsPSJ3aGl0ZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+RmlndXJlIDE6IFNhbGVzIFBlcmZvcm1hbmNlIENoYXJ0PC90ZXh0Pjx0ZXh0IHg9IjI2MCIgeT0iMTEwIiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMTQiIGZpbGw9IiNkMGU4ZmYiIHRleHQtYW5jaG9yPSJtaWRkbGUiPlExOiAkMS4yTSAgfCAgUTI6ICQxLjhNICB8ICBRMzogJDIuMU0gIHwgIFE0OiAkMi42TTwvdGV4dD48dGV4dCB4PSIyNjAiIHk9IjE0NSIgZm9udC1mYW1pbHk9IkFyaWFsIiBmb250LXNpemU9IjEzIiBmaWxsPSIjYTBkMGZmIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj5Bbm51YWwgVG90YWw6ICQ3LjdNICAoKzI4JSBZb1kpPC90ZXh0Pjwvc3ZnPg=="" width=""520"" height=""180"" alt=""Sales Performance Chart"" />
    <figcaption>Figure 1: Quarterly Sales Performance &mdash; FY 2025 (USD)</figcaption>
  </figure>

  <h2>2. Financial Highlights</h2>
  <p>Total revenue for FY 2025 reached $7.7 million, exceeding the revised forecast of $7.2 million by 6.9%. Gross margin improved to 58.4%, up from 54.1% in FY 2024, as a result of product mix optimisation and renegotiated supplier contracts. Operating expenses grew by only 9% despite a 28% increase in headcount, demonstrating improved operational leverage.</p>
  <table>
    <tr><th>Metric</th><th>FY 2024</th><th>FY 2025</th><th>Change</th></tr>
    <tr><td>Total Revenue</td><td>$6.0M</td><td>$7.7M</td><td>+28.3%</td></tr>
    <tr><td>Gross Margin</td><td>54.1%</td><td>58.4%</td><td>+4.3 pp</td></tr>
    <tr><td>Operating Income</td><td>$0.9M</td><td>$1.5M</td><td>+66.7%</td></tr>
    <tr><td>Net Income</td><td>$0.6M</td><td>$1.1M</td><td>+83.3%</td></tr>
    <tr><td>Headcount (EoY)</td><td>85</td><td>109</td><td>+28.2%</td></tr>
  </table>

  <h2>3. Regional Performance</h2>
  <p>The North region remained the largest contributor to overall revenue, accounting for 32% of the total. Strong demand from enterprise clients in the technology and healthcare verticals drove Q3 and Q4 outperformance. The East region showed the fastest growth rate at 41% YoY, benefitting from two new partnership agreements signed in February.</p>
  <p>The South region grew at a more modest 18%, impacted by increased competitive pressure and the loss of one major account in Q2. Recovery initiatives launched in Q3 are showing early positive results, with pipeline value up 35% entering FY 2026. The West region, while the smallest by revenue, demonstrated the highest customer retention rate of 94%.</p>

  <figure>
    <img src=""data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI1MjAiIGhlaWdodD0iMTgwIj48cmVjdCB3aWR0aD0iNTIwIiBoZWlnaHQ9IjE4MCIgZmlsbD0iIzI3YWU2MCIgcng9IjgiLz48dGV4dCB4PSIyNjAiIHk9Ijc1IiBmb250LWZhbWlseT0iQXJpYWwiIGZvbnQtc2l6ZT0iMjIiIGZvbnQtd2VpZ2h0PSJib2xkIiBmaWxsPSJ3aGl0ZSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+RmlndXJlIDI6IFJlZ2lvbmFsIERpc3RyaWJ1dGlvbiBNYXA8L3RleHQ+PHRleHQgeD0iMjYwIiB5PSIxMTAiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxNCIgZmlsbD0iI2MwZjBkMCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+Tm9ydGg6IDMyJSAgfCAgU291dGg6IDI1JSAgfCAgRWFzdDogMjglICB8ICBXZXN0OiAxNSU8L3RleHQ+PHRleHQgeD0iMjYwIiB5PSIxNDUiIGZvbnQtZmFtaWx5PSJBcmlhbCIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzkwZTBiMCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+Q292ZXJhZ2UgYWNyb3NzIDQ4IHN0YXRlcywgMTIgY291bnRyaWVzPC90ZXh0Pjwvc3ZnPg=="" width=""520"" height=""180"" alt=""Regional Distribution Map"" />
    <figcaption>Figure 2: Revenue Distribution by Region &mdash; FY 2025</figcaption>
  </figure>

  <h2>4. Product Portfolio</h2>
  <p>The product portfolio was streamlined in Q1, reducing the active SKU count from 312 to 248. This rationalisation improved inventory turnover by 23% and reduced warehousing costs by $180,000 annually. Three new flagship products were launched in H2, contributing $420,000 in revenue in their first six months &mdash; 17% above the launch targets.</p>
  <p>The software-as-a-service offering, introduced as a pilot in Q4 FY 2024, expanded to 37 paying customers by the end of FY 2025. Average contract value of $14,200 per year positions this segment for significant growth. The product roadmap for FY 2026 includes two major platform releases and a mobile application currently in beta testing with select enterprise clients.</p>

  <h2>5. Strategic Initiatives &amp; Outlook</h2>
  <p>Three strategic priorities guided decision-making throughout FY 2025: customer-centricity, operational excellence, and digital transformation. Progress against all three was strong. The customer-success function, staffed with seven dedicated managers, achieved a portfolio renewal rate of 91%. Process automation initiatives eliminated approximately 2,400 person-hours of manual work per quarter.</p>
  <p>Looking ahead, management targets revenue of $9.5&ndash;10.2 million for FY 2026, representing growth of 23&ndash;32%. Key assumptions include successful retention of the top-20 accounts (representing 44% of revenue), continued expansion in the East region, and the launch of the new SaaS platform by Q2. Investment in talent will continue, with 24 additional hires planned in Engineering, Sales, and Customer Success.</p>

  <h2>6. Key Risks</h2>
  <ul>
    <li><strong>Competitive pressure:</strong> Two well-funded competitors entered the market in Q4; pricing discipline will be critical in FY 2026.</li>
    <li><strong>Supply chain:</strong> Component lead times remain elevated at 14&ndash;18 weeks; safety-stock levels have been increased accordingly.</li>
    <li><strong>Talent retention:</strong> Engineering attrition of 12% in H2 is above target; enhanced compensation and career-development programmes are being implemented.</li>
    <li><strong>Regulatory:</strong> Pending data-privacy legislation in three key markets may require product modifications by Q3 FY 2026.</li>
    <li><strong>Currency exposure:</strong> Approximately 22% of revenue is denominated in non-USD currencies; a hedging programme is under review.</li>
  </ul>

  <h2>7. Conclusion</h2>
  <p>FY 2025 was a year of significant achievement. The business delivered record revenue, improved margins, and strengthened its customer relationships &mdash; all while navigating a dynamic competitive environment. The foundations laid during this period &mdash; a larger team, a rationalised product portfolio, and an expanding SaaS revenue stream &mdash; position the organisation well for continued growth. The Board expresses its appreciation to every member of the team for their contributions to these results.</p>
  <p><em>This document is intended for internal distribution only. All financial figures are preliminary and subject to audit confirmation.</em></p>
</body>
</html>",
        };

        // ── Bottom panel — original print options ─────────────────────────────
        pnlBottom = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8),
        };

        // System Print button
        btnSystemPrint = new System.Windows.Forms.Button
        {
            Text = "System Print",
            Size = new Size(120, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnSystemPrint.Click += btnSystemPrint_Click;

        // Browser Print button
        btnBrowserPrint = new System.Windows.Forms.Button
        {
            Text = "Browser Print",
            Size = new Size(130, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnBrowserPrint.Click += btnBrowserPrint_Click;

        // MSHTML Print button
        btnMshtmlPrint = new System.Windows.Forms.Button
        {
            Text = "MSHTML Print",
            Size = new Size(130, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnMshtmlPrint.Click += btnMshtmlPrint_Click;

        // Add right-to-left: first added = rightmost.
        pnlBottom.Controls.Add(btnSystemPrint);
        pnlBottom.Controls.Add(btnBrowserPrint);
        pnlBottom.Controls.Add(btnMshtmlPrint);

        // ── GroupBox — other (newly added) print options ───────────────────────
        // Single row: GDI Print | Embedded Preview
        btnGdiPrint = new System.Windows.Forms.Button
        {
            Text = "GDI Print",
            Size = new Size(110, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnGdiPrint.Click += btnGdiPrint_Click;

        btnEmbeddedPreview = new System.Windows.Forms.Button
        {
            Text = "Embedded Preview",
            Size = new Size(155, 36),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnEmbeddedPreview.Click += btnEmbeddedPreview_Click;

        pnlOtherOptions = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8, 6, 8, 6),
        };
        // First added = rightmost: GDI Print, Embedded Preview
        pnlOtherOptions.Controls.Add(btnGdiPrint);
        pnlOtherOptions.Controls.Add(btnEmbeddedPreview);

        gbOtherPrintOptions = new System.Windows.Forms.GroupBox
        {
            Text = "Other Print Options",
            Dock = DockStyle.Bottom,
            Height = 66,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(0),
        };
        gbOtherPrintOptions.Controls.Add(pnlOtherOptions);

        // ── Form ─────────────────────────────────────────────────────────────
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 600);
        MinimumSize = new Size(640, 430);
        Text = "WinForms HTML Print Sample";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblHtmlContent);
        Controls.Add(txtHtmlContent);
        // gbOtherPrintOptions added first → docks to very bottom edge.
        // pnlBottom added second → docks just above the GroupBox.
        Controls.Add(gbOtherPrintOptions);
        Controls.Add(pnlBottom);
    }

    #endregion
}
