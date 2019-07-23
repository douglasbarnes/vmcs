using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using static debugger.FormSettings;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace debugger
{
    class CustomControls
    {
        public interface IMyCustomControl
        {
            Layer DrawingLayer { get; set; }
            Emphasis TextEmphasis { get; set; }
        }
        public abstract class CustomPanel : Panel, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomPanel(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                //TAG = TEXT TO WRITE
                Graphics GraphicsHandler = e.Graphics;
                Rectangle Bounds = e.ClipRectangle;
                //prevent line being cut
                Bounds.Y += 10; //push downwards so we can have the text in line, we cannot draw outside the bounds of the control
                Bounds.Width -= 3;
                Bounds.Height -= 11;
                GraphicsHandler.DrawRectangle(new Pen(LayerBrush), Bounds);
                GraphicsHandler.DrawRectangle(new Pen(ElevatedTransparentOverlays[(int)DrawingLayer]), Bounds);
                //Prevent strikethough of text, constants here are exact, they make it look nice
                int Offset = TextRenderer.MeasureText((string)Tag, BaseUI.BaseFont).Width;
                GraphicsHandler.DrawLine(new Pen(BaseUI.BackgroundColour), new Point(Bounds.Location.X + 11, Bounds.Location.Y), new Point(Bounds.Location.X + Offset + 14, Bounds.Location.Y));
                Bounds.X += 15;
                Bounds.Y -= 7;
                Bounds.Width += 15;
                Bounds.Height += 15;
                //GraphicsHandler.DrawString((string)Tag, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);          
                GraphicsHandler.DrawString((string)Tag, BaseUI.BaseFont, LayerBrush, Bounds);
                GraphicsHandler.DrawString((string)Tag, BaseUI.BaseFont, ElevatedTransparentOverlays[(int)DrawingLayer+1], Bounds);
            }
        }
        public abstract class CustomLabel : Label, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public string DisabledSubstring = "";
            public bool DisableEntire = false;
            public CustomLabel(Emphasis textLayer) : base()
            {
                TextEmphasis = textLayer;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Rectangle Bounds = e.ClipRectangle;
                if (Text.Contains(DisabledSubstring) && DisabledSubstring != "") // assume it wont happen often, so do indexof in the case it does rather than testing for -1 every time
                {                 
                    
                    if(DisableEntire)
                    {
                        e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)Emphasis.Disabled], Bounds);
                    }
                    else
                    {
                        string[] SortedText = Util.Core.SeparateString(Text, DisabledSubstring);
                        e.Graphics.DrawString(SortedText[0], BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
                        e.Graphics.DrawString(SortedText[1], BaseUI.BaseFont, TextBrushes[(int)Emphasis.Disabled], Bounds);
                    }                    
                }
                else
                {
                    e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
                }
            }
        }
        public abstract class CustomListview : ListView, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public Emphasis HeaderEmphasis;
            public CustomListview(Layer drawingLayer, Emphasis textEmphasis, Emphasis headerEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
                HeaderEmphasis = headerEmphasis;
                ColumnWidthChanging += (sender, args) => { args.NewWidth = Columns[args.ColumnIndex].Width; args.Cancel = true; };
            }
        }
        public abstract class CustomButton : Button, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public bool CustomBorder = false;
            public CustomButton(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
            }
            protected override void OnPaint(PaintEventArgs e)
            {              
                Rectangle Bounds = e.ClipRectangle;
                e.Graphics.FillRectangle(LayerBrush, Bounds);
                e.Graphics.FillRectangle(ElevatedTransparentOverlays[(int)DrawingLayer], Bounds);
                Rectangle InnerBounds = new Rectangle(Bounds.X+1 , Bounds.Y+1 , Bounds.Width-3, Bounds.Height-3);
                e.Graphics.DrawRectangle(new Pen(ElevatedTransparentOverlays[(int)DrawingLayer]), InnerBounds);
                Size TextSize = TextRenderer.MeasureText(Text, BaseUI.BaseFont);
                Rectangle StringPosition = new Rectangle(new Point(Bounds.X + (Bounds.Width - TextSize.Width)/2, Bounds.Y + (Bounds.Height - TextSize.Height)/2), TextSize);
                e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], StringPosition);               
            }  
        }
        public class BorderedPanel : CustomPanel
        {
            public BorderedPanel() : base(Layer.Background, Emphasis.High)
            {
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                BringToFront();
                base.OnPaint(e);                             
            }
        }
        public class RegisterLabel : CustomLabel
        {          
            public RegisterLabel() : base(Emphasis.High) { DisabledSubstring = "0x0000000000000000"; DisableEntire = true; }

        }
        public class FlagLabel : CustomLabel
        {
            public FlagLabel() : base(Emphasis.High) { DisabledSubstring = "False"; }
        }
        public class StepButton : CustomButton
        {
            public StepButton() : base(Layer.Foreground, Emphasis.Medium) { FlatAppearance.BorderSize = 0; CustomBorder = true; }
        }
        public class DisassemblyListView : CustomListview
        {
            //https://docs.microsoft.com/en-gb/windows/win32/controls/cookbook-overview
            [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(IntPtr LVHandle, string keepEmpty1, string keepEmpty2);
            public List<Disassembler.DisassembledItem> AddressedItems = new List<Disassembler.DisassembledItem>();
            public BindingList<ulong> BreakpointsSource = new BindingList<ulong>();
            private SolidBrush[] ItemColours = new SolidBrush[]
            {
                LayerBrush,
                PrimaryBrush
            };
            public DisassemblyListView() : base(Layer.Background, Emphasis.High, Emphasis.Medium)
            {
                DoubleBuffered = true;
                SendToBack();
                Items.Add("");
                SelectedIndexChanged += (sender, args) => 
                {
                    ulong Address = AddressedItems[SelectedItems[0].Index].Address;
                    if (BreakpointsSource.Contains(Address))
                    {
                        BreakpointsSource.Remove(Address);
                    } else
                    {
                        BreakpointsSource.Add(Address);
                    }                                      
                };
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                SetWindowTheme(Handle, null, null);
            }
            protected override void OnDrawItem(DrawListViewItemEventArgs e)
            {
                if(AddressedItems.Count() > 0)
                {
                    if (e.ItemIndex == 0) { Items.Clear(); Items.Add(""); }
                    if (AddressedItems.Count() - 1 > e.ItemIndex) { Items.Add(""); }
                    //keep going if there is another item
                    //http://prntscr.com/oibinm not great, but it isn't the part slowing this down

                    Disassembler.DisassembledItem Line = AddressedItems[e.ItemIndex];
                    e.Graphics.FillRectangle(ItemColours[(BreakpointsSource.Contains(Line.Address) ? 1 : 0)], e.Bounds);
                    Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);

                    string[] SortedText = Util.Core.SeparateString(Line.DisassembledLine.Substring(2, 18), "0", StopAtFirstDifferent: true);
                    e.Graphics.DrawString($"  {SortedText[0]}{Line.DisassembledLine.Substring(18)}", BaseUI.BaseFont, TextBrushes[2], HeightCenteredBounds);
                    e.Graphics.DrawString($"0x{SortedText[1]}", BaseUI.BaseFont, TextBrushes[3], HeightCenteredBounds);
                }                
            }
        }
    }
}
