using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using debugger.Hypervisor;
using debugger.Util;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public class DisassemblyListView : CustomListView
    {
        public BindingList<ulong> BreakpointSource = new BindingList<ulong>();

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
            e.Graphics.FillRectangle(LayerBrush, e.Bounds);
            Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);
            if (!IndexToAddr.ContainsKey(e.ItemIndex))
            {
                return;
            }
            if (BreakpointSource.Contains(IndexToAddr[e.ItemIndex]))
            {
                Drawing.DrawFormattedText(Drawing.CleanString(new string(IndexToLine[e.ItemIndex])).Insert(18, "^").Insert(0, "^"), e.Graphics, HeightCenteredBounds);
            }
            else
            {
                Drawing.DrawFormattedText(new string(IndexToLine[e.ItemIndex]), e.Graphics, HeightCenteredBounds);
            }
            
        }
        public void SetRIP(ulong insPtr)
        {
            try
            {
                if (!AddrToLine.ContainsKey(insPtr))
                {
                    insPtr -= 1;
                }
                for (int i = 0; i < 4; i++)
                {
                    IndexToLine[RIPIndex][23 + i] = ' ';
                    AddrToLine[insPtr][23 + i] = "←RIP"[i];
                }
                RIPIndex = AddrToIndex[insPtr];
            }
            catch
            {
                Logging.Logger.Log(Logging.LogCode.DISASSEMBLY_RIPNOTFOUND, insPtr.ToString("X"));
            }
        }
        private readonly Dictionary<ulong, char[]> AddrToLine = new Dictionary<ulong, char[]>();
        private readonly Dictionary<int, char[]> IndexToLine = new Dictionary<int, char[]>();
        private readonly Dictionary<ulong, int> AddrToIndex = new Dictionary<ulong, int>();
        private readonly Dictionary<int, ulong> IndexToAddr = new Dictionary<int, ulong>();
        private int RIPIndex;
        public void RemoveAll()
        {
            AddrToLine.Clear();
            IndexToAddr.Clear();
            IndexToLine.Clear();
            AddrToIndex.Clear();
        }
        public void AddParsed(Dictionary<ulong, Disassembler.DisassembledItem> ParsedLines)
        {
            AddrToLine.Clear();
            int Index = 0;
            foreach (var Line in ParsedLines)
            {
                
                string FormattedNumber = Core.FormatNumber(Line.Key, FormatType.Hex);
                string[] CutAddress = Core.SeparateString(FormattedNumber.Substring(2,FormattedNumber.Length-2), "0", stopAtFirstDifferent: true);
                char[] Reference;
                
                if (CutAddress[1].Length > 0) // if there are trailing 0's, make them darker
                {
                    CutAddress[1] = $"%{CutAddress[1]}%";
                }
                Reference = $"%0x%{CutAddress[1]}{CutAddress[0].Trim()}                  {Line.Value.DisassembledLine}".ToCharArray();       
                AddrToLine.Add(Line.Key, Reference);
                IndexToLine.Add(Index, Reference);
                AddrToIndex.Add(Line.Key, Index);
                IndexToAddr.Add(Index, Line.Key);
                if ((Line.Value.AddressInfo | Disassembler.DisassembledItem.AddressState.RIP) == Line.Value.AddressInfo)
                {
                    RIPIndex = Index;
                    SetRIP(Line.Key);
                }
                Index++;
            }
            Invoke(new Action(() =>
            {
                BeginUpdate();
                Items.Clear();
                foreach (var Line in IndexToLine)
                {
                    Items.Add(new string(Line.Value));
                }
                EnsureVisible(RIPIndex);               
                EndUpdate();
                Update();
            }));
        }
    }
}
