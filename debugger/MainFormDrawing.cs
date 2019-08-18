using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using static debugger.Util.Drawing;
using static debugger.CustomControls;
using static debugger.FormSettings;
namespace debugger
{
    public partial class MainForm
    {
        internal EndToolStripMenuItem ExitMenu;
        internal ThemedToolStripMenuHeader DebugMenu;
        internal ThemedToolStripMenuHeader ViewMenu;
        internal ThemedMenuStrip TopMenuStrip;
        internal ThemedToolStripMenuItem SelectDebugMenu;
        internal ThemedToolStripMenuItem AllDebugMenu;
        internal TestcaseSearchTextbox SearchDebugMenu;
        internal ThemedToolStripMenuItem FormatViewMenu;
        private void CustomDraw()
        {
            DrawMenuBar();
        }
        private void DrawMenuBar()
        {
            TopMenuStrip = new ThemedMenuStrip(Layer.Background, Emphasis.Medium)
            {
                Location = new Point(0, 0),
                Name = "TopMenuStrip",
                Size = new Size(Width, 24),   
                
            };
            MainMenuStrip = TopMenuStrip;
            Controls.Add(TopMenuStrip);
            CreateMenuDebug();
            CreateMenuExit();
            CreateMenuView();
            TopMenuStrip.Items.AddRange(new[] { DebugMenu, ViewMenu, ExitMenu });
            TopMenuStrip.PerformLayout();
            
            
        }
        private void CreateMenuExit()
        {
            ExitMenu = new EndToolStripMenuItem()
            {
                DrawingLayer = Layer.Imminent,
                Name = "ExitMenuItem",
                Size = CorrectedMeasureText(" Exit ", BaseUI.BaseFont),
                Text = "Exit",
                TextEmphasis = Emphasis.Medium
            };
            ExitMenu.Click += new EventHandler(ExitToolStripMenuItem_Click);
            
        }
        private void CreateMenuDebug()
        {
            //a work around for SelectDebugMenuItem.Dropdown opening when 's' is used in search, this is selected instead, which does nothing.
            var FillerDebugItem = new ToolStripMenuItem() { Text = "S", Size = new Size(0, 0), AutoSize = false };
            Size ItemSize = new Size(70, 20);
            SelectDebugMenu = new ThemedToolStripMenuItem()
            {
                Size = ItemSize,
                Text = "Select"                
            };
            string[] Testcases = Hypervisor.TestHandler.GetTestcases();
            for (int i = 0; i < Testcases.Length; i++)
            {
                var ToAdd = new ThemedToolStripMenuItem()
                {
                    Text = Testcases[i],
                    Size = ItemSize
                };
                ToAdd.Click += (s,a) => OnTestcaseSelected(ToAdd.Text);
                SelectDebugMenu.DropDownItems.Add(ToAdd);
            }
            AllDebugMenu = new ThemedToolStripMenuItem()
            {
                Size = ItemSize,
                Text = "All"
            };
            AllDebugMenu.Click += (s, a) => OnTestcaseSelected("all");
            SearchDebugMenu = new TestcaseSearchTextbox(
                () => Hypervisor.TestHandler.GetTestcases(),
                (name) =>  OnTestcaseSelected(name),
                Layer.Background, 
                Emphasis.Medium)
            {
                Size = ItemSize,
                Name = ""
            };
            DebugMenu = new ThemedToolStripMenuHeader()
            {
                Name = "DebugMenuItem",
                Size = CorrectedMeasureText(" Debug ", BaseUI.BaseFont),
                Text = "Debug",
            };
            DebugMenu.DropDown.PreviewKeyDown += SearchDebugMenu.KeyPressed;
            DebugMenu.DropDown.KeyDown += (s, e) => { };
            DebugMenu.DropDownItems.AddRange(new ToolStripItem[]{ FillerDebugItem, AllDebugMenu, SelectDebugMenu, SearchDebugMenu });
        }
        private void CreateMenuView()
        {
            ViewMenu = new ThemedToolStripMenuHeader()
            {
                DrawingLayer = Layer.Imminent,
                Name = "ViewMenuItem",
                Size = CorrectedMeasureText(" View ", BaseUI.BaseFont),
                Text = "View",
                TextEmphasis = Emphasis.Medium
            };
            FormatViewMenu = new ThemedToolStripMenuItem()
            {
                Name = "FormatView",
                Text = "Format",                
            };
            FormatViewMenu.DropDownItems.AddRange(new[] { new ThemedToolStripRadioButton(), new ThemedToolStripRadioButton()});
            ViewMenu.DropDownItems.Add(FormatViewMenu);
        }
    }

    public static class FormSettings
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
        public static List<SolidBrush> ElevatedTransparentOverlays = new List<SolidBrush>();
        public static List<SolidBrush> TextBrushes = new List<SolidBrush>();
        public static SolidBrush LayerBrush;
        public static SolidBrush PrimaryBrush;

        public static List<Color> SurfaceShades = new List<Color>();
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
                //
                PrimaryColour = Color.FromArgb(240, InputBackgroundColour.R + 0xA9, InputBackgroundColour.B + 0x74, InputBackgroundColour.G + 0xEA);
                PrimaryVariantColour = InputTextColour;
                SecondaryColour = InputTextColour;
            }
        }
        public static UISettings BaseUI;
        static FormSettings()
        {
            BaseUI = new UISettings(new Font("Consolas", 9), Color.FromArgb(18, 18, 18), Color.FromArgb(220, 255, 255, 255));
            LayerBrush = new SolidBrush(BaseUI.BackgroundColour);
            ElevatedTransparentOverlays = new List<SolidBrush>()
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
