using System;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Call : Opcode
    {
        public Call(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("CALL", input, 
                  (settings | OpcodeSettings.RELATIVE) == settings ? RegisterCapacity.GP_DWORD : RegisterCapacity.GP_QWORD
                  ,settings)
        {
        } 
        public override void Execute()
        {
            //ControlUnit.SetMemory(ControlUnit.FetchRegister(XRegCode.S))
            ControlUnit.Jump(BitConverter.ToUInt64(Fetch()[0], 0));
        }
    }
}
