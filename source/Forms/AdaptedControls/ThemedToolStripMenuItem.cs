// ThemedToolStripMenuItem provides a ToolStripMenuItem that complies with the FormSettings convention. 
// The only difference currently is the border, but is free to expand upon. 
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
            // Draw a border
            Drawing.DrawShadedRect(e.Graphics, Drawing.ShrinkRectangle(e.ClipRectangle, 0), Layer.Imminent, 3);

            base.OnPaint(e);            
        }
    }
}
