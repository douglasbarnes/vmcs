using System.Drawing;
using System.Collections.Generic;
namespace debugger.Forms
{
    public class FlagPanel : BorderedPanel
    {
        private const int FlagCount = 6;
        private readonly FlagLabel[] FlagLabels = new FlagLabel[FlagCount]
        {
            new FlagLabel(),
            new FlagLabel(),
            new FlagLabel(),
            new FlagLabel(),
            new FlagLabel(),
            new FlagLabel(),
        };
        private static int PaddedLabelHeight = FlagLabel.LabelSize.Height+5;
        public FlagPanel() : base(Layer.Imminent, Emphasis.High)
        {
            Size = new Size(370, 100);
            Controls.AddRange(FlagLabels);
            for (int i = 0; i < FlagCount; i++)
            {
               
                FlagLabels[i].Location = new Point
                    (5 + ((FlagLabel.LabelSize.Width+5) * (i / 4))
                    //                  X
                    // 5 : Offset from border
                    // ..Width+5 : Spacing between labels including the size of the label
                    // i/4, 4 per column   
                    , ((i * (PaddedLabelHeight)) % (Size.Height - PaddedLabelHeight)) + PaddedLabelHeight);                
                //                  Y
                //i * ..Height+5 : Spacing between labels, including size of label, but on Y axis
                //% (this.Size.Height : Make sure the rows don't render out of the control bounds
                //-..Height) + ..Height : Borrow the height of the current and make sure we leave room for it on the next column 
            }
        }

        public void UpdateFlags(Dictionary<string, bool> inputFlags)
        {
            int FlagLabelIndex = 0;
            foreach (var Flag in inputFlags)
            {
                FlagLabels[FlagLabelIndex].Text = $"{Flag.Key.PadRight(10)}: {(Flag.Value ? "True" : "$False$")}";
                FlagLabelIndex++;
            }
        }
    }
}
