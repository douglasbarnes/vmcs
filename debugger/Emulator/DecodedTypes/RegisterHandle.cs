using System.Collections.Generic;
using debugger.Util;
using debugger.Logging;
namespace debugger.Emulator
{
    public partial class ControlUnit
    {
        public class RegisterHandle : DecodedTypes.IMyDecoded
        {
            private readonly static List<string> GPMnemonics = new List<string> { "A", "C", "D", "B", "SP", "BP", "SI", "DI", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15" };
            private readonly static List<string> SSEMnemonics = new List<string>() { "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", };
            public XRegCode Code { get; private set; }
            public RegisterTable Table { get; private set; }
            public RegisterCapacity Size { get; set; }
            public bool OverrideRex;
            public void Initialise(RegisterCapacity size)
            {
                Size = size;
            }
            public RegisterHandle(XRegCode registerCode, RegisterTable table, RegisterCapacity size)
            {
                Code = registerCode;
                Table = table;
                OverrideRex = false;
                Size = size;
            }
            public RegisterHandle(XRegCode registerCode, RegisterTable table)
            {
                Code = registerCode;
                Table = table;
                OverrideRex = false;
            }
            public List<byte[]> Fetch()
            {                
                byte[] Output;
                if (Size == RegisterCapacity.BYTE
                && Code > XRegCode.B
                && (RexByte == REX.NONE || OverrideRex))
                {
                    Output = Bitwise.Subarray(CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4], 1);
                }
                else
                {
                    Output = CurrentContext.Registers[Table, Size, Code];
                }
                return new List<byte[]> { Output };                
            }
            public void Set(byte[] data)
            {
                if ((int)Size != data.Length)
                {
                    throw new LoggedException(LogCode.REGISTER_BADLEN, "");
                }
                if (Table == RegisterTable.GP
                    && data.Length == (int)RegisterCapacity.BYTE
                    && (int)Code > 3 && (RexByte == REX.NONE || OverrideRex)) // setting higher bit of gp word reg
                { // e.g AH has the same integer value as SP(SP has no higher bit register) so when 0b101 is accessed with byte width we need to sub 4 to get the normal reg code for that reg then set higher bit ourselves 
                    CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4] = new byte[] { CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4][0], data[0] };
                }
                else
                {
                    if (data.Length == 4) { data = Bitwise.ZeroExtend(data, 8); }
                    CurrentContext.Registers[Table, (RegisterCapacity)data.Length, Code] = data;
                }
            }
            public List<string> Disassemble()
            {
                REX RexByte = ControlUnit.RexByte;
                string Output;
                if (Table == RegisterTable.GP)
                {
                    if (Size == RegisterCapacity.BYTE && RexByte == REX.NONE && Code > XRegCode.B && Code < XRegCode.R8)
                    {
                        return GPMnemonics[(int)Code - 4] + "H";
                    }
                    Output = GPMnemonics[(int)Code];
                    if (Code <= XRegCode.B && Size > RegisterCapacity.BYTE)
                    {
                        Output += "X";
                    }
                    if (Size == RegisterCapacity.BYTE)
                    {
                        Output += "L";
                    }
                    else if (Size == RegisterCapacity.WORD
                         && Code > XRegCode.DI)
                    {
                        Output += "W";
                    }
                    else if (Size == RegisterCapacity.DWORD)
                    {
                        if (Code <= XRegCode.DI)
                        {
                            Output = $"E{Output}";
                        }
                        else
                        {
                            Output += "D";
                        }
                    }
                    else if (Size == RegisterCapacity.QWORD
                         && Code <= XRegCode.DI)
                    {
                        Output = $"R{Output}";
                    }
                }
                else
                {
                    Output = SSEMnemonics[(int)Code];
                    if (Size == RegisterCapacity.M128)
                    {
                        Output = $"X{Output}";
                    }
                    else if (Size == RegisterCapacity.M256)
                    {
                        Output = $"Y{Output}";
                    }
                }
                return new List<>() { Output };
            }
        }
    }
}
