// Pop a value from the stack into a register/memory location.
// Pop(0x8F) technichally is an extended opcode because it has 1 operand, and is also stated to be one in the intel manual, however 
// having an extended opcode table for a single opcode seems strange to me, so instead it has its own position in the opcode table.
namespace debugger.Emulator.Opcodes
{
    public class Pop : Opcode
    {   
        public Pop(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE)

            // Uses a specific register capacity derivation. If there is a SIZEOVR prefix, it is a WORD, otherwise a QWORD. There is no
            // other case.
            : base("POP", input, settings, (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.WORD : RegisterCapacity.QWORD)
        {
        }
        public override void Execute()
        {
            // Set the destination to the bytes popped off the stack.
            Set(StackPop());   
        }
    }
}
