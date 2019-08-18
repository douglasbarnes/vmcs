using System;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Jmp : Opcode
    {
        private readonly Condition JmpCondition;
        public Jmp(DecodedTypes.IMyDecoded input, Condition condition=Condition.NONE) 
            : base(condition == Condition.NONE ? "JMP" : "J" + Util.Disassembly.DisassembleCondition(condition)
                  , input
                  , (ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.DWORD))
        {
            JmpCondition = condition;
        }
        public override void Execute()
        {
           if(TestCondition(JmpCondition)) {
                ControlUnit.InstructionPointer = BitConverter.ToUInt64(Bitwise.SignExtend(Fetch()[0], 8),0);
            }            
        }
    }
}
