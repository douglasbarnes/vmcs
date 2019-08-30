using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Sxr : Opcode 
    {
        byte[] Result;
        FlagSet ResultFlags;
        public Sxr(DecodedTypes.IMyDecoded input, bool arithmetic,  OpcodeSettings settings = OpcodeSettings.NONE) : base(arithmetic ? "SAR" : "SHR", input, settings)
        {
            List<byte[]> Operands = Fetch();
            byte Mask = (byte)(Capacity == RegisterCapacity.GP_QWORD ? 0b00111111 : 0b00011111);
            ResultFlags = Bitwise.ShiftRight(Operands[0], Operands[1][0], Mask,(int)Capacity, out Result, arithmetic);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
