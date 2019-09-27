using System.Collections.Generic;
using debugger.Emulator.DecodedTypes;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Cmps : StringOperation
    {
        private FlagSet Result;
        public Cmps(StringOpSettings settings = StringOpSettings.NONE)
            : base("CMPS", XRegCode.DI, XRegCode.SI, settings | StringOpSettings.COMPARE) {  }
        protected override void OnInitialise()
        {
            List<byte[]> DestSource = Fetch();
            Result = Bitwise.Subtract(DestSource[0], DestSource[1], (int)Capacity, out _);
        }
        protected override void OnExecute()
        {
            //basically all cmp does flags-wise is subtract, but doesn't care about the result               
            ControlUnit.SetFlags(Result);
            AdjustDI();
            AdjustSI();
        }
    }
}
