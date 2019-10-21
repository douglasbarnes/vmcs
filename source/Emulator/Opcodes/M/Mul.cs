// MUl or IMUL. This class handle both of them, as Bitwise.Multiply() does most of the hard work.
// IMUL has 3 forms, which are a superset of the 2 forms MUL has.
//  imul a:d rm1      : result -> a = least significant bytes,  d = most significant bytes
//  imul rm1 rm2     : rm1 = cut(rm1*rm2, rm1.length)
//  imul rm1 rm2 imm : rm1 = cut(rm2*imm, rm1.length) (rm1 and rm2 always have the same length)
// MUL has forms 0 and 1 of the above. In 1 and 2, cut(x,y) denotes a truncation to length y.
// In each case, the last two operands are multiplied. This only matters for form 2, where rm1 is
// used only to store the truncated result of rm2*imm. When using form 0, the A and D registers hold
// each half of the result, resulting in the total capacity of twice the size of rm1. I.e, any sized
// multiplication can be done without any result being truncated.
// The reason MUL does not have these extra forms is because forms 1 and 2 will have the same result
// regardless of signed/unsigned multiplication because of the truncation. This is because the sign of
// the product is preserved by sign extending it in the result. This means that using IMUL/MUL where they
// are not interchangeable will result in useless and incorrect results.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Mul : Opcode
    {
        public Mul(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base((settings | OpcodeSettings.SIGNED) == settings ? "IMUL" : "MUL", input, settings)
        {            
            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> Operands = Fetch();

            // Perform multiplication of the last two operands(read summary)
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Multiply(Operands[Operands.Count - 1], Operands[Operands.Count - 2], (Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity, out Result);

            // Set results.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
