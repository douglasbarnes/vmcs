using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using debugger.Util;
using debugger.Hypervisor;
using debugger.Emulator;
using static debugger.FormSettings;

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
            CustomDraw();
            ForeColor = BaseUI.SurfaceColour;
            BackColor = BaseUI.BackgroundColour;
        }
        VM VMInstance;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0x48, 0xB8, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0x50, 0x66, 0xFF, 0x35, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x35, 0x00, 0x00, 0x00, 0x00, 0x58, 0x66, 0x5B, 0x90, 0x48, 0xB8, 0x18, 0x29, 0x3A, 0x3B, 0x4C, 0x4D, 0x4E, 0x4F, 0x66, 0x50, 0x50, 0x5B, 0x66, 0x59, 0x90, 0x6A, 0xF2, 0x66, 0x68, 0x34, 0x12, 0x68, 0x78, 0x56, 0x34, 0x12, 0x58, 0x66, 0x5B, 0x59, 0x5A, 0x90 }






);
            VM.VMSettings settings = new VM.VMSettings()
            {
                UndoHistoryLength = 5,
                RunCallback = RefreshAll
            };
            VMInstance = new VM(settings, ins);
            ListViewDisassembly.BreakpointSource = VMInstance.Breakpoints;
            ListViewDisassembly.BreakpointSource.ListChanged += (s, lc_args) => Refresh();
            PanelRegisters.OnRegSizeChanged += RefreshRegisters;
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
            RegisterCapacity RegCap = RegisterCapacity.GP_QWORD;
            Registers = VMInstance.GetRegisters(RegCap); //qword regs
            PanelRegisters.Invoke(new Action(() => RegCap = (RegisterCapacity)PanelRegisters.RegSize));
            string[] ParsedRegValues = new string[9];
            for (int i = 0; i < ParsedRegValues.Length; i++)
            {
                string Value;
                if(i == 0) //always format rip the same
                {
                    Value = Core.FormatNumber(Registers["RIP"], SelectedFormatType);
                    if (SelectedFormatType == FormatType.Hex)
                    {
                        Value = Value.Insert(2, "%").Insert(0, "%");
                    }
                }
                else if (RegCap == RegisterCapacity.GP_BYTE & i > 4) //higher bit reg
                {
                    Value = Core.FormatNumber(Registers[Disassembly.DisassembleRegister((XRegCode)i-5, RegisterCapacity.GP_QWORD, REX.B)], SelectedFormatType);
                    if (SelectedFormatType == FormatType.Hex)
                    {
                        Value = Value.Insert(16, "%").Insert(14, "%").Insert(0, "%");
                    }
                } else
                {      //anything else                     
                    Value = Core.FormatNumber(Registers[Disassembly.DisassembleRegister((XRegCode)i-1, RegisterCapacity.GP_QWORD, REX.NONE)], SelectedFormatType);
                    if (SelectedFormatType == FormatType.Hex)
                    {
                        Value = Value.Insert(2 + 16 - ((byte)RegCap * 2), "%").Insert(0, "%");
                    }
                }                
                ParsedRegValues[i] = Value;
            }
            Invoke(new Action(() =>
            {
                Label[] LabelOrder = new Label[] { RIPLABEL, RSPLABEL, RBPLABEL, RSILABEL, RDILABEL, RAXLABEL, RBXLABEL, RCXLABEL, RDXLABEL };
                for (int i = 0; i < LabelOrder.Length; i++)
                {
                    string Mnemonic = (i == 0) ? "RIP" : Disassembly.DisassembleRegister((XRegCode)i-1, RegCap, REX.B);
                    LabelOrder[i].Text = $"{Mnemonic}{(((byte)RegCap < 4 & i != 0) ? " " : "")} : {ParsedRegValues[i]}";
                }
                PanelRegisters.Refresh();           //extra space so stuff stays in line
            })); 
        }
        private void RefreshFlags()
        {            
            Dictionary<string, bool> FetchedFlags = VMInstance.GetFlags();
            
            Invoke(new Action(() =>
            {
                Label[] LabelOrder = new Label[] { LabelCarry, LabelOverflow, LabelSign, LabelZero, LabelParity, LabelAuxiliary };
                for (int i = 0; i < LabelOrder.Length; i++)
                {
                    string Flag = LabelOrder[i].Tag.ToString();
                    LabelOrder[i].Text = $"{Flag.PadRight(9)}: ";
                    LabelOrder[i].Text += (FetchedFlags[Flag] == true) ? "True" : "$False$";
                }
            }));            
        } 
        private void RefreshMemory()
        {
            SortedDictionary<ulong, byte> _memory = new SortedDictionary<ulong, byte>((Dictionary<ulong,byte>)VMInstance.GetMemory());
            ulong _currentaddr = _memory.First().Key;
            StringBuilder _currentline = new StringBuilder();
            memviewer.Invoke(new Action(( () => {
                memviewer.Items.Clear();
                foreach (var address in _memory)
                {
                    if (_currentline.Length >= 48 || _currentaddr + 16 < address.Key)
                    {
                        if (_currentline.Length < 48) { _currentline.Append(string.Join("", Enumerable.Repeat("00 ", (48 - _currentline.Length) / 3))); }
                        memviewer.Items.Add(new ListViewItem(new string[] { $"0x{_currentaddr.ToString("X").PadLeft(16, '0')}", _currentline.ToString() }));

                        if (_currentaddr + 16 < address.Key)
                        {
                            memviewer.Items.Add(new ListViewItem(new string[] { $"[+{(address.Key - _currentaddr).ToString("X")}]", "" }));
                        }

                        _currentline = new StringBuilder();
                        _currentaddr = address.Key;
                    }
                    _currentline.Append(address.Value.ToString("X").PadLeft(2, '0') + " ");



                }
                if (_currentline.Length < 48) { _currentline.Append(string.Join("", Enumerable.Repeat("00 ", (48 - _currentline.Length) / 3))); }
                memviewer.Items.Add(new ListViewItem(new[] { $"0x{_currentaddr.ToString("X").PadLeft(16, '0')}", _currentline.ToString() }));
            })));                    
        }      
        private void RefreshDisassembly()
        {
            using (Disassembler DisassemblerInstance = new Disassembler(VMInstance.Handle))
            {
                ListViewDisassembly.AddParsed(DisassemblerInstance.StepAll().Result);           
            }                
        }
        private void RefreshTestcases()
        {

        }
        private async void RefreshAll()
        {
            List<Task> RefreshTasks = new List<Task>
            {
                new Task(() => RefreshDisassembly()),
                new Task(() => RefreshRegisters()),
                new Task(() => RefreshMemory()),                
                new Task(() => RefreshFlags())
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
        private const string ResultOutputPath = "Results\\";
        private void OnTestcaseSelected(string name)
        {
            XElement Result;
            if (name == "all")
            {
                string OutputPath = ResultOutputPath + "AllTestcases.xml";
                if (new FileInfo(OutputPath) == null)
                {
                    throw new Exception("Invalid output path");
                } else
                {
                    Task.Run(async () =>
                    {
                        Result = await TestHandler.ExecuteAll();
                        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                        Result.Save(OutputPath);
                        
                        MessageBox.Show("Results written to " + OutputPath, Result.Attribute("result").Value);
                    });
                }                
            } else
            {
                Task.Run(async () =>
                {                    
                    Result = await TestHandler.ExecuteTestcase(name);
                    string OutputPath = ResultOutputPath + $"{name}Testcase.xml";
                    Result.Save(OutputPath);
                    if (MessageBox.Show($"Click No to see full results", name + " " + Result.Attribute("result").Value.ToString().ToLower(), MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        MessageBox.Show(Result.ToString());
                    }
                });
            }
                       
        }
        private void Reset_Click(object sender, EventArgs e) { VMInstance.Reset(); RefreshAll(); }
        //Format methods
        private FormatType SelectedFormatType = FormatType.Hex;        
        //
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Environment.Exit(0);

    }
   
}
