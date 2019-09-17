using System.Collections.Generic;
using debugger.Util;
using debugger.Logging;
namespace debugger.Emulator
{
    public enum RegisterHandleSettings
    {
        NONE=0,
        NO_REX =1,
        NO_INIT=2,
    }
    public static partial class ControlUnit
    {        
        public class RegisterHandle : DecodedTypes.IMyDecoded
        {
            protected readonly static List<string> GPMnemonics = new List<string> { "A", "C", "D", "B", "SP", "BP", "SI", "DI", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15" };
            protected readonly static List<string> MMXMnemonics = new List<string>() { "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", "MM0", "MM1", "MM2", "MM3", "MM4", "MM5", "MM6", "MM7", };
            public XRegCode Code { get; private set; }
            public RegisterTable Table { get; private set; }
            public RegisterCapacity Size { get; set; } 
            private readonly RegisterHandleSettings Settings;
            public void Initialise(RegisterCapacity size)
            {
                if((Settings | RegisterHandleSettings.NO_INIT) != Settings)
                {
                    Size = size;
                }                
            }
            public RegisterHandle(string mnemonic)
            {
                char Prefix = mnemonic[0];
                char Suffix = mnemonic[mnemonic.Length - 1];

                if (Prefix == 'R' && Suffix != 'X') // get r8w,r9d,r10b etc
                {
                    Table = RegisterTable.GP;
                    Size = Suffix switch
                    {
                        'D' => RegisterCapacity.DWORD,
                        'W' => RegisterCapacity.WORD,
                        'B' => RegisterCapacity.BYTE,
                        _ => RegisterCapacity.QWORD,
                    };
                    Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                }
                else if (Suffix == 'H' || Suffix == 'L')
                {
                    Size = RegisterCapacity.BYTE;
                    Table = RegisterTable.GP;
                    Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                }
                else {
                    switch (Prefix)
                    {
                        case 'E':
                            Size = RegisterCapacity.DWORD;
                            Table = RegisterTable.GP;
                            Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                            break;
                        case 'R':
                            Size = RegisterCapacity.QWORD;
                            Table = RegisterTable.GP;
                            Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                            break;
                        case 'M':
                            Size = RegisterCapacity.QWORD;
                            Table = RegisterTable.MMX;
                            Code = (XRegCode)int.Parse(Suffix.ToString());
                            break;
                        case 'X':
                            Size = RegisterCapacity.M128;
                            Table = RegisterTable.SSE;
                            Code = (XRegCode)int.Parse(Suffix.ToString());
                            break;
                        case 'Y':
                            Size = RegisterCapacity.M256;
                            Table = RegisterTable.SSE;
                            Code = (XRegCode)int.Parse(Suffix.ToString());
                            break;
                        default:
                            Size = RegisterCapacity.WORD;
                            Table = RegisterTable.GP;
                            Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                            break;
                    }
                }
                
            }
            public RegisterHandle(XRegCode registerCode, RegisterTable table, RegisterCapacity size=RegisterCapacity.NONE, RegisterHandleSettings settings=RegisterHandleSettings.NONE)
            {
                Code = registerCode;
                Table = table;
                Settings = settings;
                if(size != RegisterCapacity.NONE)
                {
                    Size = size;
                }
                else if ((Settings | RegisterHandleSettings.NO_INIT) == Settings)
                {
                    throw new System.Exception("Must be a possibility of register size initialising.");
                }
            }
            public List<byte[]> Fetch() => new List<byte[]> { FetchOnce() };
            public byte[] FetchOnce()
            {
                byte[] Output;
                if (Size == RegisterCapacity.BYTE
                && Code > XRegCode.B
                && (RexByte == REX.NONE || (Settings | RegisterHandleSettings.NO_REX) == Settings))
                {
                    Output = Bitwise.Subarray(CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4], 1);
                }
                else
                {
                    Output = CurrentContext.Registers[Table, Size, Code];
                }
                return Output;
            }
            public void Set(byte[] data)
            {
                if ((int)Size != data.Length)
                {
                    System.Array.Resize(ref data, (int)Size);
                    //This is completely normal most of the time, e.g instructions like MUL which produce a result size greater than that of the destination
                    //throw new LoggedException(LogCode.REGISTER_BADLEN, "");
                }
                if (Table == RegisterTable.GP
                    && data.Length == (int)RegisterCapacity.BYTE
                    && (int)Code > 3 && (RexByte == REX.NONE || (Settings | RegisterHandleSettings.NO_REX) != Settings)) // setting higher bit of gp word reg
                { // e.g AH has the same integer value as SP(SP has no higher bit register) so when 0b101 is accessed with byte width we need to sub 4 to get the normal reg code for that reg then set higher bit ourselves 
                    CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4] = new byte[] { CurrentContext.Registers[Table, RegisterCapacity.WORD, Code - 4][0], data[0] };
                }
                else
                {
                    if (data.Length == 4) { data = Bitwise.ZeroExtend(data, 8); }
                    CurrentContext.Registers[Table, (RegisterCapacity)data.Length, Code] = data;
                }
            }
            public List<string> Disassemble() => new List<string> { DisassembleOnce() };
            public string DisassembleOnce()
            {
                REX RexByte = ControlUnit.RexByte;
                string Output;
                if (Table == RegisterTable.GP)
                {
                    if (Size == RegisterCapacity.BYTE && RexByte == REX.NONE && Code > XRegCode.B && Code < XRegCode.R8)
                    {
                        Output = GPMnemonics[(int)Code - 4] + "H";
                    }
                    else
                    {
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
                }
                else
                {
                    Output = MMXMnemonics[(int)Code];
                    if (Size == RegisterCapacity.M128)
                    {
                        Output = Output.Insert(0, "X").Remove(1, 1);
                    }
                    else if (Size == RegisterCapacity.M256)
                    {
                        Output = Output.Insert(0, "Y").Remove(1, 1);
                    }
                }
                return Output;
            }
            public void SetLower(byte[] data)
            {
                System.Array.Resize(ref data, (int)Size / 2);
                System.Array.Copy(data, Fetch()[0], (int)Size / 2);
                Set(data);                
            }
            public void SetUpper(byte[] data)
            {
                System.Array.Resize(ref data, (int)Size / 2);
                System.Array.Copy(data, 0, Fetch()[0], ((int)Size/2)-1,(int)Size / 2);
                Set(data);
            }
            public byte[] FetchUpper() => Bitwise.Subarray(Fetch()[0], (int)Size / 2);
            public byte[] FetchLower() => CurrentContext.Registers[Table, (RegisterCapacity)((int)Size/2), Code];
        } 
    }
}
