using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Mul : Opcode
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        readonly List<byte[]> Operands;
        public Mul(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base((settings | OpcodeSettings.SIGNED) == settings ? "IMUL" : "MUL", input, settings)
        {
            Operands = Fetch();
            //imul opcodes come in the format,
            //imul rm1 : a = upper(rm1*a) d = lower(rm1*a)
            //imul rm1 rm2 : rm1 = cut(rm1*rm2, rm1.length)
            //imul rm1 rm2 imm : rm1 = cut(rm2*imm, rm1.length) (rm1 and rm2 always have the same length)
            ResultFlags = Bitwise.Multiply(Operands[Operands.Count - 1], Operands[Operands.Count - 2], (Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
