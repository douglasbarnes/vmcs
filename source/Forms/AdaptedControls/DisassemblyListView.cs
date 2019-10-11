using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using debugger.Hypervisor;
using debugger.Util;
using static debugger.Forms.FormSettings;
using static debugger.Hypervisor.Disassembler;
namespace debugger.Forms
{
    public class DisassemblyListView : CustomListView
    {
        private readonly ListeningDict<Emulator.AddressRange, ListeningList<ParsedLine>> ParsedLines;

        public delegate void OnAddressClickedDelegate(ulong address);
        public event OnAddressClickedDelegate OnAddressClicked = (a) => { };
        public DisassemblyListView(ListeningDict<Emulator.AddressRange, ListeningList<ParsedLine>> parsedLines, Size size) 
            : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            SelectedIndexChanged += (_, args) =>
            {
                if (SelectedItems.Count > 0)
                {
                    OnAddressClicked.Invoke(ulong.Parse(SelectedItems[0].SubItems[1].Text));
                }
                SelectedItems.Clear();
            };
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
            ParsedLines.OnAdd += AddNewRange;
            ParsedLines.OnClear += () => Items.Clear();
        }
        private void AddNewRange(Emulator.AddressRange range, ListeningList<ParsedLine> lines)
        {            
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = new ParsedLine(lines[i]) { Index = Items.Count };                
                FormatAndApply(lines[i]);
            }
            lines.OnSet += (line, _) => FormatAndApply(line);
            lines.OnAdd += (line, _) => FormatAndApply(line);
        }
        private void FormatAndApply(ParsedLine line)
        {            
            string FormattedNumber = Core.FormatNumber(line.Address, FormatType.Hex);

            // Only dim trailing zeros if its not a breakpoint, make it purple otherwise.
            if ((line.Info | Emulator.AddressInfo.BREAKPOINT) == line.Info)
            {
                FormattedNumber = $"^{FormattedNumber}^";
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
            FormattedNumber = $"{FormattedNumber}                  {line.DisassembledLine}";
            if ((line.Info | Emulator.AddressInfo.RIP) == line.Info)
            {
                FormattedNumber = FormattedNumber.Remove(23, 4).Insert(23, "←RIP");
            }

            if (Items.Count - 1 < line.Index)
            {
                Items.Add(new ListViewItem(new string[] { FormattedNumber, line.Address.ToString() }));
            }
            else
            {
                Items[line.Index].Text = FormattedNumber;
                Items[line.Index].SubItems[1].Text = line.Address.ToString();
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
