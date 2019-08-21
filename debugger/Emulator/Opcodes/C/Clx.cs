using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Clc : Opcode
    {
        public Clc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CLC", input, settings)
        {
        } 
        public override void Execute()
        {
            ControlUnit.SetFlags(new FlagSet() { Carry = FlagState.ON });
        }
    }
    public class Cld : Opcode
    {
        public Cld(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CLC", input, settings)
        {
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(new FlagSet() { Direction = FlagState.ON });
        }
    }
}
