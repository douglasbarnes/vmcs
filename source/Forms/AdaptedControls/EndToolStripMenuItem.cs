using System.Windows.Forms;
namespace debugger.Forms
{
    public class EndToolStripMenuItem : ThemedToolStripMenuHeader
    {
        private void RefreshPosition()
        {
            int WidthSum = 0;
            for (int i = 0; i <= Parent.Items.IndexOf(this); i++)
            {
                WidthSum += Parent.Items[i].Width;
            }
            Margin = new Padding(Parent.Width - WidthSum - 4, 0, Parent.Width - WidthSum, 0);
        }
        protected override void OnParentChanged(ToolStrip oldParent, ToolStrip newParent)
        {
            base.OnParentChanged(oldParent, newParent);
            if (newParent != null)
            {
                RefreshPosition();
            }
        }

    }
}
