// FlagPanel takes a dictionary that represents flags and turns it into several flag labels displaying the state of each flag.
// It is not dependent on any other part of the source, such that flag names could be changed by the caller to whatever they want.
// Generally I prefer controls to act like this wherever possible, however there are some cases where massive advantages would be
// lost if they did not use lower level data structures.
using System.Collections.Generic;
using System.Drawing;
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
        public FlagPanel() : base(Layer.Imminent, Emphasis.High)
        {
            Size = new Size(370, 100);
            Controls.AddRange(FlagLabels);

            int PaddedLabelHeight = FlagLabel.LabelSize.Height + 5;
            for (int i = 0; i < FlagCount; i++)
            {
                // Here is an explanation of each of these coordinates,
                // X:
                //  5 : Offset from border
                //  ..Width+5 : Spacing between labels including the size of the label
                //  i/4, 4 per column 
                // Y:
                //  i * ..Height+5 : Spacing between labels, including size of label, but on Y axis
                //  % (this.Size.Height : Make sure the rows don't render out of the control bounds
                //  -..Height) + ..Height : Borrow the height of the current and make sure we leave room for it on the next column 
                FlagLabels[i].Location = new Point(5 + ((FlagLabel.LabelSize.Width + 5) * (i / 4)), ((i * (PaddedLabelHeight)) % (Size.Height - PaddedLabelHeight)) + PaddedLabelHeight);
            }
        }

        public void UpdateFlags(Dictionary<string, bool> inputFlags)
        {
            int FlagLabelIndex = 0;
            foreach (var Flag in inputFlags)
            {
                // Use markdown to reduce the emphasis on flags that are off.
                FlagLabels[FlagLabelIndex].Text = $"{Flag.Key.PadRight(10)}: {(Flag.Value ? "True" : "$False$")}";
                FlagLabelIndex++;
            }
        }
    }
}
