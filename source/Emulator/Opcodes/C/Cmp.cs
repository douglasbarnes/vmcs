// Cmp; Compare. Compare two operands. It can give a range of information, not just equality, see
// Disassembly and the Opcode base class for more information on condition codes.
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Cmp : Opcode
    {
        public Cmp(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE)
            : base("CMP", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // Subtract operand 1 from operand 0 and discard the result, keeping only the flags. This is
            // the essence of a compare in assembly.
            FlagSet Result = Bitwise.Subtract(DestSource[0], DestSource[1], out _);         
            ControlUnit.SetFlags(Result);
        }
    }
}
