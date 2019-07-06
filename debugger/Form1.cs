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
        public Form1()
        {
            InitializeComponent();
        }
        VM Emulator;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0xB8, 0x10, 0x00, 0x00, 0x00, 0xBB, 0x20, 0x00, 0x00, 0x00, 0x50, 0x53, 0x67, 0x89, 0x00, 0x67, 0x89, 0x1B, 0x90, 0x29, 0xC3, 0x83, 0xC3, 0x01, 0x67, 0x29, 0x18, 0x5B, 0x58, 0x67, 0x01, 0x04, 0x45, 0x00, 0x00, 0x00, 0x00, 0x67, 0x89, 0x1C, 0x5D, 0x00, 0x00, 0x00, 0x00, 0xF7, 0xE3, 0x89, 0x1C, 0x25, 0x00, 0x01, 0x00, 0x00, 0xB8, 0x10, 0x00, 0x00, 0x00, 0xBB, 0x04, 0x00, 0x00, 0x00, 0xF7, 0xF3, 0x89, 0x04, 0x25, 0x10, 0x01, 0x00, 0x00, 0x89, 0x14, 0x25, 0x20, 0x01, 0x00, 0x00 }
);            
            Emulator = new VM(ins);
            _refresh();
        }

        private void Step_Click(object sender, EventArgs e)
        {
            Emulator.Step();
            Refresh();
        }
        private void _refresh(object sender=null, EventArgs e=null)
        {
            List<Dictionary<string, ulong>> _regs = Emulator.GetRegisters();
            if (IsDec.Checked)
            {
                specialregs.DataSource = _regs[0].Select(x => x.Key + ": " + x.Value.ToString()).ToList();
                generalregs.DataSource = _regs[1].Select(x => x.Key + ": " + x.Value.ToString()).ToList();
            }
            else
            {
                specialregs.DataSource = _regs[0].Select(x => x.Key + ": " + x.Value.ToString("X")).ToList();
                generalregs.DataSource = _regs[1].Select(x => x.Key + ": " + x.Value.ToString("X")).ToList();
            }
            SortedDictionary<ulong, byte> _memory = new SortedDictionary<ulong, byte>(Emulator.GetMemory());
            /*if (bytestrings[iIndex] != separator)
                {
                    if (Convert.ToUInt64(bytestrings[iIndex].Substring(2, 8), 16) != addr.Key - 20)
                    {
                        if(Convert.ToUInt64(bytestrings[iIndex-1].Substring(2, 8), 16) != addr.Key - 20)
                        {
                            bytestrings.Add(separator);
                        }                 
                    }
                    else if (bytestrings[iIndex].Length != 15 + 20 * 3 || bytestrings[iIndex] != separator)
                    {
                        bytestrings[iIndex] += addr.Value.ToString("X").PadLeft(2, '0');
                    }
                    
                }*/
            string separator = new string(' ', 40) + '~';
            List<string> bytestrings = new List<string>();
            bytestrings.Add("0x" + _memory.First().Key.ToString("X").PadLeft(8, '0') + " |   ");

            int iIndex = 0;
            ulong lLast = 0;
            int bytesmissing = 0;
            foreach (var addr in _memory)
            {
                if(lLast != addr.Key - 1)
                {
                    bytestrings.Add(new string(' ', 20) + $"[{(addr.Key - lLast).ToString("X")}]");
                }


                
                if (!_memory.ContainsKey(addr.Key -1) || bytestrings[iIndex].Length == 111) //15 + (32 * 3)
                {
                    if(bytestrings[iIndex].Length != 111)
                    {
                        bytesmissing = (111 - bytestrings[iIndex].Length) / 3;
                        bytestrings[iIndex] += string.Join("", Enumerable.Repeat("00 ", bytesmissing));
                        
                    }
                    bytestrings.Add("0x" + addr.Key.ToString("X").PadLeft(8, '0') + " |   ");
                    
                }
                lLast = addr.Key - lLast + (ulong)bytesmissing;
                iIndex = bytestrings.Count() - 1;
                bytestrings[iIndex] += _memory[addr.Key].ToString("X").PadLeft(2, '0') + " ";
                
            }
            bytestrings.RemoveAt(0);

            memviewer.DataSource = bytestrings;
            
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Emulator.Run();
        }

        private void SetMemviewPos(object sender, EventArgs e)
        {

        }
    }
}
