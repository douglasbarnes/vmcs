using System.Windows.Forms;
namespace debugger.Forms
{
    public abstract class CustomPanel : Panel, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomPanel(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DoubleBuffered = true;
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
        }

    }
}
