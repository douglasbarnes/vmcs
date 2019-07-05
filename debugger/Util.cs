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
        public static class Bitwise
        {
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
            public static string LogicalOr(string sBits1, string sBits2)
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
                switch(_regcap)
                {
                    case RegisterCapacity.R:
                        return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1,0) + BitConverter.ToUInt64(baInput2,0)));
                    case RegisterCapacity.E:
                        return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) + BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.X:
                        return BitConverter.GetBytes(Convert.ToUInt16(BitConverter.ToUInt32(baInput1, 0) + BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.B:
                        return new byte[] { (byte)(baInput1[0] + baInput2[0])};
                    default:
                        throw new Exception();
                }
                
            }
            public static byte[] Subtract(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) - BitConverter.ToUInt64(baInput2, 0)));
                    case RegisterCapacity.E:                                                             
                        return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) - BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.X:                                                             
                        return BitConverter.GetBytes(Convert.ToUInt16(BitConverter.ToUInt32(baInput1, 0) - BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.B:
                        return new byte[] { (byte)(baInput1[0] - baInput2[0]) };
                    default:
                        throw new Exception();
                }
            }
            public static byte[] Divide(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) / BitConverter.ToUInt64(baInput2, 0)));
                    case RegisterCapacity.E:
                        return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) / BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.X:
                        return BitConverter.GetBytes(Convert.ToUInt16(BitConverter.ToUInt32(baInput1, 0) / BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.B:
                        return new byte[] { (byte)(baInput1[0] / baInput2[0]) };
                    default:
                        throw new Exception();
                }
            }
            public static byte[] Multiply(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) * BitConverter.ToUInt64(baInput2, 0)));
                    case RegisterCapacity.E:
                        return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.X:
                        return BitConverter.GetBytes(Convert.ToUInt16(BitConverter.ToUInt32(baInput1, 0) * BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.B:
                        return new byte[] { (byte)(baInput1[0] * baInput2[0]) };
                    default:
                        throw new Exception();
                }
            }
            public static byte[] Modulo(byte[] baInput1, byte[] baInput2, RegisterCapacity _regcap)
            {
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        return BitConverter.GetBytes(Convert.ToUInt64(BitConverter.ToUInt64(baInput1, 0) % BitConverter.ToUInt64(baInput2, 0)));
                    case RegisterCapacity.E:
                        return BitConverter.GetBytes(Convert.ToUInt32(BitConverter.ToUInt32(baInput1, 0) % BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.X:
                        return BitConverter.GetBytes(Convert.ToUInt16(BitConverter.ToUInt32(baInput1, 0) % BitConverter.ToUInt32(baInput2, 0)));
                    case RegisterCapacity.B:
                        return new byte[] { (byte)(baInput1[0] % baInput2[0]) };
                    default:
                        throw new Exception();
                }
            }
            public static string GetBits(byte[] bInput)
            {
                string sOutput = "";
                for (int i = 0; i < bInput.Length; i++)
                {
                    sOutput += Convert.ToString(bInput[i],2).PadLeft(8);
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
                for (int i = 0; i < sInput.Length; i += 8)
                {
                    baOutput[i] = Convert.ToByte(sInput.Substring(i,8));
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



            public static void MoveReg(ByteCode bcDestCode, ByteCode bcSrcCode, RegisterCapacity WorkingBits)
            {
                ControlUnit.SetRegister(bcDestCode, ControlUnit.FetchRegister(bcSrcCode, WorkingBits), WorkingBits);
            }
        }



    }
}
