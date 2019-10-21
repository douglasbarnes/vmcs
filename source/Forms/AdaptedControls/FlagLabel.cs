// This class does little itself except ensure a constant size for labels.
using System.Drawing;

namespace debugger.Forms
{
    public class FlagLabel : CustomLabel
    {        
        public static readonly Size LabelSize = new Size(100, 15);
        public FlagLabel() : base(Emphasis.Medium)
        {
            AutoSize = false;
            Size = LabelSize;
        }
    }
}
