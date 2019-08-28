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
            Text = Prefix + Input;
        }
        private KeysConverter Converter = new KeysConverter();
        public void KeyPressed(object s, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Back && Input.Length > 0)
            {
                Input = Input.Remove(Input.Length - 1);
            }
            string Key = Converter.ConvertToString(e.KeyCode).ToLower();
            if (Input.Length < BufferSize && Key.Length == 1 && "abcdefghijklmnopqrstuvwxyz!\"!£$%^&*()[];'#,./<>?:@~{}1234567890".Contains(Key))
            {
                Input += Key;
            }
            Ready();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle Bounds = Drawing.GetCenterHeight(e.ClipRectangle);
            Drawing.DrawShadedRect(e.Graphics, Drawing.ShrinkRectangle(e.ClipRectangle, 0), Layer.Imminent, 3);
            e.Graphics.DrawString(Prefix, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
            Bounds.Offset(Drawing.CorrectedMeasureText(Prefix, BaseUI.BaseFont).Width, 0);
            e.Graphics.DrawString(Input, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis + 1], Bounds);

        }
    }
}
