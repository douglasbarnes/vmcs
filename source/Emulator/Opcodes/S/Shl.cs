// SHL, shift left. SHL and SAL are identical, as any differentiation between the two like seen in SAR/SHR would be
// nonsense as the bits are moving in the direction of the sign bit. For explanation of shifts see Bitwise.ShiftLeft().
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Shl : Opcode
    {
        
        public Shl(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("SHL", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> Operands = Fetch();

            // Shift $operands[0] left $operands[1] times.
            byte[] Result;
            FlagSet ResultFlags = Bitwise.ShiftLeft(Operands[0], Operands[1][0], out Result);

            // Store the result in the first operand and set flag appropriately.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
