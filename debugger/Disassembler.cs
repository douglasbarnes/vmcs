using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Registers;
namespace debugger
{
    static class Disassembler
    {/*
        public static ulong DisassemblerPointer = 0;
        public static string DisasRegister(ByteCode bcByteCode, RegisterCapacity _regcap)
        {
            switch (_regcap)
            {
                case RegisterCapacity.R:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return "RAX";
                        case ByteCode.B:
                            return "RBX";
                        case ByteCode.C:
                            return "RCX";
                        case ByteCode.D:
                            return "RDX";
                        case ByteCode.AH:
                            return "RSP";
                        case ByteCode.BH:
                            return "RBP";
                        case ByteCode.CH:
                            return "RSI";
                        case ByteCode.DH:
                            return "RDI";
                    }
                    break;
                case RegisterCapacity.E:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return "EAX";
                        case ByteCode.B:
                            return "EBX";
                        case ByteCode.C:
                            return "ECX";
                        case ByteCode.D:
                            return "EDX";
                        case ByteCode.AH:
                            return "ESP";
                        case ByteCode.BH:
                            return "EBP";
                        case ByteCode.CH:
                            return "ESI";
                        case ByteCode.DH:
                            return "EDI";
                    }
                    break;
                case RegisterCapacity.X:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return "AX";
                        case ByteCode.B:
                            return "BX";
                        case ByteCode.C:
                            return "CX";
                        case ByteCode.D:
                            return "DX";
                        case ByteCode.AH:
                            return "SP";
                        case ByteCode.BH:
                            return "BP";
                        case ByteCode.CH:
                            return "SI";
                        case ByteCode.DH:
                            return "DI";
                    }
                    break;
                case RegisterCapacity.B:
                    switch (bcByteCode)
                    {
                        case ByteCode.A:
                            return "AL";
                        case ByteCode.B:
                            return "BL";
                        case ByteCode.C:
                            return "CL";
                        case ByteCode.D:
                            return "DL";
                        case ByteCode.AH:
                            return "AH";
                        case ByteCode.BH:
                            return "BH";
                        case ByteCode.CH:
                            return "CH";
                        case ByteCode.DH:
                            return "DH";
                    }
                    break;
            }
            throw new Exception();
        }
        public static string[] DisassembleMemory(MemorySpace msToDisassemble)
        {
            DisassemblerPointer = msToDisassemble.EntryPoint;
            for (ulong IP = msToDisassemble.EntryPoint; IP < msToDisassemble.LastAddr; IP++)
            {
                
                


            }
        }

        public static string DisassembleLine(byte[] baInputBytes)
        {

            StringBuilder sbLine = new StringBuilder();
            int i = 0;
            while(OpcodeLookup.PrefixTable.ContainsKey(baInputBytes[i]))
            {
                sbLine.Append(OpcodeLookup.DisasTable[baInputBytes[i]] + " ");
                i++;
            }
            sbLine.Append(OpcodeLookup.DisasTable[baInputBytes[i]] + " ");
            
            
        }

        private static string _disasByte(byte bInputByte)
        {
            
        }*/
    }
}
