using System.Collections.Generic;
using debugger.Emulator;
using debugger.Emulator.Opcodes;
namespace debugger.Util
{
    public static class Disassembly
    {
        private readonly static Dictionary<RegisterCapacity, string> SizeMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
            {
                {RegisterCapacity.GP_BYTE, "BYTE"},
                {RegisterCapacity.GP_WORD, "WORD"},
                {RegisterCapacity.GP_DWORD, "DWORD"},
                {RegisterCapacity.GP_QWORD, "QWORD"}
            };
        private readonly static Dictionary<Condition, string> ConditionMnemonics = new Dictionary<Condition, string>()
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
        public readonly static List<string> RegisterMnemonics = new List<string>
        {
            "A","C","D","B","SP","BP","SI","DI","R8","R9","R10","R11","R12","R13","R14","R15"
        };
        public static string DisassembleCondition(Condition condition) => ConditionMnemonics[condition];
        public static string DisassembleRegister(XRegCode Register, RegisterCapacity RegCap, REX RexByte)
        {            
            if(RegCap == RegisterCapacity.GP_BYTE && RexByte != REX.NONE && Register - 4 > 0)
            {
                return RegisterMnemonics[(int)Register-4] + "H";
            }
            string Output = RegisterMnemonics[(int)Register];
            if (Register <= XRegCode.B && RegCap > RegisterCapacity.GP_BYTE) 
            {
                Output = $"{Output}X";
            }
            switch (RegCap)
            {
                case RegisterCapacity.GP_BYTE:                
                    Output += "L";
                    break;
                case RegisterCapacity.GP_WORD:
                    if(Register > XRegCode.DI && Register <= XRegCode.R15)
                    {
                        Output += "W";
                    }
                    break;
                case RegisterCapacity.GP_DWORD:
                    if (Register <= XRegCode.DI)
                    {
                        Output = $"E{Output}";
                    }
                    else
                    {
                        Output += "D";
                    }
                    break;
                case RegisterCapacity.GP_QWORD:
                    
                    if(Register <= XRegCode.DI)
                    {
                        Output = $"R{Output}";
                    }
                    break;

            }
            return Output;
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
            return Output;
        }
        public static string DisassembleSize(RegisterCapacity size) => SizeMnemonics[size];
    }

}

