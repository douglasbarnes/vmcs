namespace debugger.Emulator.Opcodes
{
    public class Pop : Opcode
    {
        byte[] StackBytes;                                // no 32 bit mode for reg pop, default it to 64
        public Pop(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("POP", input, (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.WORD : RegisterCapacity.QWORD, settings)
        {
            StackBytes = StackPop();
        }
        public override void Execute()
        {
            Set(StackBytes); // pop 0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction so i just point it to the generic pop function  
        }
    }
}
