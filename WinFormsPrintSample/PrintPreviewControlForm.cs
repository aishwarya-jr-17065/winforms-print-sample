using System.Drawing.Printing;

namespace WinFormsPrintSample;

/// <summary>
/// Demonstrates the <see cref="PrintPreviewControl"/> approach from the Microsoft WinForms
/// printing documentation ("How to print in Windows Forms using Print Preview").
///
/// Unlike <see cref="PrintPreviewDialog"/> — which is a self-contained ready-made dialog —
/// <see cref="PrintPreviewControl"/> is a bare control that you can embed anywhere inside
/// your own form. This lets you design a fully custom preview UI: add your own toolbar,
/// zoom controls, page navigation, etc.
///
/// See: https://learn.microsoft.com/dotnet/desktop/winforms/printing/how-to-print-in-windows-forms-using-print-preview
/// </summary>
internal sealed class PrintPreviewControlForm : Form
{
    private readonly PrintDocument _printDocument;
    private readonly PrintPreviewControl _previewControl;
    private readonly Label _lblZoom;
    private double _zoom = 0.75;

    public PrintPreviewControlForm(PrintDocument printDocument)
    {
        _printDocument = printDocument;

        // ── Form ──────────────────────────────────────────────────────────────
        Text = "Embedded Preview — PrintPreviewControl";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(960, 720);
        MinimumSize = new Size(640, 480);

        // ── Toolbar panel ──────────────────────────────────────────────────────
        var toolbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = SystemColors.ControlLight,
        };

        // Print button
        var btnPrint = new Button
        {
            Text = "🖨  Print…",
            Size = new Size(110, 34),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 9.5f),
        };
        btnPrint.Click += OnPrintClick;

        // Zoom in button
        var btnZoomIn = new Button
        {
            Text = "＋",
            Size = new Size(36, 34),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnZoomIn.Click += (s, e) => AdjustZoom(+0.15);

        // Zoom out button
        var btnZoomOut = new Button
        {
            Text = "－",
            Size = new Size(36, 34),
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 10f),
        };
        btnZoomOut.Click += (s, e) => AdjustZoom(-0.15);

        // Zoom label
        _lblZoom = new Label
        {
            Text = FormatZoom(),
            AutoSize = false,
            Size = new Size(60, 34),
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f),
        };

        // Description label (fills remaining space)
        var lblDesc = new Label
        {
            Text = "Using embedded PrintPreviewControl (not a dialog) — zoom with ± buttons, then click Print…",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
        };

        // Add right-to-left (first added = rightmost), then the fill label last.
        toolbar.Controls.Add(btnPrint);
        toolbar.Controls.Add(btnZoomIn);
        toolbar.Controls.Add(_lblZoom);
        toolbar.Controls.Add(btnZoomOut);
        toolbar.Controls.Add(lblDesc);

        // ── PrintPreviewControl (fills the rest of the form) ───────────────────
        // This is the key difference vs. PrintPreviewDialog:
        // the control is embedded directly inside the form, giving full
        // control over the surrounding UI.
        _previewControl = new PrintPreviewControl
        {
            Dock = DockStyle.Fill,
            Document = _printDocument,
            Zoom = _zoom,
            UseAntiAlias = true,
        };

        Controls.Add(_previewControl);
        Controls.Add(toolbar);
    }

    // -----------------------------------------------------------------------

    private void AdjustZoom(double delta)
    {
        _zoom = Math.Max(0.10, Math.Min(5.0, _zoom + delta));
        _previewControl.Zoom = _zoom;
        _lblZoom.Text = FormatZoom();
    }

    private string FormatZoom() => $"{_zoom * 100:0}%";

    private void OnPrintClick(object? sender, EventArgs e)
    {
        using var dlg = new PrintDialog
        {
            Document = _printDocument,
            AllowSomePages = true,
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
            _printDocument.Print();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _printDocument.Dispose();

        base.Dispose(disposing);
    }
}
