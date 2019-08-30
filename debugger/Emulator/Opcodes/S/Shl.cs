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
            byte Mask = (byte)(Capacity == RegisterCapacity.GP_QWORD ? 0b00111111 : 0b00011111);
            ResultFlags = Bitwise.ShiftLeft(Operands[0], Operands[1][0], Mask,(int)Capacity, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
