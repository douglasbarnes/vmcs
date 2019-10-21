// CustomMenuStrip applies the FormSettings convention to a MenuStrip. It also handles the drawing of the background for its elements.
// This ensures they have a consistent background, however if really desired they could draw their own on top. This is done through
// the ThemeRenderer as required by windows forms.
using debugger.Util;
using System.Drawing;
using System.Windows.Forms;
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
                Drawing.FillShadedRect(e.Graphics, Rectangle.Round(e.Graphics.ClipBounds), DrawingLayer);
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
