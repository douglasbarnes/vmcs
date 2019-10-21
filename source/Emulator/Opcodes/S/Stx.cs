// Stx represents Stc, std, and cmc. All these functions simply modify the a flag
// - STC : Set the carry flag on
// - STD : Set the direction flag on
// - CMC : Compliement the carry flag, i.e XOR it by 1.
namespace debugger.Emulator.Opcodes
{
    public class Stc : Opcode
    {
        public Stc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("STC", input, settings)
        {
        }
        public override void Execute()
        {
            // Set the carry flag on
            ControlUnit.SetFlags(new FlagSet() { Carry = FlagState.ON });
        }
    }
    public class Std : Opcode
    {
        public Std(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("STD", input, settings)
        {
        }
        public override void Execute()
        {
            // Set the direction flag on
            ControlUnit.SetFlags(new FlagSet() { Direction = FlagState.ON });
        }
    }
    public class Cmc : Opcode
    {
        public Cmc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CMC", input, settings)
        {
        }
        public override void Execute()
        {
            // If the carry flag is on, turn it off. If the carry flag is off, turn it on.
            ControlUnit.SetFlags(new FlagSet() { Carry = (ControlUnit.Flags.Carry == FlagState.ON) ? FlagState.OFF : FlagState.ON });
        }
    }
}
