namespace debugger.Emulator.Opcodes
{
    public class Stc : Opcode
    {
        public Stc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("STC", input, settings)
        {            
        } 
        public override void Execute()
        {
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
            ControlUnit.SetFlags(new FlagSet() { Direction = FlagState.ON });
        }
    }
    //F5
    public class Cmc : Opcode
    {
        public Cmc(DecodedTypes.NoOperands input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CMC", input, settings)
        {
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(new FlagSet() { Carry = (ControlUnit.Flags.Carry == FlagState.ON) ? FlagState.OFF: FlagState.ON });
        }
    }
}
