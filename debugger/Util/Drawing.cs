using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using static debugger.FormSettings;
using System.Windows.Forms;

namespace debugger.Util
{
    public static class Drawing
    {
        private static string FormatModifiers = "!\"£$%";
        public static void DrawFormattedText(string text, Graphics graphicsHandler, Rectangle bounds, Emphasis defaultEmphasis = Emphasis.Medium)
        {
            string[] Output = new string[5];
            string Position = "";
            Stack<int> ModifierHistory = new Stack<int>();
            ModifierHistory.Push((int)defaultEmphasis);
            bool Escaped = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (Escaped & !"!\"£$%".Contains(text[i]))
                {
                    Output[ModifierHistory.Peek()] += "\\";

                }

                if (FormatModifiers.Contains(text[i]) & !Escaped)
                {
                    if (FormatModifiers.IndexOf(text[i]) == ModifierHistory.Peek())
                    {
                        ModifierHistory.Pop();
                    }
                    else
                    {
                        ModifierHistory.Push(FormatModifiers.IndexOf(text[i]));
                    }
                }
                else if (text[i] == '\\' & !Escaped)
                {
                    Escaped = true;
                }
                else
                {
                    Escaped = false;
                    graphicsHandler.DrawString(Position + text[i], BaseUI.BaseFont, TextBrushes[ModifierHistory.Peek()], bounds);
                    Position += " ";
                }
            }
        }
        public static void DrawShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer, int penSize = 1)
        {
            graphics.DrawRectangle(new Pen(LayerBrush, penSize), bounds);
            graphics.DrawRectangle(new Pen(ElevatedTransparentOverlays[(int)overlayLayer], penSize), bounds);
        }
        public static void FillShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer)
        {
            graphics.FillRectangle(LayerBrush, bounds);
            graphics.FillRectangle(ElevatedTransparentOverlays[(int)overlayLayer], bounds);
        }
        public static Rectangle GetCenter(Rectangle bounds, string text, Font font)
        {
            Size TextSize = CorrectedMeasureText(text, font);
            return GetCenter(bounds, TextSize.Width, TextSize.Height);
        }
        public static Rectangle GetCenter(Rectangle bounds, int offsetx = 0, int offsety = 0)
            => new Rectangle(
                new Point(bounds.X + (bounds.Width - offsetx) / 2, bounds.Y + (bounds.Height - offsety) / 2),
                new Size(bounds.Width / 2 + offsetx, bounds.Height / 2 + offsety));
        public static Rectangle GetCenterHeight(Rectangle bounds)
            => new Rectangle(
                new Point(bounds.X, bounds.Y + (bounds.Height / 4)),
                new Size(bounds.Width, bounds.Height / 2));
        public static Rectangle ShrinkRectangle(Rectangle bounds, int pxSquared)
            => new Rectangle(
                bounds.Location,
                new Size(bounds.Width - pxSquared, bounds.Height - pxSquared));
        public static Size CorrectedMeasureText(string text, Font font)
        {
            Size ToCorrect = TextRenderer.MeasureText(text, font);
            ToCorrect.Width -= (int)font.Size / 2;
            return ToCorrect;
        }
    }
}
