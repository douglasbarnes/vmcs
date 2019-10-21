using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Sxr : Opcode
    {
        byte[] Result;
        FlagSet ResultFlags;
        public Sxr(DecodedTypes.IMyDecoded input, bool arithmetic, OpcodeSettings settings = OpcodeSettings.NONE) : base(arithmetic ? "SAR" : "SHR", input, settings)
        {
            List<byte[]> Operands = Fetch();
            ResultFlags = Bitwise.ShiftRight(Operands[0], Operands[1][0], (int)Capacity, out Result, arithmetic);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
