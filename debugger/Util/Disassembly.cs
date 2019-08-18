using System.Collections.Generic;
using debugger.Emulator;
using debugger.Emulator.Opcodes;
namespace debugger.Util
{
    public static class Disassembly
    {
        public static List<Dictionary<RegisterCapacity, string>> RegisterMnemonics = new List<Dictionary<RegisterCapacity, string>>
            {
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RAX" }, { RegisterCapacity.DWORD, "EAX" },{ RegisterCapacity.WORD, "AX" },{ RegisterCapacity.BYTE, "AL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RCX" }, { RegisterCapacity.DWORD, "ECX" },{ RegisterCapacity.WORD, "CX" },{ RegisterCapacity.BYTE, "CL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RDX" }, { RegisterCapacity.DWORD, "EDX" },{ RegisterCapacity.WORD, "DX" },{ RegisterCapacity.BYTE, "DL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RBX" }, { RegisterCapacity.DWORD, "EBX" },{ RegisterCapacity.WORD, "BX" },{ RegisterCapacity.BYTE, "BL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RSP" }, { RegisterCapacity.DWORD, "ESP" },{ RegisterCapacity.WORD, "SP" },{ RegisterCapacity.BYTE, "AH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RBP" }, { RegisterCapacity.DWORD, "EBP" },{ RegisterCapacity.WORD, "BP" },{ RegisterCapacity.BYTE, "CH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RSI" }, { RegisterCapacity.DWORD, "ESI" },{ RegisterCapacity.WORD, "SI" },{ RegisterCapacity.BYTE, "DH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RDI" }, { RegisterCapacity.DWORD, "EDI" },{ RegisterCapacity.WORD, "DI" },{ RegisterCapacity.BYTE, "BH" }}
            };
        public static Dictionary<RegisterCapacity, string> SizeMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
            {
                {RegisterCapacity.BYTE, "BYTE"},
                {RegisterCapacity.WORD, "WORD"},
                {RegisterCapacity.DWORD, "DWORD"},
                {RegisterCapacity.QWORD, "QWORD"}
            };
        private static Dictionary<Condition, string> ConditionMnemonics = new Dictionary<Condition, string>()
        {
            { Condition.A, "A" },
            { Condition.NA, "NA" },
            { Condition.C, "C" },
            { Condition.NC, "NC" },            
            { Condition.RCXZ, "RCXZ" },
            { Condition.Z, "Z" },
            { Condition.NZ, "NZ" },
            { Condition.G, "G" },
            { Condition.GE, "GE" },
            { Condition.L, "L" },
            { Condition.LE, "LE" },
            { Condition.O, "O" },
            { Condition.NO, "NO" },
            { Condition.S, "S" },
            { Condition.NS, "NS" },
            { Condition.P, "P" },
            { Condition.NP, "NP" }
        };
        public static string DisassembleCondition(Condition condition) => ConditionMnemonics[condition];
        public static string DisassembleRegister(ByteCode Register, RegisterCapacity RegCap)
        {
            return RegisterMnemonics[(byte)Register][RegCap];
        }
        public struct Pointer
        {
            public long Offset;
            public string BaseReg;
            public string AdditionalReg;
            public int Coefficient;
            public RegisterCapacity? Size;
        }
        public static string DisassemblePointer(Pointer p)
        {
            string Output = "";
            if(p.BaseReg != null)
            {
                Output += p.BaseReg;
                if (p.Coefficient > 1)
                {
                    Output += $"*{p.Coefficient}";
                }
                if(p.AdditionalReg != null)
                {
                    Output += '+';
                }
            }            
            if (p.AdditionalReg != null)
            {
                Output += $"{p.AdditionalReg}";
            }
            if (p.Offset != 0)
            {
                if (p.BaseReg != null || p.AdditionalReg != null)
                {
                    Output += p.Offset > 0 ? "+" : "-";
                }
                Output += $"0x{System.Math.Abs(p.Offset).ToString("X")}";
            }
            if (p.Size != null)
            {
                Output = $"{SizeMnemonics[p.Size.Value]} PTR [{Output}]";
            }
            return Output;
        }

    }

}

