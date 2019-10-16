// CustomPanel applies the FormSettings convention to a Panel. It also enabled double buffering which greatly improves the
// frame rate when drawing as the new graphics are drawn to another frame buffer. Once they are finished they are moved to the real
// frame. The interval is almost instant but eliminates an otherwise super annoying flicker.
// Extra literature on this: https://docs.microsoft.com/en-us/dotnet/framework/winforms/advanced/how-to-reduce-graphics-flicker-with-double-buffering-for-forms-and-controls
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
