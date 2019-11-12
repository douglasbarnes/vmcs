// Sxr represents SHR and SAR. The difference between those being the preservation of the sign bit.
// E.g SAR, if operand[0] is negative(the operand being shifted), the result will also be negative
// SHR, no sign bit adjustments
//   [10010101] >> 1 = [01001010]
// SAR, sign bit preserved
//   [10010101] >> 1 = [11001010]
//                      ^- This was set afterwards because the input was negative
// See Bitwise.ShiftRight() for specific implementation.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Sxr : Opcode
    {
        private bool Arithmetic;
        public Sxr(DecodedTypes.IMyDecoded input, bool arithmetic, OpcodeSettings settings = OpcodeSettings.NONE) : base(arithmetic ? "SAR" : "SHR", input, settings)
        {
            Arithmetic = arithmetic;            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> Operands = Fetch();

            // Shift operands[0], operands[1][0] times(the LSB of operands[1])
            byte[] Result;
            FlagSet ResultFlags = Bitwise.ShiftRight(Operands[0], Operands[1][0], out Result, Arithmetic);

            // Set the results.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
