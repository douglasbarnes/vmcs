using System.Windows.Forms;
using System.Drawing;
using static debugger.Forms.FormSettings;
using System;

namespace debugger.Forms
{
    public class MemoryListView : CustomListView
    {
        private readonly Size ForcedSize;
        public MemoryListView(Size size) : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            BackColor = LayerBrush.Color;
            AutoSize = false;
            View = View.Details;
            HideSelection = false;
            OwnerDraw = true;        
            BorderStyle = BorderStyle.None;
            Columns.Add(new ColumnHeader(""));
            ForcedSize = size; // http://prntscr.com/oxdg0s
            Size = size;
            Columns[0].Width = size.Width;
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            string[] itemstring = new string[e.Item.SubItems.Count + 1];
            itemstring[0] = e.Item.Text;
            for (int i = 0; i < e.Item.SubItems.Count; i++)
            {
                itemstring[i] = e.Item.SubItems[i].Text;
            }
            e.Graphics.DrawString(" " + string.Join(" ", itemstring), BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], e.Bounds);
        }

        private readonly char[] ColumnChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            if(Size != ForcedSize || Columns[0].Width != ForcedSize.Width)
            {
                Size = ForcedSize;
                Columns[0].Width = ForcedSize.Width;
            } 
            Rectangle Bounds = e.Bounds;
            e.Graphics.FillRectangle(LayerBrush, Bounds);
            Bounds.X += (int)BaseUI.BaseFont.SizeInPoints / 2;
            e.Graphics.DrawString("                    " + string.Join("  ", ColumnChars), BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
            Bounds.X -= (int)BaseUI.BaseFont.SizeInPoints / 2;
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            if(Size != ForcedSize)
            {
                Size = ForcedSize;
            }
            base.OnSizeChanged(e);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            //base.OnMouseClick(e);
        }
    }
}
