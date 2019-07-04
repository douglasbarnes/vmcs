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
            public static string LogicalOr(string sBits1, string sBits2)
            {
                if (sBits1.Length != sBits2.Length)
                {
                    throw new Exception();
                }

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
                if (sBits1.Length != sBits2.Length)
                {
                    throw new Exception();
                }

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


            public static string GetBits(byte bInput)
            {
                return Convert.ToString(bInput, 2).PadLeft((int)RegisterCapacity.B, '0');
            }

            public static string GetBits(ushort sInput)
            {
                return Convert.ToString(sInput, 2).PadLeft((int)RegisterCapacity.X, '0');
            }

            public static string GetBits(uint iInput)
            {
                return Convert.ToString(iInput, 2).PadLeft((int)RegisterCapacity.E, '0');
            }

            public static string GetBits(ulong lInput)
            {
                return Convert.ToString((long)lInput, 2).PadLeft((int)RegisterCapacity.R, '0');
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
