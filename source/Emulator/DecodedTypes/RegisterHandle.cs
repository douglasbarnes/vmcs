// A register handle is a unique type of IMyDecoded. It is a part of the ControlUnit class and therefore has direct access to registers. So much so that currently the only way to access a
// register from outside of the ControlUnit class is by a RegisterHandle. A RegisterHandle creates access to a register with minimum input data required to do so. It holds static members
// responsible for the disassembly of every register instance throughout the source. 
// There are two different settings associated with the handle,
//  NO_REX  - The handle ignores the current rex byte set in the ControlUnit when infering a register from the inputs. This means AH,CH,DH,BH will always be favoured instead of SPL,BPL,SIL,DIL
//            However, regardless of this setting, if XRegCode specified in the constructor is R8,R9..,R15, that register will still be used. See the explanation of REX bytes in ControlUnit.cs if unsure.
//  NO_INIT - Once constructed, the RegisterHandle will ignore all Initialise() calls. This is useful when the opcode always takes a specific register as an input, such as shift instructions which have 
//            an implementation where the source is implicitly defined as $CL. If the NO_INIT setting did not exist, this would be overwritten to the same size register as the input.
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator
{
    public enum RegisterHandleSettings
    {
        NONE = 0,
        NO_REX = 1,
        NO_INIT = 2,
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
                // If the register has the NO_INIT bit set, it will never initialise after the constructor.
                if ((Settings | RegisterHandleSettings.NO_INIT) != Settings)
                {
                    Size = size;
                }
            }
            public RegisterHandle(string mnemonic)
            {
                // A constructor for creating a register handle from a disassembled mnemonic.
                // No validation is used here. In the case of invalid data that can not be parsed,
                // it would be better to let the error throw than to try fix it inside the function.
                // In other words, invalid data cannot be trusted at all.

                // The size of a register handle can be determined from its prefix and suffix
                // For example,
                // E?X = DWORD
                // R?X = QWORD
                // R?# = QWORD(where # = a number)                
                char Prefix = mnemonic[0];
                char Suffix = mnemonic[mnemonic.Length - 1];

                // If $mnemomnic begins with "R", it could either any 64 bit GP register.
                // To narrow this down to R8,R9..,R15, check if it has an "X" at the end,
                // if it doesn't it has to be disassembled a little differently(in the if block)
                if (Prefix == 'R' && Suffix != 'X') // get r8w,r9d,r10b etc
                {
                    // Every mnemonic that satisfies R?# is in the GP block
                    Table = RegisterTable.GP;
                    // R8-R15 have a suffix that indicates their size.
                    // For example,
                    // R8D = DWORD
                    // R11W = WORD
                    // R10B = BYTE
                    // R15 = QWORD
                    // This method is much more intuitive than other registers.
                    Size = Suffix switch
                    {
                        'D' => RegisterCapacity.DWORD,
                        'W' => RegisterCapacity.WORD,
                        'B' => RegisterCapacity.BYTE,
                    };

                    // If the suffix does not satisfy the above, it must be a number(out of exhaustion of valid possiblilities), meaning that
                    // it also has to be of QWORD size.
                    if (Suffix >= '0' && Suffix <= '9')
                    {
                        Size = RegisterCapacity.QWORD;

                        // As it is of QWORD size, the mnemonic has no trailing characters and so will match exactly into the dictionary.
                        Code = (XRegCode)GPMnemonics.IndexOf(mnemonic);
                    }
                    else
                    {
                        // If it isn't of QWORD size, it must have a trailing character which should be ommitted before matching into the dictionary.
                        Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                    }
                }

                // If the suffix is a "H" or "L", it is a byte register such as AL or CH 
                else if (Suffix == 'H' || Suffix == 'L')
                {
                    Size = RegisterCapacity.BYTE;
                    Table = RegisterTable.GP;
                    // For a ia-32 GP register, only the first 1-2 characters are be needed to determine the register,
                    // e.g "SP"L, "A"H
                    Code = (XRegCode)GPMnemonics.IndexOf(mnemonic.Substring(0, mnemonic.Length - 1));
                }
                else
                {
                    // If the previous two conditions were not met, a general approach can be taken.
                    // If the prefix is E, it must be a 32 bit GP register e.g EAX EBP
                    // If the prefix is R, it must be a 64 bit GP register e.g RCX RDX(Although 64 bit GP registers, the possibility of R8-R15 was eliminated earlier because their format is different.
                    // If the prefix is M, it must be a 64 bit MMX register i.e MM0-MM7
                    // If the prefix is X, it must be the lower 128 bits of a YMM register i.e XMM0-XMM7
                    // If the prefix is Y, it must be a 256 bit SSE register i.e YMM0-7
                    // Otherwise is a GP WORD register, e.g AX, BX, SP. There is not really a pattern for such.

                    // The XRegCode of a MMX/SSE register can be determined by parsing the suffix as an integer.
                    // E.g XMM4 -> '4' -> (XRegCode)4

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
            public RegisterHandle(XRegCode registerCode, RegisterTable table, RegisterCapacity size = RegisterCapacity.NONE, RegisterHandleSettings settings = RegisterHandleSettings.NONE)
            {
                // A constructor for infering a register from a set of arguments.
                // Strictly this isnt done here, rather in the Fetch() and Set() methods
                Code = registerCode;
                Table = table;
                Settings = settings;

                // If the input register capacity is NONE(or there wasn't one passed in the arguments), do not initialise the size.
                // This means that attempting to use the register handle before it is initialised will consistently throw the same exception.
                if (size != RegisterCapacity.NONE)
                {
                    Size = size;
                }

                // If $size was NONE, and the register handle has NO_INIT set, there would never be a possibility of the register handle being used, so throw an exception now.
                // This rounds down the problem a little more than a "variable not initialised" exception or alike, as it would be a typo or improper class usage.
                else if ((Settings | RegisterHandleSettings.NO_INIT) == Settings)
                {
                    throw new System.Exception("Must be a possibility of register size initialising.");
                }
            }
            public List<byte[]> Fetch() => new List<byte[]> { FetchOnce() };
            public byte[] FetchOnce() => FetchAs(Size);
            public byte[] FetchAs(RegisterCapacity regCap, bool forceUpper=false)
            {
                // A method for fetching the value of the register as a single byte[] rather than a list<byte[]> as the interface requires.
                byte[] Output;

                // If it is an upper byte register, return the subarray at position 1 of the provided XRegCode - 4.
                // This isn't implemented into RegisterGroup, but it can be easily worked around.
                // The idea is:
                //  1. Fetch a WORD from the desired register
                //  2. Return only the upper byte from the fetched WORD
                // First the provided XRegCode must be translated to its WORD equivalent
                // This is because single byte registers are mapped like,
                // 0  1  2  3  4  5  6  7
                // AL CL BL DL AH CH DH BH
                // rather than the WORD registers which are mapped like,
                // 0  1  2  3  4  5  6  7
                // AX CX DX BX SP BP SI DI
                // So if CH(5) was inputted, 4 has to be subtracted from that
                // to get CX.
                // Strictly speaking, GP registers have 4 different tables, one for each size, but this is the only case where the table is
                // not identical for each, so a small workaround can go quite far.
                // Finally, since the program operates in little endian, the 1st index of the returned WORD byte array will be the upper byte.
                // Explanations for fetching from a RegisterGroup can be found in its class file.
                // In short, if this predicate is true, it should be accessed as an upper byte register.
                if (forceUpper || (regCap == RegisterCapacity.BYTE
                && Code > XRegCode.B
                && Table == RegisterTable.GP
                && (RexByte == REX.NONE || (Settings | RegisterHandleSettings.NO_REX) != Settings)))
                {
                    Output = new byte[] { CurrentContext.Registers.GetUpperByte(Code - 4) };
                }
                else
                {
                    Output = CurrentContext.Registers[Table, regCap, Code];
                }
                return Output;
            }
            public void Set(byte[] data)
            {
                // A method for setting the value of a register pointed to by the register handle. 
                //This is completely normal most of the time, e.g instructions like MUL which produce a result size greater than that of the destination
                if ((int)Size != data.Length)
                {
                    System.Array.Resize(ref data, (int)Size);
                }

                // Setting an upper byte register
                // See full explanations for the following in FetchOnce()
                if (data.Length == (int)RegisterCapacity.BYTE
                    && Code > XRegCode.B
                    && Table == RegisterTable.GP
                    && (RexByte == REX.NONE || (Settings | RegisterHandleSettings.NO_REX) == Settings))
                {
                    // e.g AH has the same integer value as SP(SP has no higher bit register reference) so when 0b101 is accessed with byte width you need to sub 4 to get the 
                    // normal reg code for that reg then set higher bit ourselves .
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
                // Dissassemble the register pointed to be this register handle and return the result as a string.
                // Essentially the inverse of when a register handle is initialised from a mnemonic, so cross reference with the constructor if anything is unclear. 
                // I will repeat my self and explain fully in both places.

                string Output;

                // If the register handle points to a GP register, disassembly is a little less straightforward.
                if (Table == RegisterTable.GP)
                {

                    // This is explained in FetchNext(). Essentially, if the register is an upper byte register such as AH, it will satisfy following conditions.
                    // If this is the case, the disassembly is simple.
                    if (Size == RegisterCapacity.BYTE && ControlUnit.RexByte == REX.NONE && Code > XRegCode.B && Code < XRegCode.R8)
                    {
                        Output = GPMnemonics[(int)Code - 4] + "H";
                    }
                    else
                    {
                        Output = GPMnemonics[(int)Code];

                        // Some registers follow this rule, some do not. To be precise, one quarter do.
                        // Registers A,C,D,B all have an "X" appended when their capacity is greater than that of a BYTE.
                        // Registers SP,BP,SI,DI just stay as their mnemonic. This is likely because in IA-32, SPL BPL SIL and DIL
                        // did not exist.
                        // Registers R8 through to R15 have a "W" appended instead. Presumably short for "WORD"
                        // Some examples,
                        // WORD A register = AX
                        // WORD C register = CX
                        // WORD R10 register = R10W
                        // WORD SP register = SP
                        if (Code <= XRegCode.B && Size > RegisterCapacity.BYTE)
                        {
                            Output += "X";
                        }


                        // If the register capacity is that of a BYTE, the possibility of it being an upper register was already handled, so it must be a
                        // lower byte and therefore end in "L"(true for all cases)
                        if (Size == RegisterCapacity.BYTE)
                        {
                            Output += "L";
                        }
                        else if (Size == RegisterCapacity.WORD
                             && Code > XRegCode.DI)
                        {
                            Output += "W";
                        }

                        // If the register existed in IA-32(EAX,ECX,ESP,EDI etc) it will start with an "E" for "Extended". In the newer IA-64 registers R8-R15, it will end with a "D"
                        // I assume this is because of historical reasons as mnemonics were getting a little ridiculous as registers continued to enlarge.
                        // BX = B eXtended
                        // EBX = Extended B eXtended
                        // RBX = Register Extended B eXtended(there is no official source of R standing for register, but it seems implied)
                        else if (Size == RegisterCapacity.DWORD)
                        {
                            // XRegCodes greater than DI must be R8,R9 etc
                            if (Code <= XRegCode.DI)
                            {
                                Output = $"E{Output}";
                            }
                            else
                            {
                                Output += "D";
                            }
                        }

                        // In any other case, it must be a 64 bit register, so prefix it with an R
                        // An exception would be the registers introduced in IA-64 such as R8,R9 because I already prefixed them in the GPMnemonics dictionary.
                        else if (Size == RegisterCapacity.QWORD
                             && Code <= XRegCode.DI)
                        {
                            Output = $"R{Output}";
                        }
                    }
                }
                else
                {
                    // If the register is not a GP register, it can be disassembled following a simple patter.

                    // This will return MM0,MM1, MM2 etc based on $Code
                    Output = MMXMnemonics[(int)Code];

                    // If it is a XMM or YMM register, insert the prefix accordingly..
                    if (Size == RegisterCapacity.M128)
                    {
                        Output = Output.Insert(0, "X");
                    }
                    else if (Size == RegisterCapacity.M256)
                    {
                        Output = Output.Insert(0, "Y");
                    }
                }
                return Output;
            }
            public void SetLower(byte[] data)
            {
                // Make sure $data is the size of the entire register.
                System.Array.Resize(ref data, (int)Size);

                // Copy the already existing upper bytes into $data into the upper half(because only the lower bytes are changed by the input $data)
                System.Array.Copy(FetchOnce(), 0, data, (int)Size / 2, (int)Size / 2);

                // Set the data with the modified lower bytes
                Set(data);
            }
            public void SetUpper(byte[] data)
            {
                byte[] Buffer = new byte[(int)Size];

                // Copy $data into the upper half of the buffer(because only the upper bytes are changed by the input $data)
                // Copy either $data.Length bytes or (int)$Size/2 bytes, whichever is smaller(to prevent overflow)
                System.Array.Copy(data, 0, Buffer, 0, (data.Length < (int)Size / 2) ? data.Length : (int)Size / 2);

                // Copy the existing lower bytes into the buffer
                System.Array.Copy(Fetch()[0], 0, Buffer, ((int)Size / 2) - 1, (int)Size / 2);

                Set(Buffer);
            }
            public byte[] FetchUpper() => Bitwise.Subarray(FetchOnce(), (int)Size / 2); // Cut all but the upper half of FetchOnce()
            public byte[] FetchLower() => CurrentContext.Registers[Table, (RegisterCapacity)((int)Size / 2), Code]; // Fetch the register half the size of the current (Will throw an exception if $Size == BYTE, if this is happening I definitely want to know)
        }
    }
}
