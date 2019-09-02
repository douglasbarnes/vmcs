using System;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Jmp : Opcode
    {
        private readonly Condition JmpCondition;
        public Jmp(DecodedTypes.IMyDecoded input, Condition condition=Condition.NONE, OpcodeSettings settings = OpcodeSettings.NONE, bool dwordOnly = false) 
            : base(condition == Condition.NONE ? "JMP" : "J" + Disassembly.DisassembleCondition(condition)
                  , input
                  , (settings | OpcodeSettings.BYTEMODE) == settings ? RegisterCapacity.GP_BYTE : JmpRegCap(dwordOnly)
                  , settings)
        {
            JmpCondition = condition;
        }
        public override void Execute()
        {
           if(TestCondition(JmpCondition)) {

                ControlUnit.Jump(BitConverter.ToUInt64(Fetch()[0], 0));
           }            
        }

        private static RegisterCapacity JmpRegCap(bool dwordOnly)
        {           
            if(ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) && !dwordOnly)
            {
                return RegisterCapacity.GP_WORD;
            }           
            return RegisterCapacity.GP_DWORD;
        }
    }
}
