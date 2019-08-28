using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using debugger.Forms;
using static debugger.Forms.FormSettings;
namespace debugger.Util
{

    public static class Drawing
    {
        private const string FormatModifiers = "!\"£$%^&";
        private static readonly Dictionary<int, SolidBrush> ModifierTypes = new Dictionary<int, SolidBrush>
        {
            { 0, TextBrushes[0] },
            { 1, TextBrushes[1] },
            { 2, TextBrushes[2] },
            { 3, TextBrushes[3] },
            { 4, TextBrushes[4] },
            { 5, PrimaryBrush },
            { 6, SecondaryBrush },
        };
        public static void DrawFormattedText(string input, Graphics graphicsHandler, Rectangle bounds, Emphasis defaultEmphasis = Emphasis.Medium)
        {
            string[] Output = new string[FormatModifiers.Length];
            for (int i = 0; i < Output.Length; i++)
            {
                Output[i] = "";
            }
            Stack<int> ModifierHistory = new Stack<int>();
            ModifierHistory.Push((int)defaultEmphasis);
            bool Escaped = false;
            char Cursor;
            int CurrentModifier = (int)defaultEmphasis;
            for (int InputPosition = 0; InputPosition < input.Length; InputPosition++)
            {
                Cursor = input[InputPosition];
                if (Escaped)
                {
                    if (FormatModifiers.Contains(Cursor))
                    {
                        Output[CurrentModifier] = Output[CurrentModifier].PadRight(InputPosition) + Cursor;
                    }
                    else
                    {
                        Output[ModifierHistory.Peek()] += "\\";
                    }
                    Escaped = false;                    
                }
                else
                {
                    if(FormatModifiers.Contains(Cursor))
                    {
                        int NewModifier = FormatModifiers.IndexOf(Cursor);
                        if (NewModifier == ModifierHistory.Peek())
                        {
                            ModifierHistory.Pop();
                            CurrentModifier = ModifierHistory.Peek();
                        }
                        else
                        {
                            CurrentModifier = NewModifier;
                            ModifierHistory.Push(NewModifier);
                        }
                        continue; // dont add the modifier to the output
                    }
                    else if(Cursor == '\\')
                    {
                        Escaped = true;
                        continue; // ^ or the escape
                    }
                }
                for (int Modifier = 0; Modifier < Output.Length; Modifier++)
                {
                    if (Modifier == CurrentModifier)
                    {
                        Output[Modifier] += Cursor;
                    }
                    else
                    {
                        Output[Modifier] += " ";
                    }                    
                }
            }
            for (int ModifierType = 0; ModifierType < FormatModifiers.Length; ModifierType++)
            {
                if (Output[ModifierType].Trim() != "")
                {
                    graphicsHandler.DrawString(Output[ModifierType], BaseUI.BaseFont, ModifierTypes[ModifierType], bounds);
                }                           
            }            
        }
        public static void DrawShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer, int penSize = 1)
        {
            graphics.DrawRectangle(new Pen(LayerBrush, penSize), bounds);
            graphics.DrawRectangle(new Pen(ElevationBrushes[(int)overlayLayer], penSize), bounds);
        }
        public static void FillShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer)
        {
            graphics.FillRectangle(LayerBrush, bounds);
            graphics.FillRectangle(ElevationBrushes[(int)overlayLayer], bounds);
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
