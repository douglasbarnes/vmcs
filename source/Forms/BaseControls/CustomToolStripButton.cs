// CustomToolStripButton applies the same ideas as a CustomToolStripItem to a ToolStripButton. The only differences are that it inherits from
// ToolStripButton and handles painting on its own.
using debugger.Util;
using System.Drawing;
using System.Windows.Forms;
using static debugger.Forms.FormSettings;

namespace debugger.Forms
{
    public abstract class CustomToolStripButton : ToolStripButton, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomToolStripButton(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw the background 
            Rectangle Bounds = e.ClipRectangle;
            Drawing.DrawShadedRect(e.Graphics, Bounds, DrawingLayer);

            // Draw the border(Must be translated and shrunk to avoid clipping)
            Rectangle InnerBounds = new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 3, Bounds.Height - 3);
            e.Graphics.DrawRectangle(new Pen(ElevationBrushes[(int)DrawingLayer]), InnerBounds);

            // Draw the text
            e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(Bounds, Text, BaseUI.BaseFont));
        }
    }
}
