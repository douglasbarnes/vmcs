using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Set : Opcode 
    {
        byte Result;
        public Set(DecodedTypes.IMyDecoded input, Condition setCondition, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("SET" + Disassembly.DisassembleCondition(setCondition), input, settings | OpcodeSettings.BYTEMODE)
        {
            Result = (byte)(TestCondition(setCondition) ? 1 : 0);
        }
        public override void Execute()
        {
            Set(Bitwise.ZeroExtend(new byte[] { Result }, (byte)Capacity));
        }
    }
}
