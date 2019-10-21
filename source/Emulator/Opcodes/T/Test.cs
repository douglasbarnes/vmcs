// Test does an AND on two operands without storing the result. I have only ever seen it used for
// checking if a register is zero, because the zero flag will be set if the discarded result is zero.
// Similar to CMP.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Test : Opcode
    {
        public Test(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("TEST", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // AND the operands and discard the result
            FlagSet ResultFlags = Bitwise.And(DestSource[0], DestSource[1], out _);       

            // Set the flags to the result.
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
