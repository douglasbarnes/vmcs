// DisassemblyListView provides a great interface for displaying disassembly. It is quite dependent on the output of the current Disassembler, however should
// work fine with new modules so long as a few concepts are followed,
// ListeningDict<Emulator.AddressRange, ListeningList<ParsedLine>> is used as the output, and the disassembler instance is constantly used for as long as
// the list view is. I.e, make a new list view for every disassembler instance. This shouldn't be a problem if done properly. Hypervisors are not designed
// to be constantly added and removed to the control unit, it is best to have one, then use that constantly. Another thing that is super important is that 
// the ListeningDict in the disassembler is never ever reinstanced. Always use IDictionary.Clear() when emptying not new ListeningDict...(). This is because
// the reference is absolutely taken advantage of to its fullest extent. As I say time and time again, references in this program are either your best friend
// or your worst enemy. Using the listeningdict once allows the listview to listen for changes to the dictionary. This means that single line rendering can
// be implemented and still be super effective(see MemoryListView for a less effective single line rendering implementation). As the dictionary could be huge
// it is absolutely essential that the whole dictionary(of parsed lines) does not have to be reparsed/rerendered(the ParsedLine struct isn't as parsed as
// this class needs it to be, there is still formatting to be done that the disassembler class doesn't know about). Another advantage is that it is super
// easy for the two to communicate. If a breakpoint is set, this is reflected here by changing the colour of that line to purple. This is made possible by
// the concept I previously explained. Any other method of doing this turns into a hacky and inefficient mess. If you want to see that, look at commits from
// before October 9th 2019. So long as these concepts are followed and understood, this class is simple to work with and understand. The dependency between
// this class and Disassembler is probably the greatest seen throughout the program, but as will be demonstrated it definitely carries its own weight.
using debugger.Util;
using System.Drawing;
using System.Windows.Forms;
using static debugger.Hypervisor.Disassembler;
namespace debugger.Forms
{
    public class DisassemblyListView : CustomListView
    {
        // The super important dictionary mentioned in the summary.
        private readonly ListeningDict<Emulator.AddressRange, ListeningList<ParsedLine>> ParsedLines;

        // Delegate and event for when an address is clicked. In the current MainForm, this sets a breakpoint.
        public delegate void OnAddressClickedDelegate(ulong address);
        public event OnAddressClickedDelegate OnAddressClicked = (a) => { };
        public DisassemblyListView(ListeningDict<Emulator.AddressRange, ListeningList<ParsedLine>> parsedLines, Size size)
            : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            SelectedIndexChanged += (s, a) =>
            {
                // This event will be raised twice, once when it should be called, and the second when the .Clear() is called below.
                // I would strongly recommend using OnAddressClicked rather than SelectedIndexChanged because this method makes sure that
                // there is never more than one index selected, or another mysterious act from windows forms.
                if (SelectedItems.Count > 0)
                {
                    // The numeric address the line represents is stored in SubItems[1] and never drawn. This saves having to do extra parsing
                    // trying to select the hexadecimal from the beginning of the line. A little extra memory used but its worthwhile from a 
                    // maintenance and robustness perspective.
                    OnAddressClicked.Invoke(ulong.Parse(SelectedItems[0].SubItems[1].Text));
                }
                SelectedItems.Clear();
            };

            // Set the reference to the provided.
            ParsedLines = parsedLines;

            // More control over how the control is drawn.
            OwnerDraw = true;
            BorderStyle = BorderStyle.None;
            FullRowSelect = true;
            HeaderStyle = ColumnHeaderStyle.None;
            View = View.Details;

            // This is a kind of best-effort thing. Sometimes it will allow multiple selection regardless. All is out of my control, but OnAddressClicked can be
            // used to work around it.
            MultiSelect = false;

            Size = size;

            // Setting the column size to the width of the control and shortening it slightly prevents the border from being cropped. 
            // It shouldn't regardless but windows forms likes to do its own thing.
            Columns.Add(new ColumnHeader() { Width = Width - 4 });

            // Keep the display up to date with changes to the parsed lines.
            ParsedLines.OnAdd += (range, lines) => AddNewLines(lines);
            ParsedLines.OnClear += () => Items.Clear();
        }
        private void AddNewLines(ListeningList<ParsedLine> lines)
        {
            // Format each struct in the input and set its index to where it is in the items of this control. This makes
            // life easier later.
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = new ParsedLine(lines[i]) { Index = Items.Count };
                FormatAndApply(lines[i]);
            }

            // Update the lines automatically later.
            lines.OnSet += (line, index) => FormatAndApply(line);
            lines.OnAdd += (line, index) => FormatAndApply(line);
        }
        private void FormatAndApply(ParsedLine line)
        {
            // Parse the address as hex.
            string FormattedAddress = line.Address.ToString("X").PadLeft(16, '0');

            // Make breakpoint lines purple using markdown. No other formatting is applied to these.
            if ((line.Info | Emulator.AddressInfo.BREAKPOINT) == line.Info)
            {
                FormattedAddress = $"^0x{FormattedAddress}^";
            }
            else
            {
                // Use Drawing.InsertAt..() to remove emphasis from insignificant zeroes. This is done by starting
                // with a low emphasis, "$" then inserting the normal emphasis, "£" at the significant digits.
                FormattedAddress = $"%0x%${Drawing.InsertAtNonZero(FormattedAddress, "£")}\"";
            }

            // Add some spacing between the address and disassembled line.
            FormattedAddress = $"{FormattedAddress}                  {line.DisassembledLine}";

            // Replace some of that spacing if the line is the next to be executed.
            if ((line.Info | Emulator.AddressInfo.RIP) == line.Info)
            {
                FormattedAddress = FormattedAddress.Remove(23, 4).Insert(23, "←RIP");
            }

            // Very similar idea to as in MemoryListView. Look there for more explanation of this.
            if (Items.Count - 1 < line.Index)
            {
                Items.Add(new ListViewItem(new string[] { FormattedAddress, line.Address.ToString() }));
            }
            else
            {
                Items[line.Index].Text = FormattedAddress;

                // Add the address as a sub item that the user will never see, but will be useful in the OnAddressClicked event.
                Items[line.Index].SubItems[1].Text = line.Address.ToString();
            }
        }
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            // Resize the bounds a little to put the disassembly in the middle of the item bounds not at the top. Purely aesthetic.
            Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);

            Drawing.DrawFormattedText(e.Item.Text, e.Graphics, HeightCenteredBounds);
        }
    }
}
