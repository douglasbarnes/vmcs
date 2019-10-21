// Perform a bitwise OR on two bytes and store the results in the destination, the first operand. To do a bitwise OR, perform
// a logical OR on each of the bits of each operand in parallel, then store the result.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Or : Opcode
    {
        public Or(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("OR", input, settings)
        {
            
        }

        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // Perform the OR.
            byte[] Result;            
            FlagSet ResultFlags = Bitwise.Or(DestSource[0], DestSource[1], out Result);

            // Store results
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
