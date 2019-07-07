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
        public static bool IsPositive(this byte[] baInput)
        {
            if(Convert.ToString(baInput[0],2).Length != 8) //                          -128 64 32 16 8 4 2 1
            { // twos compliment: negative number always has a greatest set bit of 1 .eg, 1  0  0  0 0 0 0 1 = -128+1 = -127
                return true; // this way is much faster than using GetBits() because padleft iterates the whole string multiple times
            }  // this method is just for performance because its used alot
            else
            {
                return false;
            }
        }
        public static class Bitwise
        {
            public static byte[] Zero = Enumerable.Repeat((byte)0, 128).ToArray();
            private static void _padequal(ref string s1, ref string s2)
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
            public static string Or(string sBits1, string sBits2)
            {
                _padequal(ref sBits1, ref sBits2);

                string sResult = "";
                for (int i = 0; i < sBits1.Length; i++)
                {
                    if (sBits1[i] == '1' || sBits2[i] == '1')
                    {
                        sResult += "1";
                    }
                    else
                    {
                        sResult += "0";
                    }
                }
                return sResult;
            }

            public static byte[] Or(byte[] baData1, byte[] baData2)
            {
                return GetBytes(Or(GetBits(baData1), GetBits(baData2)));
            }
            public static string LogicalAnd(string sBits1, string sBits2)
            {
                _padequal(ref sBits1, ref sBits2);

                string sResult = "";
                for (int i = 0; i < sBits1.Length; i++)
                {
                    if (sBits1[i] == '1' && sBits2[i] == '1')
                    {
                        sResult += "1";
                    }
                    else
                    {
                        sResult += "0";
                    }
                }
                return sResult;
            }
            public static byte[] Add(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                /*int iSum; //LITTLE ENDIAN ONLY /// revist this
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
                byte[] baResult;
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) + BitConverter.ToUInt64(baInput2, 0)));
                        break;
                    case RegisterCapacity.E:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) + BitConverter.ToUInt32(baInput2, 0))); // touint64 because we dont handle overflows ourself
                        break;
                    case RegisterCapacity.X:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) + BitConverter.ToUInt32(baInput2, 0)));
                        break;
                    case RegisterCapacity.B:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(baInput1[0] + baInput2[0]));
                        break;
                    default:
                        throw new Exception();
                }
                baResult = ZeroExtend(baResult, (byte)_regcap); // this works with the carry flag check
                //flags
                // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                // negative + negative should never be positive, vice versa
                if (baInput1.IsPositive() == baInput2.IsPositive() && baResult.IsPositive() != baInput1.IsPositive())
                {
                    Eflags.Overflow = true;
                } 

                if (baResult.Length > (int)_regcap)
                {
                    Eflags.Carry = true;
                    return baResult.Skip(1).ToArray(); // always 1 maybe?? baResult.Length - (int)_regcap
                } else
                {
                    return baResult;
                }
            }
            public static byte[] Subtract(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                byte[] baResult;
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) - BitConverter.ToUInt64(baInput2, 0)));
                        break;
                    case RegisterCapacity.E:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) - BitConverter.ToUInt32(baInput2, 0))); // touint64 because we dont handle overflows ourself
                        break;
                    case RegisterCapacity.X:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) - BitConverter.ToUInt32(baInput2, 0)));
                        break;
                    case RegisterCapacity.B:
                        baResult = BitConverter.GetBytes(Convert.ToUInt64(baInput1[0] + baInput2[0]));
                        break;
                    default:
                        throw new Exception();
                }
                baResult = ZeroExtend(baResult, (byte)_regcap); // this works with the carry flag check
                //flags
                // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                // negative + negative should never be positive, vice versa
                if (baInput1.IsPositive() == baInput2.IsPositive() && baResult.IsPositive() != baInput1.IsPositive())
                {
                    Eflags.Overflow = true;
                }

                if (baResult.Length > (int)_regcap)
                {
                    Eflags.Carry = true;
                    return baResult.Skip(1).ToArray(); // always 1 maybe?? baResult.Length - (int)_regcap
                }
                else
                {
                    return baResult;
                }
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
            }
            public static byte[] Multiply(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap, bool Signed)
            {
                if(Signed)
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt64(baInput1, 0) * BitConverter.ToInt64(baInput2, 0)));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes(Convert.ToInt64(BitConverter.ToInt32(baInput1, 0) * BitConverter.ToInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes(Convert.ToInt32(BitConverter.ToInt32(baInput1, 0) * BitConverter.ToInt32(baInput2, 0)));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes(Convert.ToInt16(baInput1[0] * baInput2[0]));
                        default:
                            throw new Exception();
                    }
                } else
                {
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) * BitConverter.ToUInt64(baInput2, 0)));
                        case RegisterCapacity.E:
                            return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0))); // we take this up a byte order because overflows are supported in multiply
                        case RegisterCapacity.X:
                            return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0)));
                        case RegisterCapacity.B:
                            return BitConverter.GetBytes(Convert.ToUInt16(baInput1[0] * baInput2[0]));
                        default:
                            throw new Exception();
                    }
                }               
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

        }

        public class Opcode
        {
            public static RegisterCapacity GetRegCap(RegisterCapacity defaultreg = RegisterCapacity.E)
            {
                PrefixBytes[] _prefixes = ControlUnit.Prefixes.ToArray();
                RegisterCapacity _regcap;
                if (_prefixes.Contains(PrefixBytes.REXW)) { _regcap = RegisterCapacity.R; }
                else if (_prefixes.Contains(PrefixBytes.SIZEOVR)) { _regcap = RegisterCapacity.X; }
                else { _regcap = defaultreg; }
                return _regcap;
            }

            public static uint QWORDCrop(ulong lAddress)
            {
                return BitConverter.ToUInt32(BitConverter.GetBytes(lAddress).Take(4).ToArray(), 0);
            }



            public static byte[] ImmediateFetch32(bool _signextend)
            {
                if (_signextend)
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
                    if (ModRMByte.IsAddress)
                    {
                        return ControlUnit.Fetch(ModRMByte.lSource, ControlUnit.CurrentCapacity);
                    }
                    else
                    {
                        return ControlUnit.FetchRegister((ByteCode)ModRMByte.lSource);
                    }
                }
                          
            }

            public static ModRM FromDest(ByteCode Dest)
            {
                return new ModRM { lDest = (ulong)Dest };
            }
        }



    }
}
