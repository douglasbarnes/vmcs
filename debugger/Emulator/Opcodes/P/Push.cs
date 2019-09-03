using System;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Push : Opcode
    {
        byte[] Result;
        //push imm8 is valid but not push r8                // no 8/32 bit mode for reg push  
        public Push(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("PUSH", input, FindRegCap(settings), settings)
        {
            Result = Fetch()[0];
            if (Result.Length == 1 || Result.Length == 4) // imms of length b(http://prntscr.com/outmtv),dw(http://prntscr.com/ouyxwd) are sxted to 8. manual suggests words are too but they aren't
            {
                Result = Bitwise.SignExtend(Result, 8);
            }                    
        }
        public override void Execute()
        {
            StackPush(Result);
        }

        private static RegisterCapacity FindRegCap(OpcodeSettings settings)
        {
            if((settings | OpcodeSettings.IMMEDIATE) == settings) //opcode defaults to 32 when immediate but 64 elsewhere 
            {
                return ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.DWORD;
            }
            else
            {
                return ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.QWORD;
            }
        }
    }

}
