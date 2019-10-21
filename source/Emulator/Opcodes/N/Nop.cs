// Nop, no operand. Do nothing. An opcode I like to think was specifically created for hackers,
// but maybe has a genuine use case somewhere.
namespace debugger.Emulator.Opcodes
{
    public class Nop : Opcode
    {
        public Nop() : base("NOP", new DecodedTypes.NoOperands(), OpcodeSettings.NONE) { }
        public override void Execute() { }
    }
}
