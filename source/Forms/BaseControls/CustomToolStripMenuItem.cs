using System.Windows.Forms;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public abstract class CustomToolStripMenuItem : ToolStripMenuItem, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomToolStripMenuItem(Layer drawingLayer, Emphasis textEmphasis)
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(e.ClipRectangle, Text, BaseUI.BaseFont));
        }
    }
}
