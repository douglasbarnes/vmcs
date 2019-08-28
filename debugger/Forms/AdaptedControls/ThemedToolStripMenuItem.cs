using System.Windows.Forms;
using debugger.Util;
namespace debugger.Forms
{
    public class ThemedToolStripMenuItem : CustomToolStripMenuItem
    {
        public ThemedToolStripMenuItem() : base(Layer.Background, Emphasis.Medium)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Drawing.DrawShadedRect(e.Graphics, Drawing.ShrinkRectangle(e.ClipRectangle, 0), Layer.Imminent, 3);
            base.OnPaint(e);
        }

    }
}
