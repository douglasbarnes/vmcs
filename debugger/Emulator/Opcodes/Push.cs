using System;
namespace debugger.Emulator.Opcodes
{
    public class Push : Opcode
    {
        byte[] Result;
        //push imm8 is valid but not push r8                // no 8/32 bit mode for reg push  
        public Push(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) 
            : base("PUSH", input, ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.QWORD, settings)
        {
            Result = Fetch()[0];
        }
        public override void Execute()
        {
            ControlUnit.SetMemory(Convert.ToUInt64(ControlUnit.FetchRegister(ByteCode.SP, RegisterCapacity.QWORD)), Result); 
        }
    }
}
