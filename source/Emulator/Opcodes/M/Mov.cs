// Move. The most common opcode throughout assembly, to move data from one place to another.
// It is important to know its specific behaviour,
// - Moving from a 32 bit register to a 64 bit register will zero extend the operand to 64 bits.
// - "Mov"ing a DWORD register to another DWORD register or QWORD to another QWORD register is
//   essentially instant. This is because newer processors(post-pentium most likely) have many
//   registers, not just the ones you see labelled. When a register is "mov"d into, as a byproduct
//   of the previous bullet point, it can be moved into a different register instead, that the
//   processor will then pretend is $RAX(or what ever was "mov"d into). This process in theory takes
//   zero cycles, and is likely the reason for the previous bullet point. Naturally, this could
//   probably be exhausted by using every register available, causing albeit very minor delays clearing
//   that register.
namespace debugger.Emulator.Opcodes
{
    public class Mov : Opcode
    {
        public Mov(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("MOV", input, settings)
        {
        }
        public override void Execute()
        {
            // Set the destination to the source, which is by convention stored in Fetch()[1]
            Set(Fetch()[1]);
        }
    }
}
