using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Vigil.Views;

public class DarkContextMenuRenderer : ToolStripRenderer
{
    private static readonly Color BgColor = Color.FromArgb(0x14, 0x15, 0x18);
    private static readonly Color BorderColor = Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF);
    private static readonly Color HoverBgColor = Color.FromArgb(0x22, 0x25, 0x2C);
    private static readonly Color HoverBorderColor = Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF);
    private static readonly Color TextColor = Color.FromArgb(0xED, 0xED, 0xF0);
    private static readonly Color TextMutedColor = Color.FromArgb(0x78, 0x7E, 0x8E);
    private static readonly Color DangerColor = Color.FromArgb(0xFF, 0x5C, 0x5C);
    private static readonly Color DangerHoverBg = Color.FromArgb(0x38, 0x1A, 0x1A);
    private static readonly Color DangerHoverBorder = Color.FromArgb(0x60, 0xE5, 0x53, 0x53);
    private static readonly Color SeparatorColor = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF);

    protected override void InitializeItem(ToolStripItem item)
    {
        base.InitializeItem(item);
        item.ForeColor = TextColor;
        item.Margin = new Padding(2, 1, 2, 1);
        item.Padding = new Padding(8, 5, 8, 5);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(BorderColor, 1);
        var r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        using var path = CreateRoundedPath(r, 8);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(BgColor);
        var r = new Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
        using var path = CreateRoundedPath(r, 8);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is not ToolStripMenuItem item) return;

        if (item.Selected && item.Enabled)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var isDanger = item.Tag as string == "danger" || (item.Text != null && item.Text.Contains("Stop Protection", StringComparison.OrdinalIgnoreCase));

            using var brush = new SolidBrush(isDanger ? DangerHoverBg : HoverBgColor);
            using var pen = new Pen(isDanger ? DangerHoverBorder : HoverBorderColor, 1);

            var rect = new Rectangle(4, 1, item.Width - 8, item.Height - 2);
            using var path = CreateRoundedPath(rect, 5);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var isDanger = e.Item.Tag as string == "danger" || (e.Text != null && e.Text.Contains("Stop Protection", StringComparison.OrdinalIgnoreCase));
        var color = !e.Item.Enabled ? TextMutedColor : (isDanger ? DangerColor : TextColor);

        var rect = new Rectangle(e.TextRectangle.X, e.TextRectangle.Y, e.TextRectangle.Width, e.TextRectangle.Height);

        TextRenderer.DrawText(
            e.Graphics,
            e.Text,
            e.TextFont,
            rect,
            color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
        );
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(SeparatorColor, 1);
        e.Graphics.DrawLine(pen, 14, y, e.Item.Width - 14, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw stylish minimalist pill checkbox
        var r = e.ImageRectangle;
        var boxRect = new Rectangle(r.X + 2, r.Y + (r.Height - 14) / 2, 14, 14);

        using var boxBg = new SolidBrush(Color.FromArgb(0xED, 0xED, 0xF0));
        using var checkPath = CreateRoundedPath(boxRect, 3);
        e.Graphics.FillPath(boxBg, checkPath);

        // Draw dark checkmark inside
        using var checkPen = new Pen(Color.FromArgb(0x13, 0x14, 0x17), 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        var p1 = new Point(boxRect.X + 3, boxRect.Y + 7);
        var p2 = new Point(boxRect.X + 6, boxRect.Y + 10);
        var p3 = new Point(boxRect.X + 11, boxRect.Y + 4);

        e.Graphics.DrawLine(checkPen, p1, p2);
        e.Graphics.DrawLine(checkPen, p2, p3);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Seamless background without separate gray/white column
    }

    private static GraphicsPath CreateRoundedPath(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        if (d > r.Width) d = r.Width;
        if (d > r.Height) d = r.Height;

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
