using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace debugger
{
    static class SelfDebug
    {

        public static void ExecuteTestcase(string name)
        {

        }
        public static void TestcaseSelected(object sender, EventArgs e)
        {
            //sender.tostring= text
            MessageBox.Show(sender.ToString());
        }       
    }

    
}
