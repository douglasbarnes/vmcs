namespace debugger.Emulator.Opcodes
{
    public class Nop : Opcode
    {
        public Nop() : base("NOP", new DecodedTypes.NoOperands(), OpcodeSettings.None) { }
        public override void Execute() { }
    }
}
