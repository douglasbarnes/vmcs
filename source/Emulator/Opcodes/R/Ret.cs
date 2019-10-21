using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Ret : Opcode
    {
        List<byte[]> Operands;
        public Ret(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("RET", input, settings, RegisterCapacity.WORD)
        {
            Operands = Fetch();
        }
        public override void Execute()
        {
            ControlUnit.Jump(BitConverter.ToUInt64(StackPop(RegisterCapacity.QWORD), 0));
            if (Operands.Count > 0)
            {
                byte[] NewSP;
                ControlUnit.RegisterHandle StackPointer = new ControlUnit.RegisterHandle(XRegCode.SP, RegisterTable.GP, RegisterCapacity.QWORD);
                Bitwise.Add(StackPointer.FetchOnce(), Bitwise.ZeroExtend(Operands[0], 8), 8, out NewSP);
                StackPointer.Set(NewSP);
            }
        }
    }
}
