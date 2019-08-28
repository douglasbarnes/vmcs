using System.Drawing;
namespace debugger.Forms
{
    public class ControlButton : CustomButton
    {
        public ControlButton() : base(Layer.Surface, Emphasis.Medium)
        {
            FlatAppearance.BorderSize = 0;
            CustomBorder = true;
            Size = new Size(75, 23);
        }
    }
}
