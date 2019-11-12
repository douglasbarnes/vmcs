// Scan string, or more practically, compare a string at *DI with the value of the A register. Like all string ops,
// this size is inferred from prefixes/opcodes, specifics can be found in the base constructor. This is a comparison
// operator, therefore uses REPZ/REPNZ rather than REP. 
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Scas : StringOperation
    {
        FlagSet ResultFlags;
        public Scas(StringOpSettings settings = StringOpSettings.NONE)
            : base("SCAS", XRegCode.DI, XRegCode.A, settings | StringOpSettings.COMPARE)
        {
        }
        protected override void OnInitialise()
        {
            // Fetch operands 
            List<byte[]> Operands = Fetch();

            // Subtract and store the result flags. Like cmp, only the flags are stored.
            ResultFlags = Bitwise.Subtract(Operands[1], Operands[0], out _);
        }
        protected override void OnExecute()
        {
            // Set flags
            ControlUnit.SetFlags(ResultFlags);

            // See base class.
            AdjustDI();
        }
    }
}
