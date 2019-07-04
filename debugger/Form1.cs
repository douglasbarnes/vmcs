using System;
using System.Collections.Generic;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            var ins = new MemorySpace(new byte[]
{ 0xB8, 0x0A, 0x00, 0x00, 0x00, 0x50, 0x66, 0x50, 0xB8, 0x32, 0x00, 0x00, 0x00, 0x83, 0xC0, 0x0A, 0xBB, 0x14, 0x00, 0x00, 0x00, 0x01, 0xD8, 0x66, 0x58, 0x8F, 0x04, 0x25, 0x10, 0x00, 0x00, 0x00 }
);            
            VM Emulator = new VM();
            Emulator.Run(ins);
            
        }




    }
}
