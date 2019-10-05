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
      public static string DisassembleCondition(Condition condition) => ConditionMnemonics[condition];
        public struct DisassembledPointer
        {
            public long Offset;
            public string IndexReg;
            public string AdditionalReg;
            public int IndexScale;
            public RegisterCapacity? Size;
        }
        public static string DisassemblePointer(DisassembledPointer p)
        {
            string Output = "";
            if(p.IndexReg != null)
            {
                Output += p.IndexReg;
                if (p.IndexScale > 0)
                {
                    Output += $"*{(int)System.Math.Pow(2, p.IndexScale)}";
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
                if (p.IndexReg != null || p.AdditionalReg != null)
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

