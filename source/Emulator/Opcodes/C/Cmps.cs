// Cmps; compare strings. The string of bytes at DI will be compared with the string of bytes at SI.
// Most commonly will be seen with a REPZ/REPNZ prefix to determine the equality of an entire string
// rather than just one word-scale length.
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Cmps : StringOperation
    {
        private FlagSet Result;
        public Cmps(StringOpSettings settings = StringOpSettings.NONE)
            : base("CMPS", XRegCode.DI, XRegCode.SI, settings | StringOpSettings.COMPARE) { }
        protected override void OnInitialise()
        {
            List<byte[]> DestSource = Fetch();

            // Carry out the subtraction and discard the results(That is how comparison works in assembly, it has the same flags as subtraction).
            Result = Bitwise.Subtract(DestSource[0], DestSource[1], (int)Capacity, out _);
        }
        protected override void OnExecute()
        {
            // Set flags          
            ControlUnit.SetFlags(Result);

            // Adjust DI and SI, because both were used. See base class.
            AdjustDI();
            AdjustSI();
        }
    }
}
