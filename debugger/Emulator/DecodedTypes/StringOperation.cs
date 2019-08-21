using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    public class StringOperation : IMyDecoded
    {
        private ulong SrcPtr;
        private ulong DestPtr;
        private RegisterCapacity PtrSize; // for in the rare rare rare case that esi/edi could carry out
        public StringOperation()
        {
            Initialise(ControlUnit.PrefixBuffer.Contains(PrefixByte.ADDR32) ? RegisterCapacity.GP_DWORD : RegisterCapacity.GP_QWORD);
        }
        public void Initialise(RegisterCapacity size)
        {
            PtrSize = size;
            SrcPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), 8), 0);
            DestPtr = BitConverter.ToUInt64(Bitwise.ZeroExtend(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), 8), 0);
        }
        public List<string> Disassemble(RegisterCapacity size) 
            => new List<string> { $"[{Disassembly.DisassembleRegister(XRegCode.DI, size, REX.NONE)}]", $"[{Disassembly.DisassembleRegister(XRegCode.SI, size, REX.NONE)}]" };
        public List<byte[]> Fetch(RegisterCapacity length)
        {
            byte[] NewSI;
            byte[] NewDI;
            byte[] Constant = new byte[] { (byte)length };
            if (ControlUnit.Flags.Direction == FlagState.ON)
            {
                Bitwise.Add(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), Constant, (int)length, out NewSI);
                Bitwise.Add(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), Constant, (int)length, out NewDI);
            }
            else
            {
                Bitwise.Subtract(ControlUnit.FetchRegister(XRegCode.SI, PtrSize), Constant, (int)length, out NewSI);
                Bitwise.Subtract(ControlUnit.FetchRegister(XRegCode.DI, PtrSize), Constant, (int)length, out NewDI);
            }
            ControlUnit.SetRegister(XRegCode.SI, NewSI);
            ControlUnit.SetRegister(XRegCode.DI, NewDI);
            return new List<byte[]> { ControlUnit.Fetch(DestPtr, (int)length), ControlUnit.Fetch(SrcPtr, (int)length) };
        }        
        public void Set(byte[] data) => ControlUnit.SetMemory(DestPtr, data);
        public void SetSource(byte[] data) => ControlUnit.SetMemory(SrcPtr, data);
    }
}
