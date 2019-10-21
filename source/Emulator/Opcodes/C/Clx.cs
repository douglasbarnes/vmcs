// Clx represents clc and cld; Clear carry and Clear direction. Simply each flag is set off repectively.
namespace debugger.Emulator.Opcodes
{
    public class Clc : Opcode
    {
        public Clc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CLC", input, settings)
        {
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(new FlagSet() { Carry = FlagState.OFF });
        }
    }
    public class Cld : Opcode
    {
        public Cld(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CLD", input, settings)
        {
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(new FlagSet() { Direction = FlagState.OFF });
        }
    }
}
