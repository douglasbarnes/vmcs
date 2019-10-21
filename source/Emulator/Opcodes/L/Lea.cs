using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Lea : Opcode
    {
        readonly byte[] SourceAddress;
        public Lea(DecodedTypes.ModRM input, OpcodeSettings settings = OpcodeSettings.NONE) : base("LEA", input, settings)
        {
            SourceAddress = Bitwise.Cut(BitConverter.GetBytes(input.EffectiveAddress), (int)Capacity);
        }
        public override void Execute()
        {
            Set(SourceAddress);
        }
        public override List<string> Disassemble()
        {
            return base.Disassemble();
        }
    }
}
