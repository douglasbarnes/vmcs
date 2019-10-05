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
            Drawing.DrawFormattedText(Text, e.Graphics, bounds: e.ClipRectangle, defaultEmphasis: TextEmphasis);
        }
    }
}
