// CustomListView applies the FormSettings convention to ListViews.
using System.Windows.Forms;
namespace debugger.Forms
{
    public abstract class CustomListView : ListView, IMyCustomControl
    {
        public Layer DrawingLayer { get; set; }
        public Emphasis TextEmphasis { get; set; }
        public Emphasis HeaderEmphasis;
        public CustomListView(Layer drawingLayer, Emphasis textEmphasis, Emphasis headerEmphasis) : base()
        {
            // See CustomPanel
            DoubleBuffered = true;

            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
            HeaderEmphasis = headerEmphasis;
        }

        protected override void OnColumnWidthChanging(ColumnWidthChangingEventArgs e)
        {
            // Quick hack to disable the changing of column widths. This isn't wanted in this program, it
            // only invites problems. There is no way to prevent the cursor changing when hovering over
            // the column divider(which is the same colour as the background so not distinguishable) as it is done
            // at a native code level.
            // Simply not calling the below method will prevent the column resizing.
            //  base.OnColumnWidthChanging(e);
        }
    }
}
