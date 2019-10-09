using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using static debugger.Util.Drawing;
using debugger.Forms;
using static debugger.Forms.FormSettings;
namespace debugger
{
    public partial class MainForm
    {
        internal EndToolStripMenuItem ExitMenu;
        internal ThemedToolStripMenuHeader DebugMenu;
        internal ThemedToolStripMenuHeader ViewMenu;
        internal ThemedToolStripMenuHeader FileMenu;
        internal ThemedMenuStrip TopMenuStrip;
        internal ThemedToolStripMenuItem SelectDebugMenu;
        internal ThemedToolStripMenuItem AllDebugMenu;
        internal TestcaseSearchTextBox SearchDebugMenu;
        internal ThemedToolStripMenuItem FormatViewMenu;
        internal MemoryListView memviewer;
        internal BorderedPanel PanelMemory;
        internal RegisterPanel PanelRegisters;     
        internal ControlButton ButtonStep;
        internal ControlButton ButtonRun;
        internal ControlButton ButtonReset;
        internal DisassemblyListView ListViewDisassembly;
        internal BorderedPanel DisassemblyBorder;
        internal Panel DisassemblyPadding;
        internal FlagPanel PanelFlags;
        private const int DOCK_TOP_Y = 45;
        private const int DOCK_BOT_Y = 510;
        private const int DOCK_LEFT_X = 40;
        private const int DOCK_RIGHT_X = 675;
        private readonly Point Dock_LeftTop = new Point(DOCK_LEFT_X, DOCK_TOP_Y);
        private readonly Point Dock_LeftBot = new Point(DOCK_LEFT_X, DOCK_BOT_Y);
        private readonly Point Dock_RightTop = new Point(DOCK_RIGHT_X, DOCK_TOP_Y);
        private readonly Point Dock_RightBot = new Point(DOCK_RIGHT_X, DOCK_BOT_Y);
        private void InitialiseCustom()
        {
            CreateMenuBar();
            CreateMemView();
            CreatePanelRegisters();
            CreatePanelFlags();
            CreateControlButtons();
            CreateDisassemblyView();
        }
        private void CreateDisassemblyView()
        {
            ListViewDisassembly = new DisassemblyListView(d.ParsedLines, new Size(620, 382))
            {
                Location = new Point(0, 0),
                BackColor = LayerBrush.Color,
            };

            ListViewDisassembly.OnAddressClicked += VMInstance.ToggleBreakpoint;
            DisassemblyPadding = new Panel()
            {
                Location = new Point(15, 15),
                Size = new Size(583, 382) // slightly smaller + offsetted to prevent overlapping the border
            };
            
            DisassemblyBorder = new BorderedPanel()
            {
                BackColor = LayerBrush.Color,
                Location = Dock_LeftTop,
                Size = new Size(600, 400),
                Tag = "Disassembly"
            };
            DisassemblyPadding.Controls.Add(ListViewDisassembly);
            DisassemblyBorder.Controls.Add(DisassemblyPadding);
            Controls.Add(DisassemblyBorder);
        }
        private void CreateControlButtons()
        {
            ButtonStep = new ControlButton()
            {
                Location = new Point(600 + 80 * 1, 450),           
                Text = "Step",
                
            };
            ButtonStep.Click += new EventHandler(VMContinue_ButtonEvent);
            ButtonRun = new ControlButton()
            {
                Location = new Point(600 + 80 * 2, 450),
                Text = "Run",

            };
            ButtonRun.Click += new EventHandler(VMContinue_ButtonEvent);
            ButtonReset = new ControlButton()
            {
                Location = new Point(600 + 80 * 3, 450),
                Text = "Reset"
            };            
            ButtonReset.Click += new EventHandler(Reset_Click);
            Controls.Add(ButtonStep);
            Controls.Add(ButtonRun);
            Controls.Add(ButtonReset);
        }
        private void CreatePanelFlags()
        {
            PanelFlags = new FlagPanel()
            {
                Tag = "Flags",
                Location = Dock_RightBot
            };
            Controls.Add(PanelFlags);
        }
        private void CreatePanelRegisters()
        {
            PanelRegisters = new RegisterPanel()
            {
                Tag = "Registers",
                Location = Dock_RightTop,
            };
            PanelRegisters.OnRegSizeChanged += RefreshRegisters;
            Controls.Add(PanelRegisters);
        }
        private void CreateMemView()
        {
            PanelMemory = new BorderedPanel
            {
                Location = Dock_LeftBot,
                AutoSize = false,
                Size = new Size(0x1a0, 383), // for some reason makes itsself bigger later?? looks like (size of child + c)
                TabIndex = 28,
                Tag = "Memory"
            };
            memviewer = new MemoryListView(new Size(0x1d8, 383))
            {
                Location = new Point(3, 16),
            };
            PanelMemory.Controls.Add(memviewer);
            Controls.Add(PanelMemory);
        }
        private void CreateMenuBar()
        {
            TopMenuStrip = new ThemedMenuStrip(Layer.Background, Emphasis.Medium)
            {
                Location = new Point(0, 0),
                Name = "TopMenuStrip",
                Size = new Size(Width, 24),   
                
            };
            MainMenuStrip = TopMenuStrip;
            Controls.Add(TopMenuStrip);
            CreateMenuFile();
            CreateMenuDebug();
            CreateMenuExit();
            CreateMenuView();
            TopMenuStrip.Items.AddRange(new ThemedToolStripMenuHeader[] { FileMenu, DebugMenu, ViewMenu, ExitMenu });
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
        private void CreateMenuFile()
        {
            FileMenu = new ThemedToolStripMenuHeader()
            {
                Name = "FileMenuItem",
                Size = CorrectedMeasureText(" File ", BaseUI.BaseFont),
                Text = "File",
            };
            ThemedToolStripMenuItem OpenMenu = new ThemedToolStripMenuItem()
            {
                Text = "Open"
            };            
            OpenMenu.Click += (s, a) => 
            {
                OpenFileDialog OpenFile = new OpenFileDialog();
                OpenFile.ShowDialog();
                FlashFromFile(OpenFile.FileName);
            };
            ThemedToolStripMenuItem OpenClipboardMenu = new ThemedToolStripMenuItem()
            {
                Text = "Open from clipboard"
            };
            OpenClipboardMenu.Click += (s, a) =>
            {
                IO.TXT ParsedClipboard = IO.TXT.Parse(System.Text.Encoding.UTF8.GetBytes(Clipboard.GetText(TextDataFormat.UnicodeText)));
                if (ParsedClipboard == null)
                {
                    // This is already handled in the TXT class, but don't commit the changes to the VM if it was unsuccessful.
                    return;
                }
                FlashProcedure(ParsedClipboard.Instructions);
            };
            FileMenu.DropDownItems.AddRange(new[] { OpenMenu, OpenClipboardMenu });
        }
        private void CreateMenuDebug()
        {
            // A work around for SelectDebugMenuItem.Dropdown opening when 's' is used in search, this is selected instead, which does nothing.
            ToolStripMenuItem FillerDebugItem = new ToolStripMenuItem() { Text = "S", Size = new Size(0, 0), AutoSize = false };
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
            SearchDebugMenu = new TestcaseSearchTextBox(
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
}
