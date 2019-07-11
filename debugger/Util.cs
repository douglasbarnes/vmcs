using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Registers;
namespace debugger
{
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
            public static byte[] Trim(byte[] baInput, RegisterCapacity _regcap) // time difference between this and linq.take is huge, 
            {                                                           // http://prntscr.com/od20o4 // linq: http://prntscr.com/od20vw  //my way: http://prntscr.com/od21br
                byte[] baBuffer = new byte[(byte)_regcap/8];       
                for (int i = 0; i < baBuffer.Length; i++)
                {
                    baBuffer[i] = baInput[i];
                }
                return baInput;
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
            public static byte[] Or(byte[] baData1, byte[] baData2)
            {
                string sBits1 = GetBits(baData1);
                string sBits2 = GetBits(baData2);
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
                Opcode.SetFlags(baResult, Opcode.FlagMode.Logic, baData1, baData2);
                return baResult;
            }
            public static byte[] And(byte[] baData1, byte[] baData2)
            {
                string sBits1 = GetBits(baData1);
                string sBits2 = GetBits(baData2);
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
                Opcode.SetFlags(baResult, Opcode.FlagMode.Logic, baData1, baData2);
                return baResult;
            }
            public static byte[] Xor(byte[] baData1, byte[] baData2)
            {
                string sBits1 = GetBits(baData1);
                string sBits2 = GetBits(baData2);
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
                Opcode.SetFlags(baResult, Opcode.FlagMode.Logic, baData1, baData2);
                return baResult;
            }
            public static byte[] Add(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool bCarry)
            {
                /*int iSum;  /// revist this
                byte[] baResult = new byte[baInput1.Length];
                for (int i = baInput1.Length-1; i >= 0; i--)
                {
                    iSum = baInput1[i] + baInput2[i];
                    if(iSum > 255)
                    {
                        baResult[i - 1] += 1;
                        baResult[i] += (byte)(iSum - 255);
                    } else
                    {
                        baResult[i] += (byte)iSum;
                    }
                }*/
                byte[] baResult = BitConverter.GetBytes(
                        Convert.ToUInt64(
                         BitConverter.ToUInt64(baInput1, 0)
                       + BitConverter.ToUInt64(baInput2, 0)
                       + (ulong)(bCarry && Eflags.Carry ? 1 : 0)
                    ));
                Opcode.SetFlags(baResult, Opcode.FlagMode.Add, baInput1, baInput2);
                return Trim(baResult, _regcap);
            }
            public static byte[] Subtract(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool bCarry)
            {
                byte[] baResult = BitConverter.GetBytes(
                        Convert.ToUInt64(
                         BitConverter.ToUInt64(baInput1, 0)
                       - BitConverter.ToUInt64(baInput2, 0)
                       + (ulong)(bCarry && Eflags.Carry ? 1 : 0) //add borrow
                    ));
                Opcode.SetFlags(baResult, Opcode.FlagMode.Sub, baInput1, baInput2);
                return Trim(baResult, _regcap);
            }
            public static byte[] Divide(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool Signed)
            {
                if (Signed)
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes((ulong)Math.Floor((double)Convert.ToInt64(BitConverter.ToInt64(baInput1, 0) / BitConverter.ToInt64(baInput2, 0))));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes((uint)Math.Floor((double)Convert.ToInt64(BitConverter.ToInt32(baInput1, 0) / BitConverter.ToInt32(baInput2, 0)))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes((ushort)Math.Floor((double)Convert.ToInt32(BitConverter.ToInt32(baInput1, 0) / BitConverter.ToInt32(baInput2, 0))));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes((byte)Math.Floor((double)Convert.ToInt16(baInput1[0] * baInput2[0])));
                        default:
                            throw new Exception();
                    }
                }
                else
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes((ulong)Math.Floor((double)Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) / BitConverter.ToUInt64(baInput2, 0))));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes((uint)Math.Floor((double)Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) / BitConverter.ToUInt32(baInput2, 0)))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes((ushort)Math.Floor((double)Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) / BitConverter.ToUInt32(baInput2, 0))));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes((byte)Math.Floor((double)Convert.ToUInt16(baInput1[0] / baInput2[0])));
                        default:
                            throw new Exception();
                    }
                }
                //divide dont set flags
            }
            public static byte[] Multiply(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool Signed)
            {
                byte[] baResult;
                if(Signed)
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            baResult = BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt64(baInput1, 0) * BitConverter.ToInt64(baInput2, 0)));
                            break;
                        case RegisterCapacity.E:// this is making it too long fix it
                            baResult = BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt32(baInput1, 0) * BitConverter.ToInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                            break;
                        case RegisterCapacity.X:
                            baResult = BitConverter.GetBytes(Convert.ToInt32(BitConverter.ToInt32(baInput1, 0) * BitConverter.ToInt32(baInput2, 0)));
                            break;
                        case RegisterCapacity.B:
                            baResult = BitConverter.GetBytes(Convert.ToInt16(baInput1[0] * baInput2[0]));
                            break;
                        default:
                            throw new Exception();
                    }
                    
                    
                } else
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) * BitConverter.ToUInt64(baInput2, 0)));
                            break;
                        case RegisterCapacity.E:
                            baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                            break;
                        case RegisterCapacity.X:
                            baResult = BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0)));
                            break;
                        case RegisterCapacity.B:
                            baResult = BitConverter.GetBytes(Convert.ToUInt16(baInput1[0] * baInput2[0]));
                            break;
                        default:
                            throw new Exception();
                    }
                }
                //set flags             
                Opcode.SetFlags(baResult, Opcode.FlagMode.Mul);
                return baResult;
            }
            public static byte[] Modulo(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool Signed)
            {
                if (Signed)
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt64(baInput1, 0) % BitConverter.ToInt64(baInput2, 0)));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt32(baInput1, 0) % BitConverter.ToInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes(Convert.ToInt32(BitConverter.ToInt32(baInput1, 0) % BitConverter.ToInt32(baInput2, 0)));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes(Convert.ToInt16(baInput1[0] * baInput2[0]));
                        default:
                            throw new Exception();
                    }
                }
                else
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) % BitConverter.ToUInt64(baInput2, 0)));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) % BitConverter.ToUInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) % BitConverter.ToUInt32(baInput2, 0)));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes(Convert.ToUInt16(baInput1[0] * baInput2[0]));
                        default:
                            throw new Exception();
                    }
                }
            }
            public static byte[] Increment(byte[] baInput1, RegisterCapacity _regcap) // inc is twice as fast as add http://prntscr.com/od5rr9 (without flags)
            {
                byte[] baResult = BitConverter.GetBytes(
                       Convert.ToUInt64(
                       BitConverter.ToUInt64(baInput1, 0)
                       + 1
                    ));
                Opcode.SetFlags(baResult, Opcode.FlagMode.Inc, baInput1);
                return Trim(baResult, _regcap);
            }
            public static byte[] Decrement(byte[] baInput1, RegisterCapacity _regcap)
            {
                byte[] baResult = BitConverter.GetBytes(
                       Convert.ToUInt64(
                       BitConverter.ToUInt64(baInput1, 0)
                       + 1
                    ));
                Opcode.SetFlags(baResult, Opcode.FlagMode.Dec, baInput1);
                return Trim(baResult, _regcap);
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
                return SignExtend(baInput, (byte)((byte)_regcap / 8));
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
            public static byte[] ZeroExtend(byte[] baInput, byte bLength)
            {
                List<byte> blBuffer = baInput.ToList();
                for (int i = baInput.Length; i < bLength; i++)
                {
                    blBuffer.Add(0);
                }
                return blBuffer.ToArray();
            }

            public static byte[] ZeroExtend(byte[] baInput, RegisterCapacity _regcap)
            {
                return ZeroExtend(baInput, (byte)((byte)_regcap / 8));
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
            public static void SetFlags(byte[] baResult, FlagMode fm, byte[] baInput1=null, byte[] baInput2=null)
            {
                RegisterCapacity _regcap = ControlUnit.CurrentCapacity;
                switch (fm)
                {
                    case FlagMode.Add:
                        Eflags.Carry = (!baResult.IsGreater(baInput1)); // if result < input(val decreased)
                        baResult = Bitwise.Trim(baResult, _regcap); // must be after carry                                                            
                        Eflags.Overflow = (baInput1.IsNegative() == baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                        // negative + negative should never be positive, vice versa
                        break;
                    case FlagMode.Sub:
                        Eflags.Carry = (baResult.IsGreater(baInput1)); // if result > input(val increased)
                        baResult = Bitwise.Trim(baResult, _regcap); // must be after carry
                        Eflags.Overflow = (baInput1.IsNegative() != baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                        // negative + negative should never be positive, vice versa
                        break;
                    case FlagMode.Mul:
                        Eflags.Carry = (baResult != Bitwise.ZeroExtend(Bitwise.Trim(baResult, _regcap), _regcap));
                        Eflags.Overflow = (baResult != Bitwise.SignExtend(Bitwise.Trim(baResult, _regcap), _regcap));
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
                PrefixBytes[] _prefixes = ControlUnit.Prefixes.ToArray();
                RegisterCapacity _regcap;
                if (_prefixes.Contains(PrefixBytes.REXW)) { _regcap = RegisterCapacity.R; }
                else if (_prefixes.Contains(PrefixBytes.SIZEOVR)) { _regcap = RegisterCapacity.X; }
                else { _regcap = defaultreg; }
                return _regcap;
            }

            public static byte[] ImmediateFetch(bool SignExtendedByte)
            {
                if (SignExtendedByte)
                {
                    return Bitwise.SignExtend(ControlUnit.FetchNext(1), (byte)((byte)ControlUnit.CurrentCapacity / 8));
                }
                else if (ControlUnit.CurrentCapacity == RegisterCapacity.R)
                {
                    //REX.W + 0D id OR RAX, imm32 I Valid N.E. RAX OR imm32 (sign-extended). (in general, sign extend when goes from imm32 to r64
                    return Bitwise.SignExtend(ControlUnit.FetchNext(4), 8);
                }
               else
               {
                   return ControlUnit.FetchNext((byte)((ControlUnit.CurrentCapacity == RegisterCapacity.R) ? 4 : (byte)ControlUnit.CurrentCapacity / 8));
               }
                
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
            public static void SetDynamic(ModRM ModRMByte, byte[] baData, bool Swap=false)
            {
                if (ModRMByte.IsAddress)
                {
                    if(Swap)
                    {
                        ControlUnit.SetRegister((ByteCode)ModRMByte.lSource, baData, ControlUnit.CurrentCapacity);
                    }
                    else
                    {
                        ControlUnit.SetMemory(ModRMByte.lDest, baData);
                    }                    
                } else
                {
                    if(Swap)
                    {
                        ControlUnit.SetRegister((ByteCode)ModRMByte.lSource, baData);
                    } else
                    {
                        ControlUnit.SetRegister((ByteCode)ModRMByte.lDest, baData);
                    }                   
                }
            }

            public static byte[] FetchDynamic(ModRM ModRMByte, bool Swap=false)
            {
                if(Swap)
                {
                    if (ModRMByte.IsAddress)
                    {
                        return ControlUnit.Fetch(ModRMByte.lDest, ControlUnit.CurrentCapacity);
                    }
                    else
                    {
                        return ControlUnit.FetchRegister((ByteCode)ModRMByte.lDest);
                    }
                } else
                {
                    return ControlUnit.FetchRegister((ByteCode)ModRMByte.lSource);
                    /*(if (ModRMByte.IsAddress)
                    {
                        return ControlUnit.Fetch(ModRMByte.lSource, ControlUnit.CurrentCapacity);
                    }
                    else
                    {
                        return ControlUnit.FetchRegister((ByteCode)ModRMByte.lSource);
                    }*/
                }
                          
            }

            public static ModRM FromDest(ByteCode Dest)
            {
                return new ModRM { lDest = (ulong)Dest };
            }
        }



    }
}
