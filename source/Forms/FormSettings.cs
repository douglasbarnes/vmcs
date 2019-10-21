// FormSettings.cs provides a really easy way to syncrhonise colours throughout the program, so long as every control makes use of
// the class. It is left separate from UISettings to make it much easier to maintain and modify from an external point of view, or 
// adapt and merge into another program. UISettings takes a few input parameters and creates a theme based on it. The outputs are then
// readily available as public variables. FormSettings makes use of this by also creating brushes out of the colours ahead of time,
// such that brushes do not have to be recreated in memory every time a control is drawn. It also standardises the brushes that
// each control uses. If the colours are to be changed, they only have to be changed in this file rather than in every control.
// The implementation of FormSettings adheres to the Google material.io dark theme, which simply describes a primary colour, a
// secondary colour, a background colour, and a surface colour. "Emphasis" is dictates the transparency of the text(which is coded
// to be white, #FFFFFF). "Layer" dictates the transparency of a white second layer that goes on top of the background. This
// transparency reduces how much attention is drawn to each element. E.g, http://prntscr.com/pj668c , the border outlining the flags
// control highlights an area of interest, however it is not as important to the user as the flags content. On top of this, a flag that
// is "True" is even more important to the user, so the text has a greater emphasis. This also creates a better indication of a flag changing.
// The text Flags is also less emphasised because the user is likely to read it once, then remember what it does.
// This example is all due possible because of the Emphasis and Layer concepts.  The brush lists in FormSettings can be used alongside Emphasis.
// To create a background layer,
//  - Fill a rectangle with the LayerBrush
//  - Fill a rectangle on top with FormSettings.ElevationBrushes[$Layer]
// To draw text, use FormSettings.TextBrushes[$Emphasis]  
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace debugger.Forms
{
    public enum Emphasis
    {
        Imminent = 0,
        High = 1,
        Medium = 2,
        Disabled = 3,
        Ignored = 4
    }
    public enum Layer
    {
        Background = 0,
        Foreground = 1,
        Surface = 2,
        Imminent = 3
    }
    public struct UISettings
    {
        public readonly Font BaseFont;
        public readonly Color BackgroundColour;
        public readonly Color SurfaceColour;
        public readonly Color PrimaryColour;
        public readonly Color SecondaryColour;
        public UISettings(Font InputFont, Color InputBackgroundColour, Color InputTextColour)
        {
            BaseFont = InputFont;
            BackgroundColour = InputBackgroundColour;
            SurfaceColour = InputTextColour;

            // The suggested material dark colours have this offset, so applying it to any colour has a similar effect.
            PrimaryColour = Color.FromArgb(200, (byte)(InputBackgroundColour.R + 0xA9), (byte)(InputBackgroundColour.B + 0x74), (byte)(InputBackgroundColour.G + 0xEA));
            SecondaryColour = Color.FromArgb(240, (byte)(InputBackgroundColour.R + 0xF1), (byte)(InputBackgroundColour.B + 0xC8), (byte)(InputBackgroundColour.G + 0xB3));
        }
    }
    public class FormSettings
    {
        public static List<SolidBrush> ElevationBrushes = new List<SolidBrush>();
        public static List<SolidBrush> TextBrushes = new List<SolidBrush>();
        public static SolidBrush LayerBrush;
        public static SolidBrush PrimaryBrush;
        public static SolidBrush SecondaryBrush;
        public static List<Color> SurfaceShades = new List<Color>();
        public static UISettings BaseUI;
        static FormSettings()
        {
            BaseUI = new UISettings(new Font("Consolas", 9), Color.FromArgb(0x12, 0x12, 0x12), Color.FromArgb(220, 255, 255, 255));

            // Create new brushes out of the BaseUI colours.
            LayerBrush = new SolidBrush(BaseUI.BackgroundColour);
            SecondaryBrush = new SolidBrush(BaseUI.SecondaryColour);
            ElevationBrushes = new List<SolidBrush>()
            {
                new SolidBrush(Color.FromArgb(0,Color.White)),  // 100% transparency of layer beneath
                new SolidBrush(Color.FromArgb(17,Color.White)), // ~93% 
                new SolidBrush(Color.FromArgb(22,Color.White)), // ~91% 
                new SolidBrush(Color.FromArgb(30,Color.White)), // ~88% 
                new SolidBrush(Color.FromArgb(38,Color.White)), // ~85%
            };
            SurfaceShades = new List<Color>()
            {
                BaseUI.SurfaceColour,
                Color.FromArgb(220, BaseUI.SurfaceColour), // ~87% transparency of text.
                Color.FromArgb(153, BaseUI.SurfaceColour), // 60%
                Color.FromArgb(97, BaseUI.SurfaceColour), // ~38%
                Color.FromArgb(17, BaseUI.SurfaceColour) // 20%
            };

            // Create TextBrushes from the surface colours.
            TextBrushes = SurfaceShades.Select(x => new SolidBrush(x)).ToList();

            PrimaryBrush = new SolidBrush(BaseUI.PrimaryColour);
        }
    }


}
