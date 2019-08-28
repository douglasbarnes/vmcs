using System.Windows.Forms;
using System.Drawing;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public class BorderedPanel : CustomPanel
    {
        public BorderedPanel(Layer layer, Emphasis emphasis) : base(layer, emphasis)
        {
            Tag = "Missing tag!";
        }
        public BorderedPanel() : base(Layer.Imminent, Emphasis.High)
        {
            Tag = "Missing tag!";
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //TAG = TEXT TO WRITE
            Graphics GraphicsHandler = e.Graphics;
            Rectangle Bounds = e.ClipRectangle;
            //prevent line being cut
            Bounds.Y += 10; //push downwards so we can have the text in line, we cannot draw outside the bounds of the control
            Bounds.Width -= 3;
            Bounds.Height -= 11;
            GraphicsHandler.DrawRectangle(new Pen(LayerBrush), Bounds);
            GraphicsHandler.DrawRectangle(new Pen(ElevationBrushes[(int)DrawingLayer]), Bounds);
            //Prevent strikethough of text, constants here are exact, they make it look nice            
            int Offset = TextRenderer.MeasureText(Tag.ToString(), BaseUI.BaseFont).Width;
            GraphicsHandler.DrawLine(new Pen(BaseUI.BackgroundColour), new Point(Bounds.Location.X + 11, Bounds.Location.Y), new Point(Bounds.Location.X + Offset + 14, Bounds.Location.Y));
            Bounds.X += 15;
            Bounds.Y -= 7;
            Bounds.Width += 15;
            Bounds.Height += 15;
            //GraphicsHandler.DrawString((string)Tag, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);          
            GraphicsHandler.DrawString(Tag.ToString(), BaseUI.BaseFont, LayerBrush, Bounds);
            GraphicsHandler.DrawString(Tag.ToString(), BaseUI.BaseFont, ElevationBrushes[(int)DrawingLayer + 1], Bounds);
        }
    }
}
