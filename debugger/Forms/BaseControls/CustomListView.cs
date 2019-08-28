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
            DoubleBuffered = true;
            DrawingLayer = drawingLayer;
            TextEmphasis = textEmphasis;
            HeaderEmphasis = headerEmphasis;
            ColumnWidthChanging += (sender, args) => { args.NewWidth = Columns[args.ColumnIndex].Width; args.Cancel = true; };
        }
    }
}
