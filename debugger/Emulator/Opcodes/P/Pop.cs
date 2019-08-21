using System;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Pop : Opcode
    {
        byte[] StackBytes;                                // no 32 bit mode for reg pop, default it to 64
        public Pop(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("POP", input, (ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.GP_WORD : RegisterCapacity.GP_QWORD, settings)
        {
            StackBytes = ControlUnit.Fetch(BitConverter.ToUInt64(ControlUnit.FetchRegister(XRegCode.SP, RegisterCapacity.GP_QWORD), 0), (int)Capacity);
        }
        public override void Execute()
        {
            Set(StackBytes); // pop 0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction so i just point it to the generic pop function  
            byte[] NewSP;
            Bitwise.Add(ControlUnit.FetchRegister(XRegCode.SP, RegisterCapacity.GP_QWORD), new byte[] { (byte)Capacity, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            ControlUnit.SetRegister(XRegCode.SP, NewSP);
        }
    }
}
