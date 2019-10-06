using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using debugger.Hypervisor;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public class DisassemblyListView : CustomListView
    {
        public ListeningList<ulong> BreakpointSource = new ListeningList<ulong>();
        private readonly ListeningList<Disassembler.ParsedLine> ParsedLines;
        public DisassemblyListView(ListeningList<ulong> breakpointSource, ListeningList<Disassembler.ParsedLine> parsedLines, Size size) : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            BreakpointSource = breakpointSource;
            ParsedLines = parsedLines;
            OwnerDraw = true;
            BorderStyle = BorderStyle.None;
            FullRowSelect = true;
            HeaderStyle = ColumnHeaderStyle.None;
            MultiSelect = false;
            View = View.Details;
            Size = size;
            Columns.Add(new ColumnHeader() { Width = this.Width - 4 });// width of DissassemblyPadding, allows the border to not get cropped off by the listview
            SendToBack();
            //SelectedIndexChanged += (sender, args) =>
            //{
            //    if (SelectedItems.Count > 0)
            //    {
            //        ulong Address = IndexToAddr[SelectedItems[0].Index];
            //        if (BreakpointSource.Contains(Address))
            //        {
            //            BreakpointSource.Remove(Address);
            //        }
            //        else
            //        {
            //            BreakpointSource.Add(Address);
            //        }
            //        SelectedItems.Clear();
            //    }
            //};
            ParsedLines.OnSet += FormatAndApply;
            ParsedLines.OnAdd += FormatAndApply;
            ParsedLines.OnRemove += (item, index ) => { Items[index].Remove(); };
            ParsedLines.OnClear += () => Items.Clear();
            BreakpointSource.OnAdd += (item, index) => Refresh();
            BreakpointSource.OnRemove += (item, index) => Refresh();
        }
        private void FormatAndApply(Disassembler.ParsedLine line, int index)
        {            
            string FormattedNumber = Core.FormatNumber(line.Address, FormatType.Hex);

            // Only dim trailing zeros if its not a breakpoint, make it purple otherwise.
            if ((line.Info | Emulator.AddressInfo.BREAKPOINT) == line.Info)
            {
                FormattedNumber = $"^0x{FormattedNumber}^";
            }
            else
            {
                
                string[] CutAddress = Core.SeparateString(FormattedNumber.Substring(2, FormattedNumber.Length - 2), "0", stopAtFirstDifferent: true);
                if (CutAddress[1].Length > 0) // if there are trailing 0's, make them darker
                {
                    CutAddress[1] = $"%{CutAddress[1]}%";
                }
                FormattedNumber = $"%0x%{CutAddress[1]}{CutAddress[0].Trim()}";
            }            
            
            if((line.Info | Emulator.AddressInfo.RIP) == line.Info)
            {
                FormattedNumber = FormattedNumber.Remove(23, 4).Insert(23, "←RIP");
            }

            if (Items.Count - 1 < index)
            {
                Items.Add(new ListViewItem($"{FormattedNumber}                  {line.DisassembledLine}"));
            }
            else
            {
                Items[index].Text = $"{FormattedNumber}                  {line.DisassembledLine}";
            }
            
        }
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            e.Graphics.FillRectangle(LayerBrush, e.Bounds);
            Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);
            Drawing.DrawFormattedText(e.Item.Text, e.Graphics, HeightCenteredBounds);
        }
    }
}
