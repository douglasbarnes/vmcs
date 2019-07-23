using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static debugger.FormSettings;
using static debugger.CustomControls;
using static debugger.Primitives;
using static debugger.Util.Core;
namespace debugger
{  
    public partial class MainForm : Form
    {

        public readonly string[] SpecialRegisterNames = { "SP", "BP", "SI", "DI" };
        public readonly string[] GeneralRegisterNames = { "AX", "BX", "" };
        public Dictionary<string, ulong> Registers = new Dictionary<string, ulong>();
        public MainForm()
        {
            Font = BaseUI.BaseFont;           
            InitializeComponent();
            ForeColor = BaseUI.SurfaceColour;
            BackColor = BaseUI.BackgroundColour;
            foreach (Control BackgroundControl in Controls)
            {
                BackgroundControl.BackColor = BaseUI.BackgroundColour;
                if (BackgroundControl.HasChildren)
                {
                    
                    BackgroundControl.ForeColor = SurfaceShades[1];
                    foreach(Control NestedControl in BackgroundControl.Controls)
                    {
                        NestedControl.BackColor = BaseUI.BackgroundColour;
                        NestedControl.ForeColor = SurfaceShades[2];
                        
                    }
                } else
                {
                    BackgroundControl.ForeColor = SurfaceShades[0];
                }             
            }
           
        }
        VM VMInstance;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0xB8, 0x10, 0x00, 0x00, 0x00, 0xBB, 0x20, 0x00, 0x00, 0x00, 0xBA, 0xAA, 0x00, 0x00, 0x00, 0x67, 0x89, 0x14, 0x45, 0x00, 0x00, 0x00, 0x00, 0x67, 0x89, 0x14, 0x45, 0x00, 0x01, 0x00, 0x00, 0x67, 0x89, 0x14, 0x43, 0x67, 0x89, 0x94, 0x43, 0x00, 0x01, 0x00, 0x00, 0x89, 0x94, 0x45, 0x00, 0x01, 0x00, 0x00 });
            VM.VMSettings settings = new VM.VMSettings()
            {
                UndoHistoryLength = 5,
                RunCallback = RefreshAll
            };
            VMInstance = new VM(settings, ins);
            ListViewDisassembly.BreakpointsSource = VMInstance.Breakpoints;
            RefreshAll();            
        }
        private void VMContinue_ButtonEvent(object sender, EventArgs e)
        {
            //sender is name of button
            VMContinue(((Control)sender).Text == "Step"); // bool  1=step
        }
        private void VMContinue(bool Step)
        {
            VMInstance.RunAsync(Step);
        }
 
        //refresh methods
        private void RefreshRegisters()
        {
            Registers = VMInstance.GetRegisters(8); //qword regs
            foreach (var Register in Registers)
            {
                foreach(RegisterLabel Label in PanelRegisters.Controls)
                {
                    if(Label.Name == $"{Register.Key}LABEL")
                    {
                        Label.Invoke((Action)(() => {Label.Text = $"{Register.Key} : {FormatNumber(Register.Value, SelectedFormatType)}"; Label.Refresh(); }));
                    }
                }
            }      
        }
        private void RefreshFlags()
        {
            
            Dictionary<string, bool> FetchedFlags = VMInstance.GetFlags();
            LabelCarry.Text =    $"Carry    : {FetchedFlags["CF"].ToString()}";
            LabelOverflow.Text = $"Overflow : {FetchedFlags["OF"].ToString()}";
            LabelSign.Text =     $"Sign     : {FetchedFlags["SF"].ToString()}";
            //col2
            LabelZero.Text =     $"Zero      : {(!FetchedFlags["ZF"]).ToString()}";
            LabelParity.Text =   $"Parity    : {FetchedFlags["PF"].ToString()}";
            LabelAuxiliary.Text =$"Auxiliary : {FetchedFlags["AF"].ToString()}";
        }
        private void RefreshMemory()
        {
            SortedDictionary<ulong, byte> _memory = new SortedDictionary<ulong, byte>((Dictionary<ulong,byte>)VMInstance.GetMemory());
            ulong _currentaddr = _memory.First().Key;
            StringBuilder _currentline = new StringBuilder();
            memviewer.Items.Clear();
            foreach (var address in _memory)
            {
                if (_currentline.Length >= 48 || _currentaddr + 16 < address.Key)
                {
                    if (_currentline.Length < 48) { _currentline.Append(string.Join("", Enumerable.Repeat("00 ", (48 - _currentline.Length) / 3))); }
                    memviewer.Items.Add(new ListViewItem(new[] { $"0x{_currentaddr.ToString("X").PadLeft(16, '0')}", _currentline.ToString() }));

                    if (_currentaddr + 16 < address.Key)
                    {
                        memviewer.Items.Add(new ListViewItem(new[] { $"[+{(address.Key - _currentaddr).ToString("X")}]", "" }));
                    }

                    _currentline = new StringBuilder();
                    _currentaddr = address.Key;
                }
                _currentline.Append(address.Value.ToString("X").PadLeft(2, '0') + " ");



            }
            if (_currentline.Length < 48) { _currentline.Append(string.Join("", Enumerable.Repeat("00 ", (48 - _currentline.Length) / 3))); }
            memviewer.Items.Add(new ListViewItem(new[] { $"0x{_currentaddr.ToString("X").PadLeft(16, '0')}", _currentline.ToString() }));
        }      
        private void RefreshDisassembly()
        {
            using (Disassembler DisassemblerInstance = new Disassembler(VMInstance.Handle))
            {
                ListViewDisassembly.AddressedItems = DisassemblerInstance.StepAll().Result;
            }                
        }
        private async void RefreshAll()
        {
            List<Task> RefreshTasks = new List<Task>
            {
                new Task(() => RefreshDisassembly()),
                new Task(() => RefreshRegisters()),
                new Task(() => RefreshMemory()),                
                new Task(() => RefreshFlags()) };

            RefreshTasks = new List<Task>
            {
                new Task(() => RefreshDisassembly()),
                new Task(() => RefreshRegisters()),
            };
            RefreshTasks.ForEach(x => x.Start());
            await Task.WhenAll(RefreshTasks);
            Invoke(new Action(() =>Refresh()));
        }
        //     
        private void SetMemviewPos(object sender, EventArgs e)
        {

            string GotoInput = gotoMemSrc.Text;
            if (GotoInput.Length >= 2 && GotoInput.Substring(0,2).ToLower() == "0x") { GotoInput = GotoInput.Substring(2); }
            GotoInput = GotoInput.PadLeft(16, '0');
            //if it is a name of reg
            if (Registers.ContainsKey(GotoInput)) { GotoInput = Registers[GotoInput].ToString("X"); }
            if (Registers.ContainsKey(GotoInput)) { GotoInput = Registers[GotoInput].ToString("X"); }

            if (GotoInput.Where(x => !"1234567890ABCDEF".Contains(x) ).Count() != 0) { gotoMemSrc.Text = "Invalid address"; } else
            {
                ulong inputAddr = Convert.ToUInt64(GotoInput, 16);

                //find closest
                ulong closestAddr = 0;
                ulong closestDiff = ulong.MaxValue;
                for (int iMemIndex = 0; iMemIndex < memviewer.Items.Count; iMemIndex++) // o(n) search
                {
                    if (memviewer.Items[iMemIndex].SubItems[0].Text[0] == '[') { continue; } //[+x] skip these
                    memviewer.Items[iMemIndex].BackColor = SystemColors.Window; //reset if was selected by anything

                    ulong currentAddr = Convert.ToUInt64(memviewer.Items[iMemIndex].SubItems[0].Text, 16);
                    ulong currentDiff = (ulong)Math.Abs((long)(currentAddr - inputAddr));
                    if (currentDiff < closestDiff)
                    {
                        closestAddr = currentAddr;
                        closestDiff = currentDiff;
                    }                   
                }
                int targetIndex = memviewer.Items.IndexOf(memviewer.FindItemWithText($"0x{closestAddr.ToString("X").PadLeft(16, '0')}"));
                memviewer.EnsureVisible(targetIndex);
                memviewer.SelectedItems.Clear();
                memviewer.Items[targetIndex].BackColor = Color.SlateGray;
            }

            

            
        }
        private void GotoMemSrc_MouseEnter(object sender, EventArgs e)
        {
            if(gotoMemSrc.Text == "Invalid address") { gotoMemSrc.Clear(); }
        }
        private void OnTestcaseSelected(object sender, EventArgs e)
        {
            MessageBox.Show(sender.ToString());
        }
        private void Reset_Click(object sender, EventArgs e)
        {

        }
        //Format methods
        private FormatType SelectedFormatType = FormatType.Hex;        
        private void MenuFormatChanged(object sender, EventArgs e)
        {
            switch(sender.ToString())
            {
                case "Hexadecimal":
                    MenuFormatDecimal.Checked = false;
                    MenuFormatString.Checked = false;
                    MenuFormatHex.Checked = true;
                    SelectedFormatType = FormatType.Hex;
                    break;
                case "Decimal":
                    MenuFormatDecimal.Checked = false;
                    MenuFormatString.Checked = false;
                    MenuFormatHex.Checked = true;
                    SelectedFormatType = (MenuFormatSigned.Checked) ? FormatType.SignedDecimal : FormatType.Decimal;
                    break;
                case "String":
                    MenuFormatDecimal.Checked = false;
                    MenuFormatString.Checked = false;
                    MenuFormatHex.Checked = true;
                    SelectedFormatType = FormatType.String;
                    break;
                    //void menuformatsignedchanged doesnt hit any of these
            }
        }
        private void MenuFormatSignedChanged(object sender, EventArgs e)
        {
            MenuFormatDecimal.Checked = false;
            MenuFormatString.Checked = false;
            MenuFormatHex.Checked = true;
            if (sender.ToString() == "Signed")
            {
                SelectedFormatType = FormatType.SignedDecimal;
            } else
            {                
                SelectedFormatType = FormatType.Decimal;
            }
        }
        //
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
    public static class FormSettings
    {
        
        public enum Emphasis
        {
            Imminent = 0,
            High = 1,
            Medium = 2,
            Disabled = 3
        }
        public enum Layer
        {
            Imminent = 0,
            Surface = 1,
            Foreground = 2,
            Background = 3
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
                Color.FromArgb(97, BaseUI.SurfaceColour) // ~38%
            };
            TextBrushes = SurfaceShades.Select(x => new SolidBrush(x)).ToList();

            PrimaryBrush = new SolidBrush(BaseUI.PrimaryColour);
            
        }
    }
}
