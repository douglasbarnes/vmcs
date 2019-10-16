// CustomTextBox applies the FormSettings convention to a TextBox
using System.Windows.Forms;
namespace debugger.Forms
{
    public abstract class CustomTextBox : TextBox, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomTextBox(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
        }
    }
}
