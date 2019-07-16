using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.ControlUnit;
using static debugger.Primitives;
namespace debugger
{
    public class Primitives
    {
        public struct OpcodeInput
        {
            internal ModRM DecodedModRM;          
            internal bool Is8Bit;
            internal bool IsSwap;
            internal bool IsSigned;
            internal bool IsImmediate;
            internal bool IsSignExtendedByte;
        }
        public enum ByteCode
        {
            A = 0x00,
            C = 0x01,
            D = 0x02,
            B = 0x03,
            SP = 0x04,
            BP = 0x05,
            SI = 0x06,
            DI = 0x07,
            AH = 0x04,
            CH = 0x05,
            DH = 0x06,
            BH = 0x07
        }
        public enum PrefixByte
        {
            ADDR32 = 0x67,
            SIZEOVR = 0x66,
            REXW = 0x48
        }
        public enum RegisterCapacity
        {
            B = 1,
            X = 2,
            E = 4,
            R = 8
        }
        public struct ModRM
        {
            public ulong DestPtr;
            public ulong SourceReg;
            //for disas only
            public long Offset;
            public byte Mod; // first 2
           // public byte Reg; // next 3
            public byte RM; // last 3
            public SIB DecodedSIB;
        }
        public struct SIB
        {
            public RegisterCapacity PointerSize;
            public ulong ResultNoOffset;
            public byte ScaledIndex;
            public byte Base;
            public ulong ScaledIndexValue;
            public ulong BaseValue;
            public long OffsetValue; // a way to avoid this? when we have a sib where its [reg + ebp + offset], we cant disassemble that visibly without this because ebp could change giving the wrong offset displayed
            public byte Scale;
        }
    }
    public static class Util
    {
        public static bool IsNegative(this byte[] baInput)
        { // convert.tostring returns the smallest # bits it can, not to the closest 8, if it evenly divides into 8 it is negative
            return Convert.ToString(baInput[0], 2).Length % 8 == 0; //                     -128 64 32 16 8  4  2  1
             // twos compliment: negative number always has a greatest set bit of 1 .eg, 1  0  0  0  0  0  0  1 = -128+1 = -127
                // this way is much faster than using GetBits() because padleft iterates the whole string multiple times
              // this method is just for performance because its used alot
        }
        public static bool IsZero(this byte[] baInput)
        {
            for (int i = 0; i < baInput.Length; i++)
            {
                if (baInput[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsGreater(this byte[] baInput1, byte[] baInput2)
        {
            baInput1 = Bitwise.Trim(baInput1);
            baInput2 = Bitwise.Trim(baInput2);
            Bitwise.PadEqual(ref baInput1, ref baInput2); //  V check for signs
            return ((sbyte)baInput1[baInput1.Length - 1] > (sbyte)baInput2[baInput2.Length - 1]);
        }
        public static class Bitwise
        {
            public static byte[] Zero = Enumerable.Repeat((byte)0, 128).ToArray();
            public static byte[] Trim(byte[] baInput)
            {
                for (int i = 0; i < baInput.Length; i++)
                {
                    if(baInput[baInput.Length-i-1] != 0)
                    {
                        return baInput.Take(baInput.Length - i).ToArray(); // cut after first non zero
                    }
                }
                return baInput;
            }
            public static byte[] Cut(byte[] baInput, int Count) // time difference between this and linq.take is huge, 
            {                                                           // http://prntscr.com/od20o4 // linq: http://prntscr.com/od20vw  //my way: http://prntscr.com/od21br
                byte[] baBuffer = new byte[Count];
                for (int i = 0; i < baBuffer.Length; i++)
                {
                    baBuffer[i] = baInput[i];
                }
                return baBuffer;
            }
            public static byte[] Subarray(byte[] Input, int Offset)
            {
                byte[] Buffer = new byte[Input.Length];
                for (int i = Offset; i < Buffer.Length; i++)
                {
                    Buffer[i] = Input[i];
                }
                return Buffer;
            }
            public static void PadEqual(ref string s1, ref string s2)
            {
                if (s1.Length > s2.Length)
                {
                    s2 = SignExtend(s2, s1.Length);
                }
                else if (s2.Length > s1.Length)
                {
                    s1 = SignExtend(s1, s2.Length);
                }
            }
            public static void PadEqual(ref byte[] b1, ref byte[] b2)
            {
                if (b1.Length > b2.Length)
                {
                    b2 = SignExtend(b2, (byte)b1.Length);
                }
                else if (b2.Length > b1.Length)
                {
                    b1 = SignExtend(b1, (byte)b2.Length);
                }
            }
            public static byte[] Or(byte[] baInput1, byte[] baInput2)
            {
                string sBits1 = GetBits(baInput1);
                string sBits2 = GetBits(baInput2);
                PadEqual(ref sBits1, ref sBits2);

                string sOutput = "";
                for (int i = 0; i < sBits1.Length; i++)
                {
                    if (sBits1[i] == '1' || sBits2[i] == '1')
                    {
                        sOutput += "1";
                    }
                    else
                    {
                        sOutput += "0";
                    }
                }
                byte[] baResult = GetBytes(sOutput);
                
                return baResult;
            }
            public static byte[] And(byte[] baInput1, byte[] baInput2)
            {
                string sBits1 = GetBits(baInput1);
                string sBits2 = GetBits(baInput2);
                PadEqual(ref sBits1, ref sBits2);

                string sOutput = "";
                for (int i = 0; i < sBits1.Length; i++)
                {
                    if (sBits1[i] == '1' && sBits2[i] == '1')
                    {
                        sOutput += "1";
                    }
                    else
                    {
                        sOutput += "0";
                    }
                }
                byte[] baResult = GetBytes(sOutput);
                Opcode.SetFlags(baResult, Opcode.FlagMode.Logic, baInput1, baInput2);
                return baResult;
            }
            public static byte[] Xor(byte[] baInput1, byte[] baInput2)
            {
                string sBits1 = GetBits(baInput1);
                string sBits2 = GetBits(baInput2);
                PadEqual(ref sBits1, ref sBits2);

                string sOutput = "";
                for (int i = 0; i < sBits1.Length; i++)
                {
                    if (sBits1[i] == '1' ^ sBits2[i] == '1')
                    {
                        sOutput += "1";
                    }
                    else
                    {
                        sOutput += "0";
                    }
                }
                byte[] baResult = GetBytes(sOutput);
                Opcode.SetFlags(baResult, Opcode.FlagMode.Logic, baInput1, baInput2);
                return baResult;
            }
            public static byte[] Add(byte[] baInput1, byte[] baInput2, bool bCarry, int Size)
            {
                Array.Resize(ref baInput1, 8);
                Array.Resize(ref baInput2, 8);
                baInput1 = ZeroExtend(baInput1, 8);
                /// http://prntscr.com/odko8i really is by far the best way to do it
                byte[] baResult = BitConverter.GetBytes(
                         BitConverter.ToUInt64(baInput1, 0)
                       + BitConverter.ToUInt64(baInput2, 0)
                       + (ulong)(bCarry ? 1 : 0)
                    );
                return Cut(baResult, Size);
            }
            public static byte[] Subtract(byte[] baInput1, byte[] baInput2, bool bCarry, int Size)
            {
                Array.Resize(ref baInput1, 8);
                Array.Resize(ref baInput2, 8);
                byte[] baResult = BitConverter.GetBytes(
                        Convert.ToUInt64(
                         BitConverter.ToUInt64(baInput1, 0)
                       - BitConverter.ToUInt64(baInput2, 0)
                       + (ulong)(bCarry ? 1 : 0) //add borrow
                    ));
                
                return Cut(baResult, Size);
            }
            public static byte[] Divide(byte[] baInput1, byte[] baInput2, bool Signed, int Size)
            {
                //divide dont set flags
                baInput1 = (Signed) ? SignExtend(baInput1, 8) : ZeroExtend(baInput1, 8);
                baInput2 = (Signed) ? SignExtend(baInput2, 8) : ZeroExtend(baInput2, 8);
                byte[] baResult = BitConverter.GetBytes( // integer divison, never have to handle floats
                         BitConverter.ToUInt64(baInput1, 0)
                       / BitConverter.ToUInt64(baInput2, 0)                      
                );
                return Cut(baResult, Size);
            }
            public static byte[] Multiply(byte[] baInput1, byte[] baInput2, bool Signed, int Size)
            {
                baInput1 = (Signed) ? SignExtend(baInput1, 8) : ZeroExtend(baInput1, 8);
                baInput2 = (Signed) ? SignExtend(baInput2, 8) : ZeroExtend(baInput2, 8);
                byte[] Result = BitConverter.GetBytes(
                         BitConverter.ToUInt64(baInput1, 0)
                       * BitConverter.ToUInt64(baInput2, 0)
                    );
                //set flags                            
                return Cut(Result, Size*2);
            }
            public static byte[] Modulo(byte[] baInput1, byte[] baInput2, bool Signed, int Size)
            {
                baInput1 = (Signed) ? SignExtend(baInput1, 8) : ZeroExtend(baInput1, 8);
                baInput2 = (Signed) ? SignExtend(baInput2, 8) : ZeroExtend(baInput2, 8);
                byte[] baResult = BitConverter.GetBytes(
                         BitConverter.ToUInt64(baInput1, 0)
                       % BitConverter.ToUInt64(baInput2, 0)
                    );
                return Cut(baResult, Size);
            }
            public static byte[] Increment(byte[] baInput1, int Size) // inc is twice as fast as add http://prntscr.com/od5rr9 (without flags)
            {
                byte[] baResult = BitConverter.GetBytes(
                       Convert.ToUInt64(
                       BitConverter.ToUInt64(baInput1, 0)
                       + 1
                    ));
                return Cut(baResult, Size);
            }
            public static byte[] Decrement(byte[] baInput1, int Size)
            {
                byte[] baResult = BitConverter.GetBytes(
                       Convert.ToUInt64(
                       BitConverter.ToUInt64(baInput1, 0)
                       + 1
                    ));
                
                return Cut(baResult, Size);
            }
            public static string GetBits(byte[] bInput)
            {
                string sOutput = "";
                for (int i = 0; i < bInput.Length; i++)
                {
                    sOutput += Convert.ToString(bInput[i],2).PadLeft(8, '0');
                }
                return sOutput;
            }
            public static string GetBits(byte bInput)
            {
                return Convert.ToString(bInput, 2).PadLeft(8, '0');
            }
            public static string GetBits(ushort sInput)
            {
                return Convert.ToString(sInput, 2).PadLeft(16, '0');
            }
            public static string GetBits(uint iInput)
            {
                return Convert.ToString(iInput, 2).PadLeft(32, '0');
            }
            public static string GetBits(ulong lInput)
            {
                return Convert.ToString((long)lInput, 2).PadLeft(64, '0');
            }
            public static byte[] GetBytes(string sInput)
            {
                byte[] baOutput = new byte[sInput.Length/8]; 
                for (int i = 0; i < baOutput.Length; i++) 
                {
                    baOutput[i] = Convert.ToByte(sInput.Substring(8*i,8), 2);
                }
                return baOutput;
            }
            public static byte[] SignExtend(byte[] baInput, byte bLength)
            {
                List<byte> blBuffer = baInput.ToList();
                byte bExtension = (byte)((blBuffer.Last() > 0x7F) ? 0xFF : 0x00);               
                for (int i = baInput.Length; i < bLength; i++)
                {
                    blBuffer.Add(bExtension);
                }
                 
                return blBuffer.ToArray();
            }
            public static byte[] SignExtend(byte[] baInput, RegisterCapacity _regcap)
            {
                return SignExtend(baInput, (byte)_regcap);
            }
            public static string SignExtend(string sInput, int bLength)
            {
                string sBuffer = "";
                char cExtension = (sInput[0] == '1') ?'1' : '0';
                for (int i = sInput.Length; i < bLength; i++)
                {
                    sBuffer += cExtension;
                }
                return sBuffer;
            }
            public static byte[] ZeroExtend(byte[] Input, byte Length) // http://prntscr.com/ofb1dy
            {
                Array.Resize(ref Input, Length);
                return Input;
            }
            public static byte[] ZeroExtend(byte[] baInput, RegisterCapacity _regcap)
            {
                return ZeroExtend(baInput, (byte)_regcap);
            }

        }
        public class Opcode
        {
            public enum FlagMode
            {
                Add,
                Sub,
                Mul,
                Logic,
                Inc,
                Dec,
            }
            public static void SetFlags(byte[] baResult, FlagMode fm, byte[] baInput1, byte[] baInput2)
            {
                switch (fm)
                {
                    case FlagMode.Add:
                        Eflags.Carry = (!baResult.IsGreater(baInput1)); // if result < input(val decreased)
                        baResult = Bitwise.Cut(baResult, (int)CurrentCapacity); // must be after carry                                                            
                        Eflags.Overflow = (baInput1.IsNegative() == baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                        // negative + negative should never be positive, vice versa
                        break;
                    case FlagMode.Sub:
                        Eflags.Carry = (baResult.IsGreater(baInput1)); // if result > input(val increased)
                        baResult = Bitwise.Cut(baResult, (int)CurrentCapacity); // must be after carry
                        Eflags.Overflow = (baInput1.IsNegative() != baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                        // negative + negative should never be positive, vice versa
                        break;
                    case FlagMode.Mul:
                        Eflags.Carry = (baResult != Bitwise.ZeroExtend(Bitwise.Cut(baResult, (int)CurrentCapacity), (byte)CurrentCapacity));
                        Eflags.Overflow = (baResult != Bitwise.SignExtend(Bitwise.Cut(baResult, (int)CurrentCapacity), (byte)CurrentCapacity));
                        //if this^^ is false, EDX = sign extension of EAX, we didn't overflow into the EDX register       
                        // take half the output, sign extend it, if they aren't the same we need to use two registers to store it
                        // dont know how this plays out with 64bit regs
                        break;
                    case FlagMode.Logic:
                        Eflags.Carry = false;
                        Eflags.Overflow = false;
                        break;
                    case FlagMode.Inc:
                        Eflags.Overflow = (!baInput1.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        break;
                    case FlagMode.Dec:
                        Eflags.Overflow = (baInput1.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        break;
                }
                Eflags.Sign = (baResult.IsNegative()); //+=false -=true
                Eflags.Zero = baResult.IsZero();
                Eflags.Parity = Bitwise.GetBits(baResult[0]).Count(x => x == 1) % 2 == 0; //parity: even no of 1 bits              
            }

            public static RegisterCapacity GetRegCap(RegisterCapacity defaultreg = RegisterCapacity.E)
            {
                PrefixByte[] _prefixes = ControlUnit.Prefixes.ToArray();
                RegisterCapacity _regcap;
                if (_prefixes.Contains(PrefixByte.REXW)) { _regcap = RegisterCapacity.R; }
                else if (_prefixes.Contains(PrefixByte.SIZEOVR)) { _regcap = RegisterCapacity.X; }
                else { _regcap = defaultreg; }
                return _regcap;
            }

            public struct DynamicResult
            {
                public byte[] SourceBytes;
                public byte[] DestBytes;
            }


            /*Dynamic functions turn..
             *  byte[] baResult;
                byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                byte[] baDestData;
                
                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lDest;
                    baDestData = ControlUnit.Fetch(lDestAddr, ControlUnit.CurrentCapacity);
                    baResult = Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, baResult);
                    }
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lDest;
                    baDestData = ControlUnit.FetchRegister(bcDestReg);
                    baResult = Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, baResult);
                    }
                }
             * ...into
             * byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
               byte[] baDestData = FetchDynamic(DestSrc);
               SetDynamic(DestSrc, Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity), IsSwap);
             * 
             * allows operations on modrm bytes directly at the cost of more arguments in constructors
             * 
             * 
             */
            public static void SetDynamic(OpcodeInput Input, byte[] baData)
            {
                if(Input.IsSwap)
                {
                    if(Input.DecodedModRM.Mod != 3)//not register->reg
                    {
                        SetRegister((ByteCode)Input.DecodedModRM.SourceReg, baData);
                    } else
                    {
                        SetRegister((ByteCode)Input.DecodedModRM.SourceReg, baData);
                    }
                } else
                {
                    if(Input.DecodedModRM.Mod != 3)
                    {
                        SetMemory(Input.DecodedModRM.DestPtr, baData);
                    } else
                    {
                        SetRegister((ByteCode)Input.DecodedModRM.DestPtr, baData, HigherBit:(CurrentCapacity==RegisterCapacity.B && Input.DecodedModRM.RM > 0x3));
                    }
                }
            }

            //works with r/m/imm
            public static DynamicResult FetchDynamic(OpcodeInput Input, bool PreventSignExtend=false)
            {
                DynamicResult Output = new DynamicResult();
                if(Input.IsSwap)
                {
                    if (Input.DecodedModRM.Mod != 3)
                    {
                        Output.SourceBytes = Fetch(Input.DecodedModRM.DestPtr, (int)CurrentCapacity);
                    }
                    else
                    {
                        Output.SourceBytes = FetchRegister((ByteCode)Input.DecodedModRM.DestPtr, CurrentCapacity);
                    }
                    Output.DestBytes = FetchRegister((ByteCode)Input.DecodedModRM.SourceReg);
                } else
                {
                    if (Input.IsImmediate)
                    {
                        if (Input.IsSignExtendedByte)
                        {
                            Output.SourceBytes = Bitwise.SignExtend(FetchNext(1), ((byte)CurrentCapacity));
                        }
                        else if (CurrentCapacity == RegisterCapacity.R)
                        {
                            //REX.W + 0D id OR RAX, imm32 I Valid N.E. RAX OR imm32 (sign-extended). (in general, sign extend when goes from imm32 to r64
                            Output.SourceBytes = (PreventSignExtend) ? Bitwise.ZeroExtend(FetchNext(4), 8) : Bitwise.SignExtend(FetchNext(4), 8);
                        }
                        else
                        {
                            Output.SourceBytes = FetchNext((byte)CurrentCapacity);
                        }
                    }
                    else
                    {
                        Output.SourceBytes = FetchRegister((ByteCode)Input.DecodedModRM.SourceReg);
                    }          
                    
                    if(Input.DecodedModRM.Mod != 3)
                    {
                        Output.DestBytes = Fetch(Input.DecodedModRM.DestPtr, (int)CurrentCapacity);
                    } else
                    {
                        Output.DestBytes = FetchRegister((ByteCode)Input.DecodedModRM.DestPtr, CurrentCapacity);
                    }
                    
                }
                return Output;                        
            }

            public static ModRM FromDest(ByteCode Dest)
            {
                return new ModRM { DestPtr = (ulong)Dest, Mod=3, RM=(byte)Dest};
            }

            
        }

        public static class Disassembly
        {
            public static List<Dictionary<RegisterCapacity, string>> RegisterMnemonics = new List<Dictionary<RegisterCapacity, string>>
            {
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RAX" }, { RegisterCapacity.E, "EAX" },{ RegisterCapacity.X, "AX" },{ RegisterCapacity.B, "AL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RCX" }, { RegisterCapacity.E, "ECX" },{ RegisterCapacity.X, "CX" },{ RegisterCapacity.B, "CL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RDX" }, { RegisterCapacity.E, "EDX" },{ RegisterCapacity.X, "DX" },{ RegisterCapacity.B, "DL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RBX" }, { RegisterCapacity.E, "EBX" },{ RegisterCapacity.X, "BX" },{ RegisterCapacity.B, "BL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RSP" }, { RegisterCapacity.E, "ESP" },{ RegisterCapacity.X, "SP" },{ RegisterCapacity.B, "AH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RBP" }, { RegisterCapacity.E, "EBP" },{ RegisterCapacity.X, "BP" },{ RegisterCapacity.B, "CH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RSI" }, { RegisterCapacity.E, "ESI" },{ RegisterCapacity.X, "SI" },{ RegisterCapacity.B, "DH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.R, "RDI" }, { RegisterCapacity.E, "EDI" },{ RegisterCapacity.X, "DI" },{ RegisterCapacity.B, "BH" }}
            };
            public static Dictionary<RegisterCapacity, string> PointerMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
            {
                {RegisterCapacity.B, "BYTE"},
                {RegisterCapacity.X, "WORD"},
                {RegisterCapacity.E, "DWORD"},
                {RegisterCapacity.R, "QWORD"}
            };
            public static string DisassembleSIB(SIB Input, int Mod)
            {
                string Source = "";
                if (Input.ScaledIndex != 4) // "none"
                {
                    Source += RegisterMnemonics[Input.ScaledIndex][Input.PointerSize];
                    if (Input.Scale > 0) // dont show eax*1 etc
                    {
                        Source += $"*{Input.Scale}";
                    }
                }

                if (Input.Base == 5) //[*] ptr/ ptr+ebp
                {
                    if (Mod > 0)
                    {
                        Source += $"+{RegisterMnemonics[(int)ByteCode.BP][Input.PointerSize]}";
                    }                              
                }
                else
                {
                    Source += $"+{RegisterMnemonics[Input.Base][Input.PointerSize]}";
                }
                return $"{Source}";
            }

            //{dest}, {src}{+/-}{offset}
            struct DisassembledObject
            {

            }
            public static string[] DisassembleModRM(OpcodeInput Input, RegisterCapacity RegCap)
            {
                if (Input.IsSwap)
                {
                    var _tmp = Input.DecodedModRM.SourceReg;
                    Input.DecodedModRM.SourceReg = Input.DecodedModRM.RM;
                    Input.DecodedModRM.SourceReg = _tmp;
                }
                string Dest = RegisterMnemonics[Input.DecodedModRM.RM][RegCap];
                string Source = RegisterMnemonics[(int)Input.DecodedModRM.SourceReg][RegCap];

                if (Input.DecodedModRM.RM == 4 && Input.DecodedModRM.Mod != 3) // sib conditions
                {
                    Dest = DisassembleSIB(Input.DecodedModRM.DecodedSIB, Input.DecodedModRM.Mod);
                }

                long OffsetSum = Input.DecodedModRM.Offset + Input.DecodedModRM.DecodedSIB.OffsetValue;
                if (OffsetSum != 0)
                {
                    if(!(Input.DecodedModRM.Mod == 0 && Input.DecodedModRM.RM == 5) && Input.DecodedModRM.DecodedSIB.ScaledIndex != 4) //disp32 or none filtered out
                    {
                        Dest += (Input.DecodedModRM.Offset < 0) ? "-" : "+";
                    }
                    Dest += $"0x{Math.Abs(Input.DecodedModRM.Offset).ToString("X")}";
                }

                if(Input.DecodedModRM.Mod != 3)
                {
                    Dest = $"{PointerMnemonics[RegCap]} PTR [{Dest}]";                   
                }
                
                if (Input.IsSwap)
                {
                    return new string[] { Source, Dest };
                } else
                {
                    return new string[] { Dest, Source };
                }
                
            }
        }

    }
}
