// And - Perform a bitwise AND between two operands and store it. Storing it is the difference between this
// instruction and Test. It is important to understand when these instructions are equivalent, it will save
// you from having to delegate a specific register to the result of the AND. 
// A bitwise AND is simply a logical AND across every bit of the operands iterated in parallel.
// E.g for bits i,j,k in each byte,
//  Result[i..k] = input[i..k] & input2[i..k]
// Where & returns 1 for true and 0 for false.
// See Bitwise.And().
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class And : Opcode
    {
        public And(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("AND", input, settings)
        {
            
        }

        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // Perform AND. The position of the two arguments is not important.
            byte[] Result;
            FlagSet ResultFlags = Bitwise.And(DestSource[0], DestSource[1], out Result); 
            
            // Set results
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
