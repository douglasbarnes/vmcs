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
using debugger.Forms;
using System.Diagnostics;

namespace debugger
{  
    public partial class MainForm : Form
    {

        public readonly string[] SpecialRegisterNames = { "SP", "BP", "SI", "DI" };
        public readonly string[] GeneralRegisterNames = { "AX", "BX", "" };
        public Dictionary<string, ulong> Registers = new Dictionary<string, ulong>();
        public MainForm()
        {
            Font = FormSettings.BaseUI.BaseFont;
            SuspendLayout();
            InitializeComponent();
            InitialiseCustom();            
            MouseDoubleClick += (s, e) => Trace.WriteLine($"X: {e.X} Y: {e.Y}");
            ForeColor = FormSettings.BaseUI.SurfaceColour;
            BackColor = FormSettings.BaseUI.BackgroundColour;
        }
        VM VMInstance;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0xB8, 0x00, 0x01, 0x00, 0x00, 0xBB, 0xFF, 0x00, 0x00, 0x00, 0x67, 0x89, 0x58, 0x50, 0x67, 0x89, 0x1C, 0x45, 0x00, 0x00, 0x00, 0x00, 0x67, 0x89, 0x1C, 0x45, 0x00, 0x01, 0x00, 0x00, 0x67, 0x89, 0x1C, 0x85, 0x00, 0x00, 0x00, 0x20, 0x67, 0x89, 0x1C, 0xC3, 0x67, 0x89, 0x9C, 0x18, 0x00, 0x01, 0x00, 0x00, 0x67, 0x89, 0x5C, 0x45, 0x10, 0x67, 0x8A, 0x4C, 0x45, 0x10, 0x90 }





);
            VMInstance = new VM(ins);
            VMInstance.RunComplete += (context) =>
            {
                RefreshCallback();
                ListViewDisassembly.SetRIP(context.InstructionPointer);
            };            
            VMInstance.Breakpoints.ListChanged += (s, lc_args) => ListViewDisassembly.Refresh();
            ListViewDisassembly.BreakpointSource = VMInstance.Breakpoints;
            Task.Run(() =>
            {
                using (Disassembler DisassemblerInstance = new Disassembler(VMInstance.Handle))
                {
                    ListViewDisassembly.AddParsed(DisassemblerInstance.StepAll().Result);

                }
                ListViewDisassembly.SetRIP(VMInstance.GetMemory().EntryPoint);
            });
            RefreshCallback();
            ResumeLayout();
            Refresh();
            Update();
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
            PanelRegisters.Invoke(new Action(() => PanelRegisters.UpdateRegisters(VMInstance.GetRegisters((RegisterCapacity)PanelRegisters.RegSize))));
        }
        private void RefreshRegisters(int size)
        {
            Registers = VMInstance.GetRegisters((RegisterCapacity)size); //qword regs
            PanelRegisters.Invoke(new Action(() => PanelRegisters.UpdateRegisters(Registers)));
            //a little better than without specifying because less time is spent in the invoke, so we will have less stalls for the thread
        }
        private void RefreshFlags()
        {            
            Dictionary<string, bool> FetchedFlags = VMInstance.GetFlags();
            PanelFlags.Invoke(new Action(() => PanelFlags.UpdateFlags(FetchedFlags)));          
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
        private async void RefreshCallback()
        {
            List<Task> RefreshTasks = new List<Task>
            {
                new Task(() => RefreshRegisters()),
                new Task(() => RefreshMemory()),
                new Task(() => RefreshFlags())
            };
            RefreshTasks.ForEach(x => x.Start());
            await Task.WhenAll(RefreshTasks);
            Invoke(new Action(() => Refresh()));
        }
        //     
        private void SetMemviewPos(object sender, EventArgs e)
        {

            string GotoInput = "";//gotoMemSrc.Text; abandoned
            if (GotoInput.Length >= 2 && GotoInput.Substring(0,2).ToLower() == "0x") { GotoInput = GotoInput.Substring(2); }
            GotoInput = GotoInput.PadLeft(16, '0');
            //if it is a name of reg
            if (Registers.ContainsKey(GotoInput)) { GotoInput = Registers[GotoInput].ToString("X"); }
            if (Registers.ContainsKey(GotoInput)) { GotoInput = Registers[GotoInput].ToString("X"); }

            if (GotoInput.Where(x => !"1234567890ABCDEF".Contains(x) ).Count() != 0) {  } else
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
        private void Reset_Click(object sender, EventArgs e) { VMInstance.Reset(); RefreshCallback(); }
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Environment.Exit(0);

    }
   
}
