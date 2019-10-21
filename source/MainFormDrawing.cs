// MainFormDrawing sets up all of the custom controls in the form. This is kept in a different file because it is very spacious, however
// is the same class as MainForm due to the partial keyword.
using debugger.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;
using static debugger.Forms.FormSettings;
using static debugger.Util.Drawing;
namespace debugger
{
    public partial class MainForm
    {
        internal EndToolStripMenuItem ExitMenu;
        internal ThemedToolStripMenuHeader DebugMenu;
        internal ThemedToolStripMenuHeader FileMenu;
        internal ThemedMenuStrip TopMenuStrip;
        internal ThemedToolStripMenuItem SelectDebugMenu;
        internal ThemedToolStripMenuItem AllDebugMenu;
        internal SearchTextBox SearchDebugMenu;
        internal MemoryListView MemoryViewer;
        internal BorderedPanel PanelMemory;
        internal RegisterPanel PanelRegisters;
        internal ControlButton ButtonStep;
        internal ControlButton ButtonRun;
        internal ControlButton ButtonReset;
        internal DisassemblyListView ListViewDisassembly;
        internal BorderedPanel DisassemblyBorder;
        internal Panel DisassemblyPadding;
        internal FlagPanel PanelFlags;

        // These constants represent docking positions for controls. This means that if a new control was to be added, an existing control
        // could be hidden, freeing the dock position and the new one could go in its place. This ensures a consistent design.
        // To see visually, check here http://prntscr.com/pk6gff
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
            // When creating new methods to be called here, it is recommended that they are their own methods rather than having actual code here.
            // This makes it super easy to change the load order of controls, as some controls have to be loaded before others. To partly enforce this,
            // certain controls that depend on other are explicitly created afterwards in their method, however it does not always make logical sense to do so
            // as the two may seem unrealted to each other. A current example is the menu bar buttons(File, Debug, etc). They are created in CreateMenuBar() and
            // added to the menu bar after it is initialised.

