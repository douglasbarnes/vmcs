using System.Windows.Forms;
using System.Drawing;
using debugger.Util;
namespace debugger.Forms
{
    public abstract class CustomMenuStrip : MenuStrip, IMyCustomControl
    {
        public class ThemeRenderer : ToolStripRenderer
        {
            private Layer DrawingLayer;
            private Emphasis TextEmphasis;
            public ThemeRenderer(Layer layer, Emphasis emphasis)
            {
                TextEmphasis = emphasis;
                DrawingLayer = layer;
            }
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                Drawing.FillShadedRect(e.Graphics, Rectangle.Round(e.Graphics.ClipBounds), Layer.Foreground);
            }
        }
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomMenuStrip(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
            Renderer = new ThemeRenderer(drawingLayer, textEmphasis);
        }
    }
}
