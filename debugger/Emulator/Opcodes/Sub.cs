using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Sub : Opcode 
    {
        byte[] Result;
        FlagSet ResultFlags;
        public Sub(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None, bool UseBorrow = false) : base((UseBorrow) ? "SBB" : "SUB", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            ResultFlags = Bitwise.Subtract(DestSource[0], DestSource[1], (int)Capacity, out Result, (ControlUnit.Flags.Carry == FlagState.ON && UseBorrow));
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
