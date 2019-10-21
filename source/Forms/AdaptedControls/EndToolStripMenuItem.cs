// EndToolStripMenuItem is like any other menu header however will automatically pad itself to the end of the menu bar. This
// must be added to the menu bar list. It cannot be added as a range. This is because OnParentChanged needs to be called after
// all other menu items have been added. Using AddRange() can mess this up for an unknown reason, most likely because the
// other elements havent been added to Parent.Items before OnParentChanged is called.
using System.Windows.Forms;
namespace debugger.Forms
{
    public class EndToolStripMenuItem : ThemedToolStripMenuHeader
    {
        private void RefreshPosition()
        {
            // Calculate the sum of widths of all items on the menu bar.
            int WidthSum = 0;            
            for (int i = 0; i <= Parent.Items.Count-1; i++)
            {
                WidthSum += Parent.Items[i].Width;
            }

            // Use $WidthSum and padding to adjust the position to the end 
            Margin = new Padding(Parent.Width - WidthSum - 4, 0, Parent.Width - WidthSum, 0);
        }
        protected override void OnParentChanged(ToolStrip oldParent, ToolStrip newParent)
        {            
            base.OnParentChanged(oldParent, newParent);

            // Refresh position when added to a new menu. Strange behaviour when closing the application
            // without this check, a null reference exception will sometimes be thrown in RefreshPosition().            
            if (newParent != null)
            {
                RefreshPosition();
            }
            
        }

    }
}
