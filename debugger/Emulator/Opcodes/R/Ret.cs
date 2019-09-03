using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Ret : Opcode 
    {
        List<byte[]> Operands;
        public Ret(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("RET", input, RegisterCapacity.WORD, settings)
        {
            Operands = Fetch();
        }
        public override void Execute()
        {
            ControlUnit.Jump(BitConverter.ToUInt64(StackPop(RegisterCapacity.QWORD), 0));
            if(Operands.Count > 0)
            {
                byte[] NewSP;
                Bitwise.Add(ControlUnit.FetchRegister(XRegCode.SP, RegisterCapacity.QWORD), Bitwise.ZeroExtend(Operands[0], 8), 8, out NewSP);
                ControlUnit.SetRegister(XRegCode.SP, NewSP);
            }
        }
    }
}
