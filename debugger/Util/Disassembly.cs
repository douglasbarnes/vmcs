using System.Collections.Generic;
using debugger.Emulator;
using debugger.Emulator.Opcodes;
namespace debugger.Util
{
    public static class Disassembly
    {
        private readonly static Dictionary<RegisterCapacity, string> SizeMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
        {
            {RegisterCapacity.BYTE, "BYTE"},
            {RegisterCapacity.WORD, "WORD"},
            {RegisterCapacity.DWORD, "DWORD"},
            {RegisterCapacity.QWORD, "QWORD"}
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
        private readonly static List<string> GPMnemonics = new List<string> { "A","C","D","B","SP","BP","SI","DI","R8","R9","R10","R11","R12","R13","R14","R15" };
        private readonly static List<string> SSEMnemonics = new List<string>() { "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", };
        public static string DisassembleCondition(Condition condition) => ConditionMnemonics[condition];
        public static string DisassembleRegister(ControlUnit.RegisterHandle register)
        {
            REX RexByte = ControlUnit.RexByte;
            string Output;
            if(register.Table == RegisterTable.GP)
            {                
                if (register.Size == RegisterCapacity.BYTE && RexByte == REX.NONE && register.Code > XRegCode.B && register.Code < XRegCode.R8)
                {
                    return GPMnemonics[(int)register.Code - 4] + "H";
                }
                Output = GPMnemonics[(int)register.Code];
                if (register.Code <= XRegCode.B && register.Size > RegisterCapacity.BYTE)
                {
                    Output += "X";
                }
                if(register.Size == RegisterCapacity.BYTE)
                {
                    Output += "L";
                }
                else if(register.Size == RegisterCapacity.WORD
                     && register.Code > XRegCode.DI)
                {
                    Output += "W";
                }
                else if(register.Size == RegisterCapacity.DWORD)
                {
                    if (register.Code <= XRegCode.DI)
                    {
                        Output = $"E{Output}";
                    }
                    else
                    {
                        Output += "D";
                    }
                }
                else if(register.Size == RegisterCapacity.QWORD
                     && register.Code <= XRegCode.DI)
                {
                    Output = $"R{Output}";
                }
            }
            else
            {
                Output = SSEMnemonics[(int)register.Code];
                if(register.Size == RegisterCapacity.M128)
                {
                    Output = $"X{Output}";
                }
                else if (register.Size == RegisterCapacity.M256)
                {
                    Output = $"Y{Output}";
                }
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
                if (p.Coefficient > 0)
                {
                    Output += $"*{(int)System.Math.Pow(2, p.Coefficient)}";
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

