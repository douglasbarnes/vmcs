// Drawing is a useful library for drawing forms. There are many times where this code would be repeated, or has need to be generalised.
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
        private static readonly List<SolidBrush> ModifierTypes = new List<SolidBrush>
        {
            TextBrushes[0],
            TextBrushes[1],
            TextBrushes[2],
            TextBrushes[3],
            TextBrushes[4],
            PrimaryBrush,
            SecondaryBrush,
        };
        public static void DrawFormattedText(string input, Graphics graphicsHandler, Rectangle bounds, Emphasis defaultEmphasis = Emphasis.Medium)
        {
            // DrawFormattedText() uses markdown modifiers to add effects to text, e.g change the colour.
            // The behaviour is hard to generalise for any case as it depends on the values of TextBrushes, but in the current implementation,
            //  ! - White
            //  " - Dimmer white
            //  £ - Light gray
            //  $ - Gray
            //  % - Dark gray
            //  ^ - Primary
            //  & - Secondary
            // The gray is produced as a result of the white text being transparent on a dark background.
            // The syntax is comparable to something like html(spacing is just for easier visualisation),
            //  DefaultText1 ! WhiteText ! DefaultText2
            // "WhiteText" is white because it has a bangbefore it.
            // "DefaultText2" is so because the bang before it terminated the white text.
            // This behaviour can also be nested,
            //  Default $ Gray ! White ! Gray $ Default
            // Also if you look on the main number keys on your keyboard, they follow the order of the numbers, so from a programmer's perspective,
            // Shift+3 uses TextBrushes[2] et cetera. It is also possible to escape these special characters with a \.
            // Throughout the method, the characters that the modifiers represent is completely abstracted. Only the index of the character in the
            // format modifiers string is concerned. This makes it very easy to swap out a character. All that has to be done is change the character
            // in the string. If you want to add a new one, make sure to add to the ModifierTypes dictionary also.

            // Create a new array with an index for each format modifier.
            // The array will be constructed like so,
            // [0] = "DarkText"
            // ...
            // [3] = "        LigherText"
            // ...
            // [5] = "                   PrimaryText"
            // The padding is necessary as all the text is written originating from the same starting point.            
            string[] Output = new string[FormatModifiers.Length];

            // Initialise the array with empty strings such that they can be appended to.
            for (int i = 0; i < Output.Length; i++)
            {
                Output[i] = "";
            }

            // Use a stack to control the behaviour of the modifiers. This allows for the nested behaviour. If the item on the top of the stack
            // is the same as the new modifier, it is popped, otherwise the new modifier is pushed on.
            Stack<int> ModifierHistory = new Stack<int>();

            // Firstly push the default modifier. There is no risk of the stack overflowing as there is no
            // modifier characacter for defaultEmphasis--It wil always be on the bottom.
            ModifierHistory.Push((int)defaultEmphasis);

            // $Escaped is used to denote a preceeding '\'
            bool Escaped = false;

            char Cursor;
            for (int InputPosition = 0; InputPosition < input.Length; InputPosition++)
            {
                Cursor = input[InputPosition];
                int NewModifier;
                // Don't consider the possibility of a modifier if the preceeding was an escape.
                if (Escaped)
                {
                    // Check whether the escape was actually escaping something or just a backslash in text.
                    if (!FormatModifiers.Contains(Cursor))
                    { 
                        // Append the escape back on(It wasn't added previously)
                        Output[ModifierHistory.Peek()] += "\\";
                    }

                    // Escapes only work for the adjacent character.
                    Escaped = false;

                    // Allow the cursor to be added as normal character.
                }

                // If it is an unescaped modifier character. (If it is not a modifier,  its index in $FormatModifiers will be -1.
                // Also assign this to a variable. This avoids calling IndexOf()/Contains() repeatedly.
                else if ((NewModifier = FormatModifiers.IndexOf(Cursor)) != -1)
                {
                    // If it is the same as the current modifier, pop that off the stack. Otherwise push the new one on.
                    if (NewModifier == ModifierHistory.Peek())
                    {
                        ModifierHistory.Pop();
                    }
                    else
                    {
                        ModifierHistory.Push(NewModifier);
                    }
                    
                    // Prevent the modifier being added onto the output.
                    continue;
                }

                // Check if it is an escape.
                else if (Cursor == '\\')
                {
                    // This will be important next iteration
                    Escaped = true;

                    // Don't add the escape on. This will be done next iteration. 
                    continue; 
                }

                // Iterate over every string in the array and append a space if its index is not the index of the current modifier, otherwise the current character.
                for (int Modifier = 0; Modifier < Output.Length; Modifier++)
                {
                    if (Modifier == ModifierHistory.Peek())
                    {
                        Output[Modifier] += Cursor;
                    }
                    else
                    {
                        Output[Modifier] += " ";
                    }
                }
                
            }

            // Draw all of the outputs
            for (int ModifierType = 0; ModifierType < FormatModifiers.Length; ModifierType++)
            {
                // It is unnecessary to draw an empty string. This would happen a lot as every string in the array is initialised to "".
                if (Output[ModifierType].Trim() != "")
                {
                    graphicsHandler.DrawString(Output[ModifierType], BaseUI.BaseFont, ModifierTypes[ModifierType], bounds);
                }                           
            }            
        }
        public static string CleanString(string input)
        {
            // A method to sanitise all format modifiers from a string.
            string Output = "";
            for (int i = 0; i < input.Length; i++)
            {
                // Append only if not a format modifier.
                if(!FormatModifiers.Contains(input[i]))
                {
                    Output += input[i];
                }
            }
            return Output;
        }
        public static void DrawShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer, int penSize = 1)
        {
            // Draw an outline of a rectangle with a particular layer. See FormSettings for explanation of layers.

            // The rectangle must be drawn twice. Once with the layer brush, then a semi-transparent layer on top to lighten.
            graphics.DrawRectangle(new Pen(LayerBrush, penSize), bounds);
            graphics.DrawRectangle(new Pen(ElevationBrushes[(int)overlayLayer], penSize), bounds);
        }
        public static void FillShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer)
        {
            // Draw a filled rectangle with a particular layer. See FormSettings for explanation of layers.
            // See DrawShadedRect() for explanation of code.

            graphics.FillRectangle(LayerBrush, bounds);
            graphics.FillRectangle(ElevationBrushes[(int)overlayLayer], bounds);
        }
        public static void DrawShadedText(Graphics graphics, Rectangle bounds, Layer overlayLayer, string text)
        {
            // Draw text as a layer rather than with an emphasis. See FormSettings for explanation of layers.
            // See DrawShadedRect() for explanation of code.

            graphics.DrawString(text, BaseUI.BaseFont, LayerBrush, bounds);
            graphics.DrawString(text, BaseUI.BaseFont, ElevationBrushes[(int)overlayLayer], bounds);
        }
        public static Rectangle GetCenter(Rectangle bounds, string text, Font font)
        {
            // A method to center text in a given rectangle.
            // The width of the text and height are used as an offset to the bounds.
            Size TextSize = CorrectedMeasureText(text, font);
            return GetCenter(bounds, TextSize.Width, TextSize.Height);
        }
        public static Rectangle GetCenter(Rectangle bounds, int offsetx = 0, int offsety = 0)
            // A method to get the center of a bound.

            // Half all values to get the centre of a rectangle. The center is offset by each of the offset values.
            // This is done by subtracting them from the width and height in order to find the center of a rectangle
            // with a length ($Width-$offsetx) and height ($Height-$offsety). This is then accounted for afterwards by
            // adding it back on to the size. Lastly it is centered. This is greatly simplified in the code.
            // Its much simpler explained with a drawing, http://prntscr.com/pjonew
            => new Rectangle(
                new Point(bounds.X + (bounds.Width - offsetx) / 2, bounds.Y + (bounds.Height - offsety) / 2),
                new Size(bounds.Width / 2 + offsetx, bounds.Height / 2 + offsety));
        public static Rectangle GetTextCenterHeight(Rectangle bounds)
            // Center the height of a rectangle specifically for text. This will only function with small rectangles. This is because there is a small amount
            // of lead associated with the text that will look disproportionate with larger rectangles.
            => new Rectangle(
                new Point(bounds.X, bounds.Y + (bounds.Height / 4)),
                new Size(bounds.Width, bounds.Height / 2));
        public static Rectangle ShrinkRectangle(Rectangle bounds, int pixels)
            // Shrink a rectangle by $pixels on the height and width.
            => new Rectangle(
                bounds.Location,
                new Size(bounds.Width - pixels, bounds.Height - pixels));
        public static Size CorrectedMeasureText(string text, Font font)
        {
            // Correct TextRenderer.MeasureText() to a higher accuracy as for some reason it adds almost an extra whitespace worth of width onto the result.
            // This is untested with other fonts, but works on proportion so in theory should work fine.
            // Here is some extra information,
            // https://stackoverflow.com/questions/1087157/accuracy-of-textrenderer-measuretext-results
            // https://stackoverflow.com/questions/7361156/why-is-textrenderer-measuretext-inaccurate-here?rq=1
            Size ToCorrect = TextRenderer.MeasureText(text, font);

            // Proportionally reduce the width by half a character. Any more can cause clipping issues.
            ToCorrect.Width -= (int)font.Size / 2;

            return ToCorrect;
        }
    }
}
