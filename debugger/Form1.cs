using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace debugger
{
    public partial class Form1 : Form
    {
        // list 1: register sets at each stage of program, list 2: 0=special regs 1=general, dictionary "regname":regval
        public List<List<Dictionary<string, ulong>>> SavedRegisters = new List<List<Dictionary<string, ulong>>>();
        public Form1()
        {
            InitializeComponent();
        }
        VM Emulator;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0xB8, 0x9F, 0x00, 0x04, 0x00, 0xBB, 0x9F, 0x00, 0x05, 0x00, 0x67, 0xC7, 0x00, 0x10, 0x00, 0x00, 0x00, 0x67, 0xC7, 0x03, 0x20, 0x00, 0x00, 0x00, 0x50, 0x67, 0x8B, 0x03, 0x58, 0x67, 0x8B, 0x18, 0xF7, 0xE3, 0xF7, 0xF3, 0x90, 0x67, 0xC7, 0x00, 0xAA, 0xAA, 0xAA, 0xAA, 0x67, 0x81, 0x08, 0xCC, 0xCC, 0xCC, 0xCC, 0x67, 0x81, 0x28, 0xBB, 0xBB, 0xBB, 0xBB, 0x67, 0x8B, 0x08, 0x67, 0xC7, 0x00, 0xEE, 0xEE, 0xEE, 0xEE, 0x67, 0x81, 0x00, 0xAA, 0xAA, 0xAA, 0xAA }
);            
            Emulator = new VM(ins);
            _refresh();
        }

        private void Step_Click(object sender, EventArgs e)
        {
           Emulator.Step();
            _refresh();
        }
        private void _refresh(object sender = null, EventArgs e = null)
        {
            //regs
            SavedRegisters.Add(Emulator.GetRegisters());
            int iCurrentIndex = SavedRegisters.Count() - 1;
            if(iCurrentIndex == 4)
            {
                SavedRegisters.RemoveAt(0);
                iCurrentIndex--;
            }
            
            if (IsDec.Checked)
            {                                         //currentindex spec 
                specialregs.DataSource = SavedRegisters[iCurrentIndex][0].Select(x => x.Key + ": " + x.Value.ToString()).ToList();
                generalregs.DataSource = SavedRegisters[iCurrentIndex][1].Select(x => x.Key + ": " + x.Value.ToString()).ToList();
            }
            else
            {
                specialregs.DataSource = SavedRegisters[iCurrentIndex][0].Select(x => x.Key + ": 0x" + x.Value.ToString("X").PadLeft(16, '0')).ToList();
                generalregs.DataSource = SavedRegisters[iCurrentIndex][1].Select(x => x.Key + ": 0x" + x.Value.ToString("X").PadLeft(16, '0')).ToList();
            }

            //memory
            SortedDictionary<ulong, byte> _memory = new SortedDictionary<ulong, byte>(Emulator.GetMemory());
            ulong _currentaddr = _memory.First().Key;
            StringBuilder _currentline = new StringBuilder();
            memviewer.Items.Clear();
            foreach(var address in _memory)
            {
                if(_currentline.Length >= 48 || _currentaddr + 32 < address.Key)
                {                  
                    if(_currentline.Length < 48) { _currentline.Append(string.Join("", Enumerable.Repeat("00 ", (48 - _currentline.Length)/3 ))); }
                    memviewer.Items.Add(new ListViewItem(new[] { $"0x{_currentaddr.ToString("X").PadLeft(16,'0')}", _currentline.ToString() }));

                    if (_currentaddr + 64 < address.Key)
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

        private void Button2_Click(object sender, EventArgs e)
        { 
            Emulator.Run();
        }

        private void SetMemviewPos(object sender, EventArgs e)
        {

            string input = gotoMemSrc.Text;
            if (input.Length >= 2 && input.Substring(0,2).ToLower() == "0x") { input = input.Substring(2); }
            input = input.PadLeft(16, '0');
            //if it is a name of reg
            int iCurrentIndex = SavedRegisters.Count() - 1;
            if (SavedRegisters[0][0].ContainsKey(input)) { input = SavedRegisters[iCurrentIndex][0][input].ToString("X"); }
            if (SavedRegisters[0][1].ContainsKey(input)) { input = SavedRegisters[iCurrentIndex][1][input].ToString("X"); }

            if (input.Where(x => !"1234567890ABCDEF".Contains(x) ).Count() != 0) { gotoMemSrc.Text = "Invalid address"; } else
            {
                ulong inputAddr = Convert.ToUInt64(input, 16);

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

    }
}
