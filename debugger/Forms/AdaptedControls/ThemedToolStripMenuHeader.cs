using System.Windows.Forms;
namespace debugger.Forms
{
    public class ThemedToolStripMenuHeader : CustomToolStripMenuItem
    {
        public ThemedToolStripMenuHeader() : base(Layer.Background, Emphasis.Medium)
        {
            DropDown.Closing += OnDropdownClosing;
            AutoSize = true;

        }
        private void OnDropdownClosing(object s, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        }

    }
}
