using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using debugger.Hypervisor;
using static debugger.Forms.FormSettings;
using debugger.Util;

namespace debugger.Forms
{
    public class DisassemblyListView : CustomListView
    {
        public BindingList<ulong> BreakpointSource = new BindingList<ulong>();
        private SolidBrush[] ItemColours = new SolidBrush[]
        {
                LayerBrush,
                BreakpointBrush
        };
        public DisassemblyListView(Size size) : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            OwnerDraw = true;
            BorderStyle = BorderStyle.None;
            FullRowSelect = true;
            HeaderStyle = ColumnHeaderStyle.None;
            MultiSelect = false;
            View = View.Details;
            Size = size;
            //HideSelection = false;
            Columns.Add(new ColumnHeader() { Width = this.Width - 4 });// width of DissassemblyPadding, allows the border to not get cropped off by the listview
            SendToBack();
            SelectedIndexChanged += (sender, args) =>
            {
                if (SelectedItems.Count > 0)
                {
                    ulong Address = IndexToAddr[SelectedItems[0].Index];
                    if (BreakpointSource.Contains(Address))
                    {
                        BreakpointSource.Remove(Address);
                    }
                    else
                    {
                        BreakpointSource.Add(Address);
                    }
                    SelectedItems.Clear();
                }
            };
            BreakpointSource.ListChanged += (sender, args) => Refresh();
        }
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            ulong CurrentAddr = IndexToAddr[e.ItemIndex];
            e.Graphics.FillRectangle(ItemColours[(BreakpointSource.Contains(CurrentAddr) ? 1 : 0)], e.Bounds);
            Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);
            Drawing.DrawFormattedText(IndexToLine[e.ItemIndex], e.Graphics, HeightCenteredBounds);
        }
        public void SetRIP(ulong insPtr)
        {
            try
            {   
                AddrToLine[insPtr] = AddrToLine[insPtr].Remove(21, 4).Insert(19, "←RIP");
                IndexToLine[RIPIndex] = IndexToLine[RIPIndex].Remove(21, 4).Insert(19, "    ");
                RIPIndex = AddrToIndex[insPtr];
            }
            finally
            {
                Logging.Logger.Log(Logging.LogCode.DISASSEMBLY_RIPNOTFOUND, insPtr.ToString("X"));
            }
        }
        private readonly Dictionary<ulong, string> AddrToLine = new Dictionary<ulong, string>();
        private readonly Dictionary<int, string> IndexToLine = new Dictionary<int, string>();
        private readonly Dictionary<ulong, int> AddrToIndex = new Dictionary<ulong, int>();
        private readonly Dictionary<int, ulong> IndexToAddr = new Dictionary<int, ulong>();
        private int RIPIndex;
        public void AddParsed(Dictionary<ulong, Disassembler.DisassembledItem> ParsedLines)
        {
            AddrToLine.Clear();
            int Index = 0;
            foreach (var Line in ParsedLines)
            {
                if((Line.Value.AddressInfo | Disassembler.DisassembledItem.AddressState.RIP) == Line.Value.AddressInfo)
                {
                    RIPIndex = Index;
                }
                string FormattedLine = "";
                string[] CutAddress = Core.SeparateString(Core.FormatNumber(Line.Key, FormatType.Hex), "0", stopAtFirstDifferent: true);
                if(Line.Value.AddressInfo | Disassembler.DisassembledItem.AddressState.Break)
                if (CutAddress[1].Length > 0) // if there are trailing 0's, make them darker
                {
                    FormattedLine += $"0x\"{CutAddress[1]}\"";
                }
                FormattedLine += $"{CutAddress[0].Trim()}                  {Line.Value.DisassembledLine}";
                AddrToLine.Add(Line.Key, FormattedLine);
                IndexToLine.Add(Index, FormattedLine);
                AddrToIndex.Add(Line.Key, Index);
                IndexToAddr.Add(Index, Line.Key);
                Index++;
            }
            Invoke(new Action(() =>
            {
                BeginUpdate();
                Items.Clear();
                foreach (var Line in IndexToLine)
                {
                    Items.Add(Line.Value);
                }
                EnsureVisible(RIPIndex);               
                EndUpdate();
                Update();
            }));
        }
    }
}
