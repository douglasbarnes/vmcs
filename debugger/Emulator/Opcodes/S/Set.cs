using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Set : Opcode 
    {
        byte[] Result;
        public Set(DecodedTypes.IMyDecoded input, Condition setCondition, OpcodeSettings settings = OpcodeSettings.NONE) : base("hi", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            if (TestCondition(setCondition))
            {
          //    Result = Bitwise.ZeroExtend(new byte[] { 1 }, (int)Capacity);
            }
        }
        public override void Execute()
        {
            Set(Result);
        }
    }
}