            CreateMenuBar();
            CreateMemView();
            CreatePanelRegisters();
            CreatePanelFlags();
            CreateControlButtons();
            CreateDisassemblyView();
        }
        private void CreateDisassemblyView()
        {
            // Create the new list view with a reference to the disassembled lines. This is super important, see DisassemblyListView.
            ListViewDisassembly = new DisassemblyListView(DisassemblerInstance.ParsedLines, new Size(620, 382))
            {
                Location = new Point(0, 0),
                BackColor = LayerBrush.Color,
            };

            // When addresses are clicked, toggle them as breakpoints.
            ListViewDisassembly.OnAddressClicked += VMInstance.ToggleBreakpoint;

            // DisassemblyPadding is a necessary aethetic component of the DisassemblyListView. There is a scrollbar "feature" buried
            // into liewviews that doesn't fit into the program(especially with the dark theme it currently has) at all and looks
            // very out of place, which furthermore cannot be disabled without disabling scrolling entirely, which would
            // be even worse on a listview that is expected to hold all of the disassembly for a program. My workaround is to create
            // a padding that overlaps the scrollbar, therefore gives the best of both worlds as the listview is still scrollable.
            DisassemblyPadding = new Panel()
            {
                // Slightly smaller and offset from the listview to prevent overlapping the border.
                Location = new Point(ListViewDisassembly.Location.X + 15, ListViewDisassembly.Location.Y + 15),
                Size = new Size(583, 382)
            };

            // Create a nice border around the control.
            DisassemblyBorder = new BorderedPanel()
            {
                BackColor = LayerBrush.Color,
                Location = Dock_LeftTop,
                Size = new Size(600, 400),
                Tag = "Disassembly"
            };

            // Add the controls to the form. To ensure the controls are visible, the list view has to be a member
            // of the padding panel otherwise the panel with overlap it.
            DisassemblyPadding.Controls.Add(ListViewDisassembly);
            DisassemblyBorder.Controls.Add(DisassemblyPadding);
            Controls.Add(DisassemblyBorder);
        }
        private void CreateControlButtons()
        {
            // Create the the three control buttons: step, reset, and run
            // 680 is the "base position", from there each is offset by 80
            // This is because DisassemblyListView ends at around 600 px.
            ButtonStep = new ControlButton()
            {
                Location = new Point(600 + 80 * 1, 450),
                Text = "Step",

            };
            ButtonRun = new ControlButton()
            {
                Location = new Point(600 + 80 * 2, 450),
                Text = "Run",

            };
            ButtonReset = new ControlButton()
            {
                Location = new Point(600 + 80 * 3, 450),
                Text = "Reset"
            };

            // Register the correct events to the buttons.
            ButtonRun.Click += new EventHandler(VMContinue_ButtonEvent);
            ButtonStep.Click += new EventHandler(VMContinue_ButtonEvent);
            ButtonReset.Click += new EventHandler(Reset_Click);

            // This order does not matter.
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

            // This is when the user double clicks the registers.
            PanelRegisters.OnRegSizeChanged += RefreshRegisters;

            Controls.Add(PanelRegisters);
        }
        private void CreateMemView()
        {
            PanelMemory = new BorderedPanel
            {
                Location = Dock_LeftBot,
                AutoSize = false,

                // For an unknown reason, the panel will resize slightly more than this value. It seems to become the size of the child control
                // plus an extra constant. This happens regardless of $AutoSize.
                Size = new Size(0x1a0, 383),
                Tag = "Memory"
            };
            MemoryViewer = new MemoryListView(new Size(0x1d8, 383))
            {
                // This position is relative to the panel. A small offset makes sure that the 
                // text and border will not be clipped over.
                Location = new Point(3, 16),
            };

            PanelMemory.Controls.Add(MemoryViewer);
            Controls.Add(PanelMemory);
        }
        private void CreateMenuBar()
        {
            // Create the menu strip at the top corner. It has a layer of foreground to make it stand out from the background.
            TopMenuStrip = new ThemedMenuStrip(Layer.Foreground, Emphasis.Medium)
            {
                Location = new Point(0, 0),
                Name = "TopMenuStrip",
                Size = new Size(Width, 24),

            };


            // The order of these methods is not important, however they do have to be called before AddRange().
            CreateMenuFile();
            CreateMenuDebug();
            CreateMenuExit();
            TopMenuStrip.Items.AddRange(new ThemedToolStripMenuHeader[] { FileMenu, DebugMenu });

            // This being separated from the above is super imporant. See EndToolStripMenuItem            
            TopMenuStrip.Items.Add(ExitMenu);

            Controls.Add(TopMenuStrip);
        }
        private void CreateMenuExit()
        {
            ExitMenu = new EndToolStripMenuItem()
            {
                DrawingLayer = Layer.Imminent,
                Name = "ExitMenuItem",

                // Add spaces to give some extra room to the menu.
                Size = CorrectedMeasureText(" Exit ", BaseUI.BaseFont),

                Text = "Exit",
                TextEmphasis = Emphasis.Medium
            };

            // Exit once clicked.
            ExitMenu.Click += (s, a) => Environment.Exit(0);
        }
        private void CreateMenuFile()
        {
            FileMenu = new ThemedToolStripMenuHeader()
            {
                Name = "FileMenuItem",

                // Add spaces to give some extra room to the menu.
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

                // This will set OpenFile.FileName to the chosen path.
                OpenFile.ShowDialog();

                // "" is returned when the user closes the DialogBox with alt f4 or the X icon. It was annoying when
                // the error message showed up after doing this.
                if (OpenFile.FileName != "")
                {
                    // This should not really be handled here, rather in the main class file.
                    FlashFromFile(OpenFile.FileName);
                }

            };
            ThemedToolStripMenuItem OpenClipboardMenu = new ThemedToolStripMenuItem()
            {
                Text = "Open from clipboard"
            };
            OpenClipboardMenu.Click += (s, a) =>
            {
                // Parse the clip board as text.
                IO.TXT ParsedClipboard = IO.TXT.Parse(System.Text.Encoding.UTF8.GetBytes(Clipboard.GetText(TextDataFormat.UnicodeText)));

                // If the output is null, there was an error somewhere, but this is already handled in the TXT class.
                // Regardless, the VM will be unchanged in this case.
                if (ParsedClipboard != null)
                {
                    // This should not really be handled here, rather in the main class file.
                    FlashProcedure(ParsedClipboard.Instructions);
                }
            };

            // It is important this is called after both of these are initialised.
            FileMenu.DropDownItems.AddRange(new[] { OpenMenu, OpenClipboardMenu });
        }
        private void CreateMenuDebug()
        {
            // A work around for SelectDebugMenuItem.Dropdown opening when 's' is used in search, this is selected instead, which does nothing.
            // A work around for a very annoying windows form feature. When pressing a key such as "a" or "b", if there is a control whose name
            // begins with that letter, it will have OnClick called. However it was extremely annnoying when using the testcase search, as if 
            // you tried to search for a testcase with an 'a' in its name, suddenly every testcase would run as All.OnClick was called(Same would happen with Select).
            // I could find very little about this online or any kind of fix, so this is the best I could come up with. As ToolStripMenuItem is no button, when its
            // corresponding key is pressed, nothing will happen. I assume windows forms recognises there could be some ambiguity here with multiple controls and corrects itself.  
            // The workaround is to create these two dummy buttons to artificially create this effect. They are sized as 0 to prevent them being seen, however setting $Visible
            // to false would null the effect.
            ToolStripMenuItem FillerDebugItemSelect = new ToolStripMenuItem() { Text = "S", Size = new Size(0, 0), AutoSize = false };
            ToolStripMenuItem FillerDebugItemAll = new ToolStripMenuItem() { Text = "A", Size = new Size(0, 0), AutoSize = false };

            // Create a constant size for all items initially.
            Size ItemSize = new Size(70, 20);

            SelectDebugMenu = new ThemedToolStripMenuItem()
            {
                Size = ItemSize,
                Text = "Select"
            };

            // Create a new menu item for each testcase available and add them as subitems to the dropdown of the Select menu.
            string[] Testcases = Hypervisor.TestHandler.GetTestcases();
            for (int i = 0; i < Testcases.Length; i++)
            {
                ThemedToolStripMenuItem ToAdd = new ThemedToolStripMenuItem()
                {
                    // Set the text to the name of the testcase. 
                    Text = Testcases[i],
                    Size = ItemSize
                };

                // When the button is clicked, call the testcase it corresponds to.
                ToAdd.Click += (s, a) => OnTestcaseSelected(ToAdd.Text);

                SelectDebugMenu.DropDownItems.Add(ToAdd);
            }
            AllDebugMenu = new ThemedToolStripMenuItem()
            {
                Size = ItemSize,
                Text = "All"
            };
            AllDebugMenu.Click += (s, a) => OnTestcaseSelected("all");

            // See SearchTextBox for information on this constructor.
            SearchDebugMenu = new SearchTextBox(
                () => Hypervisor.TestHandler.GetTestcases(),
                Layer.Background,
                Emphasis.Medium)
            {
                Size = ItemSize
            };
            SearchDebugMenu.OnResultClicked += OnTestcaseSelected;
            // Create the header
            DebugMenu = new ThemedToolStripMenuHeader()
            {
                Name = "DebugMenuItem",

                // Add some extra padding either side
                Size = CorrectedMeasureText(" Debug ", BaseUI.BaseFont),

                Text = "Debug",
            };

            // Call keypressed on the search menu when a key is pressed in the dropdown. This means that the user does not specifically have to
            // hover over the search button to search; only the dropdown has to have focus.
            DebugMenu.DropDown.PreviewKeyDown += SearchDebugMenu.KeyPressed;

            // This must be called after all controls have finished initialising.
            DebugMenu.DropDownItems.AddRange(new ToolStripItem[] { FillerDebugItemAll, FillerDebugItemSelect, AllDebugMenu, SelectDebugMenu, SearchDebugMenu });
        }
    }
}
