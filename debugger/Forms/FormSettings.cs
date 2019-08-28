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
        public readonly Color PrimaryVariantColour;
        public readonly Color SecondaryColour;
        public UISettings(Font InputFont, Color InputBackgroundColour, Color InputTextColour)
        {
            BaseFont = InputFont;
            BackgroundColour = InputBackgroundColour;
            SurfaceColour = InputTextColour;            
            PrimaryColour = Color.FromArgb(200, InputBackgroundColour.R + 0xA9, InputBackgroundColour.B + 0x74, InputBackgroundColour.G + 0xEA);
            PrimaryVariantColour = InputTextColour;
            SecondaryColour = Color.FromArgb(240, InputBackgroundColour.R + 0xF1, InputBackgroundColour.B + 0xC8, InputBackgroundColour.G + 0xB3);
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
            LayerBrush = new SolidBrush(BaseUI.BackgroundColour);
            SecondaryBrush = new SolidBrush(BaseUI.SecondaryColour);
            ElevationBrushes = new List<SolidBrush>()
            {
                new SolidBrush(Color.FromArgb(0,Color.White)),
                new SolidBrush(Color.FromArgb(17,Color.White)), //~93%
                new SolidBrush(Color.FromArgb(22,Color.White)), //~91%
                new SolidBrush(Color.FromArgb(30,Color.White)), //~88%
                new SolidBrush(Color.FromArgb(38,Color.White)), //~85%
            };
            SurfaceShades = new List<Color>()
            {
                BaseUI.SurfaceColour,
                Color.FromArgb(220, BaseUI.SurfaceColour), // ~87%
                Color.FromArgb(153, BaseUI.SurfaceColour), // 60%
                Color.FromArgb(97, BaseUI.SurfaceColour), // ~38%
                Color.FromArgb(17, BaseUI.SurfaceColour) // 20%
            };
            TextBrushes = SurfaceShades.Select(x => new SolidBrush(x)).ToList();
            PrimaryBrush = new SolidBrush(BaseUI.PrimaryColour);
        }
    }
    

}
