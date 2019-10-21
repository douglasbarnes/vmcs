// MemoryListView provides an easy way to present memory to the user. 
// Given a MemorySpace in LoadMemory(), it will handle the formatting. The caller must still handle where the listview
// is to be drawn; just like a normal list view. Behaviour when messing with inherited methods externally is undefined.
// It is not designed to be tampered with from its instance, everything works accordingly without intervention. I encourage
// anyone working with this class to make modifications directly to the class rather than mess with it outside as a common
// convention with any windows form. It makes for much cleaner code and it is very nice to be able to have methods in a form
// that you polish, then seal away for eternity.
using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using debugger.Emulator;
using static debugger.Forms.FormSettings;
namespace debugger.Forms
{
    public class MemoryListView : CustomListView
    {
        private readonly Size ForcedSize;
        public MemoryListView(Size size) : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
        {
            // Let forms draw the background
            BackColor = LayerBrush.Color;

            // All these give this class more control over the drawing of the form.
            AutoSize = false;
            View = View.Details;
            HideSelection = false;
            OwnerDraw = true;        
            BorderStyle = BorderStyle.None;
            HoverSelection = false;            
           
            // The column text will be drawn manually, but the draw event will never be called if there is no column.
            Columns.Add(new ColumnHeader(""));

            // ForcedSize is a required workaround for this control. For what ever reason, native classes pull rank over any size
            // set here. I believe this is some kind of autosizing mechanism for the scroll bar that cannot be overriden. The workaround
            // I came up with is to change $Size to $ForcedSize before drawing in OnDrawColumnHeader(), as this is the first drawing method 
            //to be called before drawing items etc. http://prntscr.com/oxdg0s
            ForcedSize = size; 

            Size = size;

            // Fill the listview with a single column(this most likely causes the above).
            Columns[0].Width = size.Width;
        }
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            // Draw only text; no other weird windows forms stuff like borders around items
            Util.Drawing.DrawFormattedText(e.Item.Text, e.Graphics, e.Bounds, Emphasis.Medium);
        }
        private const string ColumnHeader = "0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F";
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            // See constructor
            if(Size != ForcedSize || Columns[0].Width != ForcedSize.Width)
            {
                Size = ForcedSize;
                Columns[0].Width = ForcedSize.Width;
            } 

            Rectangle Bounds = e.Bounds;

            // Add a small offset(-3) such that the characters are drawn in the middle of a column
            // Without this line, it would look like this,
            // 0  1  2  3  4  5
            // AA AA AA AA AA AA
            // Its hard to depict with text, but imagine the header characters(0,1,2) being in line with the center of A|A where the pipe is.
            // The large offset is added to put the characters above where the memory will go, not the addresses.
            Bounds.X +=  ((int)BaseUI.BaseFont.SizeInPoints * 15) - 3;

