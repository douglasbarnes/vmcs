using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Rxl : Opcode 
    {
        byte[] Result;
        FlagSet ResultFlags;
        public Rxl(DecodedTypes.IMyDecoded input, bool useCarry, OpcodeSettings settings = OpcodeSettings.NONE) : base(useCarry ? "RCL" : "ROL", input, settings)
        {
            List<byte[]> Operands = Fetch();
            ResultFlags = Bitwise.RotateLeft(Operands[0], Operands[1][0], Capacity, useCarry, ControlUnit.Flags.Carry == FlagState.ON,  out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
