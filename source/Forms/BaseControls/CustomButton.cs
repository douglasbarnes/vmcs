// CustomButton applies FormSettings convention to a button. It also applies a nice and discrete border, http://prntscr.com/pjq1ts.
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
            // Fill the background of the button.
            Rectangle Bounds = e.ClipRectangle;
            Drawing.FillShadedRect(e.Graphics, Bounds, DrawingLayer);

            // Create a new InnerBounds which is the border. It has to be reduced slightly to prevent clipping. The border is effectively one order
            // of layer higher than its background.
            Rectangle InnerBounds = new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 3, Bounds.Height - 3);
            e.Graphics.DrawRectangle(new Pen(ElevationBrushes[(int)DrawingLayer]), InnerBounds);

            // Finally draw the text in the centre of the button.
            e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(Bounds, Text, BaseUI.BaseFont));
        }
    }
}
