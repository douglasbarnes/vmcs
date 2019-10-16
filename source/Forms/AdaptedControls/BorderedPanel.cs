// BorderedPanel provides a clean and consistent border texture for controls. Here is an example, http://prntscr.com/pj5r35.
// The text displayed(in the example is "Flags") is the value stored in the Tag variable(Inherited from Control).
// It is important to keep the contents of the panel fully inside of it. Sometimes the border will be clipped over otherwise.
// This is a problem with windows forms and can only be worked around internally.
using System.Windows.Forms;
using System.Drawing;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public class BorderedPanel : CustomPanel
    {
        public BorderedPanel(Layer layer, Emphasis emphasis) : base(layer, emphasis)
        {
            // Default the tag, because it would not make much sense to use this control without making use of this feature.
            Tag = "Missing tag!";
        }
        public BorderedPanel() : base(Layer.Imminent, Emphasis.High)
        {
            Tag = "Missing tag!";
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // Create a new variable to hold e.ClipRectangle(this is rectangle represents the bounds of the entire control).
            // Some modifications need to be made to this rectangle to prevent lines clipping off the edge.
            Rectangle Bounds = e.ClipRectangle;

            // Essentially, move down a little bit and shrink. The shift is because the space available to draw on is less than the bounds, not equal to.
            // The shift downwards makes sure that text has room to go. Forms do not allow drawing outside of e.ClipRectangle.
            Bounds.Y += 10;
            Bounds.Width -= 3;
            Bounds.Height -= 11;

            // Draw the outline of the rectangle, the border.
            Drawing.DrawShadedRect(e.Graphics, Bounds, DrawingLayer);

            // Now a line has to be drawn on top of the previous outline where the text will be plus a small bit of padding to look neat.
            // This prevents strikethough of text. Constants here are very exact obtained through trial and error just to make it as aesthetic as possible.
            
            // Measure the length of the text.
            int TextLength = TextRenderer.MeasureText(Tag.ToString(), BaseUI.BaseFont).Width;

            // Annotated example: http://prntscr.com/pj705m , this is referenced below.
            // Draw the a line from the corner plus a small offset to add a litte corner(1), then continue through the length of the text plus a constant(2)            
            e.Graphics.DrawLine(new Pen(BaseUI.BackgroundColour), new Point(Bounds.Location.X + 11, Bounds.Location.Y), new Point(Bounds.Location.X + TextLength + 14, Bounds.Location.Y));

            // Now adjust the bounds back a little. This centers the text, such that the middle of the text is where the line was previously.
            Bounds.X += 15;
            Bounds.Y -= 7;

            // Finally draw the text.
            Drawing.DrawShadedText(e.Graphics, Bounds, DrawingLayer+1, Tag.ToString());
        }
    }
}