            // Minor demphasis on the header to keep the memory more distinct.
            Util.Drawing.DrawFormattedText(ColumnHeader, e.Graphics, Bounds);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            // Also necessary in addition to OnDrawColumnHeader(). Without this, it will be too small when the set of items is empty.
            // See constructor.
            if(Size != ForcedSize)
            {
                Size = ForcedSize;
            }
            base.OnSizeChanged(e);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            // Avoid any rectangle area selection boxes and other weird forms things like that.
            //  base.OnMouseClick(e);
        }

        public void LoadMemory(MemorySpace inputMemory)
        {
            // Create a new list to hold the items before applying them. This is generally a good idea when working with forms,
            // work on/parse the data into a buffer before changing the real items collection. Yes you technically get o(2n)
            // but there is significantly less processing complexity when adding to the collection, especially with drawing paused.
            List<string> ItemsBuffer = new List<string>();

            // This will be used to avoid displaying repeating data
            ulong LastAddress = 0;
            
            // Iterate through every address range in the memory.
            for (int table_index = 0; table_index < inputMemory.AddressTable.Count; table_index++)
            {
                AddressRange CurrentRange = inputMemory.AddressTable[table_index];

                // Essentially round down to the nearest 0x10. This makes the memory viewer 999999x better, as it allows,
                // - To not have every single address on your screen. They are added as the program uses them. An unused address
                //   will never be shown.
                // - To have the offsets in the header be coherent with the address. Previously the address would just be the first
                //   address seen. (Note that this is before ranges were implemented, but the example stands) For example, if a range
                //   started at 0x123, that exact address would be shown in the memory viewer. This made the offsets really confusing as
                //   you would have to add up in your head for each address. Now it will be rounded down to the nearest 0x10 so if you look
                //   at the 0xE column, that would be 0x12E, not 0x123 + 0xE.
                // Here is the old version, in low resolution http://prntscr.com/plgd1m
                // Here is the current, http://prntscr.com/plgycu
                ulong CurrentAddress = CurrentRange.Start & 0xFFFFFFFFFFFFFFF0;

                // Naturally, $LastAddress does not matter if this is the first address. This is safer than something like assigning $LastAddress 
                // to an arbitary value in terms of simplicity and future developments. Or, if the same address would have already been displayed
                // by another range. This idea is explained a little more shorly, but say two addresses were to be shown, 0x101 and 0x104. When
                // 0x101 was added, as said earlier, it was rounded down to the multiple of 0x10 beneath it, 0x100. After 0x100 are 0xF more adjacent
                // addresses(excluding 0x100) that are shown on the end, not only because it fills the line but it it can come in handy, especially when
                // you consider that a new line would be added for the new address, and so would not only have repeated data but the viewer would become "jagged".
                // In otherwords, do not show the same address line twice.
                if(table_index == 0 || CurrentAddress > LastAddress)
                {
                    // If the address has gone out of the range. It's conventionally fine go a little bit out of range(no more than x % 10) 
                    // because it would look strange having incomplete rows, but an advantage stated earlier was that the minimum amount of data 
                    // necessary would be shown to the user.
                    while (CurrentAddress < CurrentRange.End)
                    {
                        // Create a new string with the current address + some formatting. 
                        // - "0x" is greatly de-emphasised.
                        // - Trailing zeros are de-emphasised.
                        // - Address is as normal.
                        string CurrentLine = $"%0x%${CurrentAddress.ToString("X").PadLeft(16, '0')}$";

                        // Note that because of the "$" at the end of the previous line, there will always be a non-zero, and
                        // therefore a " inserted, so even if the markdown block has no length(the address was 0x00...00) there will
                        // still be " to close. A £ is inserted to parse all the following bytes of memory as medium emphasis.
                        CurrentLine = Util.Drawing.InsertAtNonZero(CurrentLine, "\"", 5) + "\"£";

                        // Add the next 0x10 addresses to the string, including the byte at the address.
                        for (int i = 0; i < 0x10; i++)
                        {
                            // Convert.ToString() will not complete bytes and display in the shortest form possible, e.g instead of
                            // 0F it will return just F. This is not really ideal, its generally much more conventional to always see
                            // a whole byte.
                            CurrentLine += $" {Convert.ToString(inputMemory[CurrentAddress], 16).ToUpper().PadRight(2, '0')}";

                            // Increment the current address as well as $i, as $i will only be used to iterate 0x10 times but CurrentAddress
                            // will be used in other scopes too.
                            CurrentAddress++;
                        }

                        // Add the completed line to the buffer.
                        ItemsBuffer.Add(CurrentLine);
                    }
                }

                // Store the current address for reasons stated ealier. As 0x10 was added, it will already be the next multiple of ten. This
                // would be the case when an address range that would not have had overlap with the range directly above it if it was not for
                // the extra bytes added on to the end to complete the row.
                LastAddress = CurrentAddress;
            }
            Invoke(new Action(() => 
            {
                // Here I am against using BeginUpdate() and EndUpdate() methods. I'm not certain of how they work exactly, but 
                // they seem to worsen any flickering because the text initially displays as white for whatever reason.

                // The idea here it a countermeasure for flickering. Flickering looks awful on forms and even worse with the current
                // dark theme, so a reasonable amount of effort has been put into reducing it where possible. At the time of writing
                // it is negligible throughout the program. It seems to considerably worsen when adding items, e.g if there was a loop
                // that first cleared all items, then added the new ones, it looks horrible. This method demonstrates how this can be
                // minimised. The number of items in the internal items collection always has to be the same as the number of items
                // you want to display. This is non-negotiable. So, where ever possible, existing items are used rather than replacing
                // them with new ones. Only when necessary are new items added. In addition, unused items are removed afterwards. This
                // is mildly unecessary, however prevents the user from being able to scroll down into oblivion once a new program is loaded.
                int i = 0;

                for (; i < ItemsBuffer.Count; i++)
                {              
                    // If the item is about to go out of range, new items need to be added on top.
                    if(Items.Count == i)
                    {
                        Items.Add(ItemsBuffer[i]);
                    }

                    // This can theoretically get quite performance heavy, especially on unchanged strings as the whole string has to be compared.
                    // When the text of an item is changed, it is redrawn. You remove this and you get a considerable amount of flickering.
                    // This has been tested on a so called "toaster" machine, and it was no  real problem. Its no argument, but there are other parts
                    // of the program that are more optimisation-prone than this. With a small address space you most likely wont notice a thing. 
                    else if (ItemsBuffer[i] != Items[i].Text)
                    {
                        Items[i].Text = ItemsBuffer[i];
                    }                    
                }

                // If there are any remaining items left in the forms item collection, remove them(for reasons explained earlier). If $itemsbuffer.Count
                // was greater than or equal to $Items.Count , this will be skipped over.
                for (; i < Items.Count; i++)
                {
                    Items.RemoveAt(i);
                }
            }));
        }

    }
}
