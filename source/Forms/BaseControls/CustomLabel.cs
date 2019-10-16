// CustomLabel applies the convention of FormSettings to labels. It draws text only and relies on its background
// to be draw by something else e.g the form.
using System.Windows.Forms;
using debugger.Util;
namespace debugger.Forms
{
    public abstract class CustomLabel : Label, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public CustomLabel(Emphasis textLayer) : base()
        {
            TextEmphasis = textLayer;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw text
            Drawing.DrawFormattedText(Text, e.Graphics, bounds: e.ClipRectangle, defaultEmphasis: TextEmphasis);
        }
    }
}
