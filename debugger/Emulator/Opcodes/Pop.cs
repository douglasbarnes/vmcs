﻿using System;

namespace debugger.Emulator.Opcodes
{    public class Pop : Opcode
    {
        byte[] StackBytes;                                // no 32 bit mode for reg pop, default it to 64
        public Pop(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) 
            : base("POP", input, (ControlUnit.PrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.WORD : RegisterCapacity.QWORD, settings)
        {
            StackBytes = ControlUnit.Fetch(BitConverter.ToUInt64(ControlUnit.FetchRegister(ByteCode.SP, RegisterCapacity.QWORD), 0), (int)Capacity);
        }
        public override void Execute()
        {
            Set(StackBytes); // pop 0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction so i just point it to the generic pop function  
        }
    }
}