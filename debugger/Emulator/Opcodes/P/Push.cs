﻿using System;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Push : Opcode
    {
        byte[] Result;
        //push imm8 is valid but not push r8                // no 8/32 bit mode for reg push  
        public Push(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("PUSH", input, FindRegCap(input), settings)
        {
            Result = Fetch()[0];
            if (Result.Length == 1 || Result.Length == 4) // imms of length b(http://prntscr.com/outmtv),dw(http://prntscr.com/ouyxwd) are sxted to 8. manual suggests words are too but they aren't
            {
                Result = Bitwise.SignExtend(Result, 8);
            }                    
        }
        public override void Execute()
        {
            byte[] NewSP;
            Bitwise.Subtract(ControlUnit.FetchRegister(XRegCode.SP, RegisterCapacity.GP_QWORD), new byte[] { (byte)Result.Length, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            ControlUnit.SetRegister(XRegCode.SP, NewSP);
            ControlUnit.SetMemory(BitConverter.ToUInt64(ControlUnit.FetchRegister(XRegCode.SP, RegisterCapacity.GP_QWORD),0), Result); 
        }

        private static RegisterCapacity FindRegCap(DecodedTypes.IMyDecoded input)
        {
            if(input.GetType() == typeof(DecodedTypes.Immediate)) //opcode defaults to 32 when immediate but 64 elsewhere 
            {
                return ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.GP_WORD : RegisterCapacity.GP_DWORD;
            }
            else
            {
                return ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.GP_WORD : RegisterCapacity.GP_QWORD;
            }
        }
    }

}