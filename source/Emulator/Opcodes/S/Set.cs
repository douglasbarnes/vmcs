// Setcc, or set as I wil refer to it as for simplicity, will set a pointer or register to the result of a condition.
// If the condition is true it will be set to 0, otherwise 1. This value is sign extended to the size of the register
// or size of memory the pointer points to, whichever applicable. In many high level languages, a boolean true is -1, 
// i.e, every bit in the byte is set, and all zeroes for a boolean false i.e no bit in the byte is set. 
// A clever trick to mimic this with this opcode is to use the opposite condition that you are testing for, e.g,
// if comparing which value is greater, instead of using SETA(set above), use SETBE(set below or equal), then decrement
// by one with DEC. This will give 0 if the intended test was false and -1(0xFF) if true. 
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Set : Opcode
    {
        byte Result;
        public Set(DecodedTypes.IMyDecoded input, Condition setCondition, OpcodeSettings settings = OpcodeSettings.NONE)
            : base("SET" + Disassembly.DisassembleCondition(setCondition), input, settings | OpcodeSettings.BYTEMODE)
        {
            // Determine the value that will be set. 1 if the condition is true, 0 if false.
            Result = (byte)(TestCondition(setCondition) ? 1 : 0);
        }
        public override void Execute()
        {
            // Set the destination operand to the zero extended value of $result.
            // This would be interchangable will sign extending, as the value would always be non-negative.
            Set(Bitwise.ZeroExtend(new byte[] { Result }, (byte)Capacity));
        }
    }
}
