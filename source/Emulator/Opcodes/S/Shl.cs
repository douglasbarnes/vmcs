using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Shl : Opcode 
    {
        byte[] Result;
        FlagSet ResultFlags;
        public Shl(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("SHL", input, settings)
        {
            List<byte[]> Operands = Fetch();
            ResultFlags = Bitwise.ShiftLeft(Operands[0], Operands[1][0], (int)Capacity, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
