using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using debugger.Hypervisor;
using debugger.Util;
using static debugger.FormSettings;
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
                Drawing.DrawFormattedText(Text, e.Graphics, bounds:e.ClipRectangle, defaultEmphasis:TextEmphasis);
            }
        }
        public abstract class CustomListview : ListView, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public Emphasis HeaderEmphasis;
            public CustomListview(Layer drawingLayer, Emphasis textEmphasis, Emphasis headerEmphasis) : base()
            {
                DoubleBuffered = true;
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
                e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(Bounds, Text, BaseUI.BaseFont));               
            }  
        }
        public abstract class CustomToolStripMenuItem : ToolStripMenuItem, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomToolStripMenuItem(Layer drawingLayer, Emphasis textEmphasis)
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(e.ClipRectangle, Text, BaseUI.BaseFont));
                
                //base.OnPaint(e);
            }
        }
        public abstract class CustomTextBox : TextBox, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomTextBox(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
            }
        }
        public abstract class CustomToolStripTextBox : ToolStripMenuItem, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public string Input = "";
            public string Prefix = "";
            public int BufferSize = 15;
            public CustomToolStripTextBox(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
                AutoSize = true;
            }
            protected void Ready()
            {                
                Text = Prefix + Input;
            }
            private KeysConverter Converter = new KeysConverter();
            public void KeyPressed(object s,PreviewKeyDownEventArgs e)
            {
                if (e.KeyCode == Keys.Back && Input.Length > 0)
                {
                    Input = Input.Remove(Input.Count() - 1);
                }
                string Key = Converter.ConvertToString(e.KeyCode).ToLower();
                if (Input.Length < BufferSize && Key.Length == 1 && "abcdefghijklmnopqrstuvwxyz!\"!£$%^&*()[];'#,./<>?:@~{}1234567890".Contains(Key))
                {
                    Input += Key;
                }
                Ready();
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Rectangle Bounds = Drawing.GetCenterHeight(e.ClipRectangle);
                Drawing.DrawShadedRect(e.Graphics, Drawing.ShrinkRectangle(e.ClipRectangle, 0), Layer.Imminent, 3);
                e.Graphics.DrawString(Prefix, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
                Bounds.Offset(Drawing.CorrectedMeasureText(Prefix, BaseUI.BaseFont).Width, 0);
                e.Graphics.DrawString(Input, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis + 1], Bounds);

            }
        }
        public abstract class CustomToolStripButton : ToolStripButton, IMyCustomControl
        {
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomToolStripButton(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Rectangle Bounds = e.ClipRectangle;
                e.Graphics.FillRectangle(LayerBrush, Bounds);
                e.Graphics.FillRectangle(ElevatedTransparentOverlays[(int)DrawingLayer], Bounds);
                Rectangle InnerBounds = new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 3, Bounds.Height - 3);
                e.Graphics.DrawRectangle(new Pen(ElevatedTransparentOverlays[(int)DrawingLayer]), InnerBounds);
                e.Graphics.DrawString(Text, BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Drawing.GetCenter(Bounds, Text, BaseUI.BaseFont));
            }
        }
        public abstract class CustomMenuStrip : MenuStrip, IMyCustomControl
        {
            public class ThemeRenderer : ToolStripRenderer
            {
                private Layer DrawingLayer;
                private Emphasis TextEmphasis;
                public ThemeRenderer(Layer layer, Emphasis emphasis)
                {
                    TextEmphasis = emphasis;
                    DrawingLayer = layer;
                }
                protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
                {
                    Drawing.FillShadedRect(e.Graphics, Rectangle.Round(e.Graphics.ClipBounds), Layer.Foreground);
                }
            }
            public Layer DrawingLayer { get; set; }
            public Emphasis TextEmphasis { get; set; }
            public CustomMenuStrip(Layer drawingLayer, Emphasis textEmphasis) : base()
            {
                DrawingLayer = drawingLayer;
                TextEmphasis = textEmphasis;
                Renderer = new ThemeRenderer(drawingLayer, textEmphasis);
            }
        }
        public class BorderedPanel : CustomPanel
        {
            public BorderedPanel(Layer layer, Emphasis emphasis) : base(layer, emphasis)
            {
            }
            public BorderedPanel() : base(Layer.Imminent, Emphasis.High)
            {

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
                GraphicsHandler.DrawString(Tag.ToString(), BaseUI.BaseFont, ElevatedTransparentOverlays[(int)DrawingLayer + 1], Bounds);
            }
        }
        public class RegisterPanel : BorderedPanel
        {
            public int RegSize { get; private set; } = 8;
            public Action OnRegSizeChanged = () => { };
            public RegisterPanel() : base(Layer.Imminent, Emphasis.High)
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
            public FlagLabel() : base(Emphasis.Medium) { }
        }
        public class StepButton : CustomButton
        {
            public StepButton() : base(Layer.Surface, Emphasis.Medium) { FlatAppearance.BorderSize = 0; CustomBorder = true; }
        }
        public class DisassemblyListView : CustomListview
        {
            public BindingList<ulong> BreakpointSource = new BindingList<ulong>();
            private SolidBrush[] ItemColours = new SolidBrush[]
            {
                LayerBrush,
                PrimaryBrush
            };
            public DisassemblyListView() : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
            {
                
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
                BreakpointSource.ListChanged += (sender,args) => Refresh();
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
        public class MemoryListView : CustomListview
        {
            public MemoryListView() : base(Layer.Imminent, Emphasis.High, Emphasis.Medium)
            {
                BorderStyle = BorderStyle.None;
                Columns.Add(new ColumnHeader(""));
            }

            protected override void OnDrawItem(DrawListViewItemEventArgs e)
            {
                string[] itemstring = new string[e.Item.SubItems.Count+1];
                itemstring[0] = e.Item.Text;
                for (int i = 0; i < e.Item.SubItems.Count; i++)
                {
                    itemstring[i] = e.Item.SubItems[i].Text;
                }
                e.Graphics.DrawString(string.Join(" ", itemstring), BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], e.Bounds);

            }

            private char[] ColumnChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
            {
                Columns[0].Width = Width;
                Rectangle Bounds = e.Bounds;
                e.Graphics.FillRectangle(LayerBrush, Bounds);
                Bounds.X += (int)BaseUI.BaseFont.SizeInPoints / 2;
                e.Graphics.DrawString("                   " + string.Join("  ", ColumnChars), BaseUI.BaseFont, TextBrushes[(int)TextEmphasis], Bounds);
                Bounds.X -= (int)BaseUI.BaseFont.SizeInPoints / 2;
            }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                //base.OnMouseClick(e);
            }
        }
        public class ThemedMenuStrip : CustomMenuStrip
        {            
            public ThemedMenuStrip(Layer layer, Emphasis emphasis) : base(layer, emphasis)
            {
                               
            }
        }
        public class ThemedToolStripMenuHeader : CustomToolStripMenuItem
        {
            public ThemedToolStripMenuHeader() : base(Layer.Background, Emphasis.Medium)
            {
                DropDown.Closing += OnDropdownClosing;
                AutoSize = true;
               
            }
            private void OnDropdownClosing(object s, ToolStripDropDownClosingEventArgs e)
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                {
                    e.Cancel = true;
                }
            }

        }
        public class ThemedToolStripMenuItem : CustomToolStripMenuItem
        {
            public ThemedToolStripMenuItem() : base(Layer.Background, Emphasis.Medium)
            {
                
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Drawing.DrawShadedRect(e.Graphics, Drawing.ShrinkRectangle(e.ClipRectangle, 0), Layer.Imminent, 3);
                base.OnPaint(e);                
            }

        }
        public class TestcaseSearchTextbox : CustomToolStripTextBox
        {
            public Func<string[]> GetToSearch;
            public DelegateResultClicked ResultClicked;
            public delegate void DelegateResultClicked(string name);
            public TestcaseSearchTextbox(Func<string[]> getToSearch, DelegateResultClicked resultClicked, Layer layer, Emphasis emphasis) : base(layer, emphasis)
            {
                GetToSearch = getToSearch;
                ResultClicked = resultClicked;
                Prefix = "Search: ";
                TextAlign = ContentAlignment.MiddleLeft;
                DropDown.PreviewKeyDown += KeyPressed;
                DropDown.DefaultDropDownDirection = ToolStripDropDownDirection.BelowRight;
                DisplayStyle = ToolStripItemDisplayStyle.Text;                
                DropDown.Opening += DropDown_Opening;
                Ready();
            }
            
            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                DropDown.Hide();
                if(Input.Length > 1)
                {
                    DropDown.Show();
                }                      
            }

            private void DropDown_Opening(object sender, CancelEventArgs e)
            {
                DropDown.Items.Clear();
                if (Input.Length > 1)
                {
                    string[] ToSearch = GetToSearch.Invoke();
                    for (int i = 0; i < ToSearch.Length; i++) // each tosearch string
                    {
                        if (ToSearch[i].Length >= Input.Length) // dont bother if input is bigger than the string
                        {
                            for (int j = 0; j < Input.Length; j++) //each char in string
                            {
                                if (ToSearch[i][j] != Input[j]) break; //if they are different, the strings have a different prefix of size=input.length
                                if (j + 1 == Input.Length)//if this is the final iteration
                                {
                                    var ToAdd = new ThemedToolStripMenuItem() { Text = ToSearch[i] };                                    
                                    ToAdd.Click += (s, a) => ResultClicked(s.ToString());
                                    DropDown.Items.Add(ToAdd);//if we never broke, they were equal
                                }
                            }
                        }
                    }
                }   
                DropDown.Update();               
            }
        }
        public class EndToolStripMenuItem : ThemedToolStripMenuHeader
        {
            private void RefreshPosition()
            {
                int WidthSum = 0;
                for (int i = 0; i <= Parent.Items.IndexOf(this); i++)
                {                    
                    WidthSum += Parent.Items[i].Width;
                }
                Margin = new Padding(Parent.Width - WidthSum - 4, 0, Parent.Width  - WidthSum, 0);
            }
            protected override void OnParentChanged(ToolStrip oldParent, ToolStrip newParent)
            {
                base.OnParentChanged(oldParent, newParent);
                if(newParent != null)
                {
                    RefreshPosition();
                }                
            }

        }
        public class ThemedToolStripRadioButton : CustomToolStripButton
        {
            public ThemedToolStripRadioButton() : base(Layer.Background, Emphasis.Medium)
            {

            }
        }
    }
}
