// CustomToolStripTextBox is a wrapper around ToolStripMenuItem that implements user input functionality.
// There are two main important concepts, the variables Input and Prefix. Input is the text that can be freely modified
// by the user. Prefix can only be modified by the program. Also, $BufferSize is the maximum size of $Input.
using System.Windows.Forms;
using System.Drawing;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public abstract class CustomToolStripTextBox : ToolStripMenuItem, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public string Input = "";
        public string Prefix = "";
        public int BufferSize = 15;
        public CustomToolStripTextBox(Layer drawingLayer, Emphasis textEmphasis) : base()
        {
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
            AutoSize = true;
        }
        protected void Ready()
        {
            // Generate the text out of the prefix and input.
            Text = Prefix + Input;
        }
        private KeysConverter Converter = new KeysConverter();
        public void KeyPressed(object s, PreviewKeyDownEventArgs e)
        {
            // If the key pressed was a backspace and there is text to delete, do so.
            if (e.KeyCode == Keys.Back && Input.Length > 0)
            {
                Input = Input.Substring(0, Input.Length - 1);
            }
            else
            {
                // Convert the keycode into a string.
                string Key = Converter.ConvertToString(e.KeyCode).ToLower();

                // Check if the buffer size permits more characters. It's not strictly the size of the buffer but acts like so.
                // Also check the length of $Key. This is because strangely special keys such as End will be converted to "End". 
                // Finally validate it as an appropriate character. 
                if (Input.Length < BufferSize && Key.Length == 1 && "abcdefghijklmnopqrstuvwxyz!\"!£$%^&*()[];'#,./<>?:@~{}1234567890".Contains(Key))
                {
                    Input += Key;
                }
            }
                 
            // Commit the changes.
            Ready();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw a border
            Drawing.DrawShadedRect(e.Graphics, e.ClipRectangle, Layer.Imminent, 3);

            // Create a new bounds for the text.
            Rectangle Bounds = Drawing.GetTextCenterHeight(e.ClipRectangle);            

            // Each line is drawn separately such that they can have a different emphasis. This has the following effect, http://prntscr.com/pjpw9a
            // It makes it much easier for the user to differentiate between their input and the prefix.
            e.Graphics.DrawString(Prefix, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);

            // Offset the bounds by the width of the previous text.
            Bounds.Offset(Drawing.CorrectedMeasureText(Prefix, BaseUI.BaseFont).Width, 0);
            e.Graphics.DrawString(Input, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis + 1], Bounds);
        }
    }
}
