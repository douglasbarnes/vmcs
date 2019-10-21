using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Add : Opcode // 01 32bit, 00 8bit,
    { // imm // 05=EAX, 81=any  04=al, 80=8bit
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        public Add(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE, bool UseCarry = false) : base((UseCarry) ? "ADC" : "ADD", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            ResultFlags = Bitwise.Add(DestSource[0], DestSource[1], (int)Capacity, out Result, (ControlUnit.Flags.Carry == FlagState.ON && UseCarry));
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(ResultFlags);
            Set(Result);
        }
    }
}
