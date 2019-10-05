using System.Windows.Forms;
using System.Drawing;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public abstract class CustomButton : Button, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public bool CustomBorder = false;
        public CustomButton(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle Bounds = e.ClipRectangle;
            e.Graphics.FillRectangle(LayerBrush, Bounds);
            e.Graphics.FillRectangle(ElevationBrushes[(int)DrawingLayer], Bounds);
            Rectangle InnerBounds = new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 3, Bounds.Height - 3);
            e.Graphics.DrawRectangle(new Pen(ElevationBrushes[(int)DrawingLayer]), InnerBounds);
            e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(Bounds, Text, BaseUI.BaseFont));
        }
    }
}
