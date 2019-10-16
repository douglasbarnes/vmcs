// ThemedToolStripMenuHeader creates distinction between a Header and an Item. In native forms, there is none. The primary purpose is 
// to apply different paint methods to both.
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
            // Currently disabled, but in previous versions it was kind of annoying to have items close when clicked on, because
            // the layout was a little different. This is no longer necessary but it could be seen as annoying when clicking to focus on the
            // testcase search(you only need to hover over). Or maybe if you are adding to the forms, you may want to reuse this.
            // Setting cancel to true causes the DropdownClosing event to not be invoked, therefore nothing else will know it happened.
            //  if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            //  {
            //      e.Cancel = true;
            //  }
        }

    }
}
