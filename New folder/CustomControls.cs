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
using System.Collections.Specialized;

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
                DoubleBuffered = true;
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
                int Offset = TextRenderer.MeasureText(Tag.ToString(), BaseUI.BaseFont).Width;
                GraphicsHandler.DrawLine(new Pen(BaseUI.BackgroundColour), new Point(Bounds.Location.X + 11, Bounds.Location.Y), new Point(Bounds.Location.X + Offset + 14, Bounds.Location.Y));
                Bounds.X += 15;
                Bounds.Y -= 7;
                Bounds.Width += 15;
                Bounds.Height += 15;
                //GraphicsHandler.DrawString((string)Tag, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);          
                GraphicsHandler.DrawString(Tag.ToString(), BaseUI.BaseFont, LayerBrush, Bounds);
                GraphicsHandler.DrawString(Tag.ToString(), BaseUI.BaseFont, ElevatedTransparentOverlays[(int)DrawingLayer+1], Bounds);
            }
        }
        public abstract class CustomLabel : Label, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomLabel(Emphasis textLayer) : base()
            {
                TextEmphasis = textLayer;
            }
            protected override void OnPaint(PaintEventArgs e)
            {         
                Util.Drawing.DrawFormattedText(Text, e.Graphics, e.ClipRectangle, TextEmphasis);
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
        }
        public class RegisterPanel : CustomPanel
        {
            public int RegSize { get; private set; } = 8;
            public Action OnRegSizeChanged = () => { };
            public RegisterPanel() : base(Layer.Background, Emphasis.High)
            {
            }
            protected override void OnMouseDoubleClick(MouseEventArgs e)
            {
                NextRegSize();
            }
            public void NextRegSize()
            {
                RegSize = (RegSize == 1) ? 8 : RegSize / 2;
                OnRegSizeChanged.Invoke();
            }
            
        }
        public class RegisterLabel : CustomLabel
        {
            public RegisterLabel() : base(Emphasis.High) { }
            protected override void OnMouseDoubleClick(MouseEventArgs e)
            {
                ((RegisterPanel)Parent).NextRegSize();
                base.OnMouseDoubleClick(e);
            }
        }
        public class FlagLabel : CustomLabel
        {
            public FlagLabel() : base(Emphasis.High) { }
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
            public BindingList<ulong> BreakpointSource = new BindingList<ulong>();
            

            private SolidBrush[] ItemColours = new SolidBrush[]
            {
                LayerBrush,
                PrimaryBrush
            };
            public DisassemblyListView() : base(Layer.Background, Emphasis.High, Emphasis.Medium)
            {
                DoubleBuffered = true;
                Columns.Add(new ColumnHeader() { Width = 692 });// width of DissassemblyPadding, allows the border to not get cropped off by the listview
                SendToBack();
                SelectedIndexChanged += (sender, args) =>
                {
                    if(SelectedItems.Count > 0)
                    {
                        ulong Address = Convert.ToUInt64(Items[SelectedItems[0].Index].SubItems[1].Text);
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
            }
            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                SetWindowTheme(Handle, null, null);
            }
            protected override void OnDrawItem(DrawListViewItemEventArgs e)
            {
                e.Graphics.FillRectangle(ItemColours[(BreakpointSource.Contains(Convert.ToUInt64(e.Item.SubItems[1].Text)) ? 1 : 0)], e.Bounds);
                Rectangle HeightCenteredBounds = new Rectangle(new Point(e.Bounds.X, e.Bounds.Y + 3), e.Bounds.Size);               
                string[] SortedText = Util.Core.SeparateString(e.Item.Text.Substring(2, 16), "0", stopAtFirstDifferent: true);
                e.Graphics.DrawString($"  {SortedText[0]}{e.Item.Text.Substring(18)}", BaseUI.BaseFont, TextBrushes[2], HeightCenteredBounds);
                e.Graphics.DrawString($"0x{SortedText[1]}", BaseUI.BaseFont, TextBrushes[3], HeightCenteredBounds);            
            }
            
            public void AddParsed(List<Disassembler.DisassembledItem> ParsedLines)
            {
                List<ListViewItem> Output = new List<ListViewItem>();
                foreach (Disassembler.DisassembledItem Line in ParsedLines)
                {                   
                    Output.Add(new ListViewItem(new string[] { Line.DisassembledLine, Line.Address.ToString() }));
                }
                Invoke(new Action(() =>
                {
                    BeginUpdate();
                    Items.Clear();
                    Items.AddRange(Output.ToArray());
                    EndUpdate();
                }));
            }
        }
    }
}
