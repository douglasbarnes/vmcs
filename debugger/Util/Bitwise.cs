using System;
using System.Linq;
using static debugger.Emulator.FlagSet;
using debugger.Emulator;
namespace debugger.Util
{
   

    public static class Bitwise
    {
        public static byte[] ReverseEndian(byte[] input)
        {
            byte[] Buffer = input.DeepCopy(); // MIGHT be unnecessary, depends on whether reverse sets the array to a new value(unlikely, no ref keyword) iterates each value
            Array.Reverse(Buffer);
            return Buffer;
        }
        public static byte[] Zero = Enumerable.Repeat((byte)0, 128).ToArray();
        public static byte[] Trim(byte[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[input.Length - i - 1] != 0)
                {
                    Array.Resize(ref input, input.Length - i);// cut after first non zero
                }
            }
            return input;
        }
        public static byte[] Cut(byte[] input, int count) // time difference between this and linq.take is huge, 
        {                                                           // http://prntscr.com/od20o4 // linq: http://prntscr.com/od20vw  //my way: http://prntscr.com/od21br
            Array.Resize(ref input, count);
            return input;
        }
        public static byte[] Subarray(byte[] input, int offset)
        {
            byte[] Buffer = new byte[input.Length - offset];
            Array.Copy(input, offset, Buffer, 0, input.Length - offset);
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
        public static FlagSet Or(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits);

            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' || input2Bits[i] == '1')
                {
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits);
            return new FlagSet(Result)
            {
                Carry = FlagState.Off,
                Overflow = FlagState.Off,
                Auxiliary = FlagState.Undefined
            };
        }
        public static FlagSet And(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits);

            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' && input2Bits[i] == '1')
                {
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits);
            return new FlagSet(Result)
            {
                Carry = FlagState.Off,
                Overflow = FlagState.Off,
                Auxiliary = FlagState.Undefined
            };
        }
        public static FlagSet Xor(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits);

            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' ^ input2Bits[i] == '1')
                {
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits);
            return new FlagSet(Result)
            {
                Carry = FlagState.Off,
                Overflow = FlagState.Off,
                Auxiliary = FlagState.Undefined
            };
        }
        public static FlagSet Add(byte[] input1, byte[] input2, int size, out byte[] Result, bool carry = false)
        {
            Result = new byte[size];
            for (int i = 0; i < size; i++) // faster doing it my own way http://prntscr.com/ojwfs2
            {
                int sum = input1[i] + input2[i] + (carry ? 1 : 0); //(any carries
                if (sum > 0xFF) //overflowed that index
                {
                    Result[i] += (byte)(sum % 0x100);//leftover value stays
                    carry = true;//add 1 to next index
                }
                else
                {
                    Result[i] += (byte)sum;
                    carry = false;
                }

            }
            return new FlagSet(Result)
            {
                Carry = carry ? FlagState.On : FlagState.Off,
                Overflow = (input1.IsNegative() == input2.IsNegative() && Result.IsNegative() != input1.IsNegative()) ? FlagState.On : FlagState.Off,//adding two number of same sign and not getting the same sign as a result
                Auxiliary = FlagState.Off
            };
        }
        public static FlagSet Subtract(byte[] input1, byte[] input2, int size, out byte[] Result, bool carry = false)
        {
            Result = new byte[size];
            bool Borrow = false;
            for (int i = 0; i < size; i++)
            {
                int sum = input1[i] - input2[i] - (Borrow ? 1 : 0) + (carry ? 1 : 0);
                carry = false; //carried from another ins
                if (sum < 0)
                {
                    Result[i] = (byte)(sum + 256);
                    Borrow = true;
                }
                else
                {
                    Result[i] = (byte)sum;
                    Borrow = false;
                }
            }
            //carry, sign changed
            //overflow, negative - positive = positive
            return new FlagSet(Result) // negative - positive = positive   or positive - negative = negative
            {
                Carry = Borrow ? FlagState.On : FlagState.Off,
                Overflow = input1.IsNegative() != input2.IsNegative() && Result.IsNegative() != input1.IsNegative() ? FlagState.On : FlagState.Off,//adding two number of same sign and not getting the same sign as a result
                Auxiliary = FlagState.Off // subtracted a bigger number from a smaller number and didnt get a negative
            };
        }
        public static void Divide(byte[] dividend, byte[] divisor, bool signed, int size, out byte[] Quotient, out byte[] Modulo)
        {
            Quotient = new byte[size];
            //divide dont set flags
            while (dividend.CompareTo(divisor, signed) == 1)
            {
                Subtract(dividend, divisor, size, out dividend);
                Increment(Quotient, size, out Quotient);
            }
            Modulo = dividend;
        }
        public static FlagSet Multiply(byte[] input1, byte[] input2, bool signed, int size, out byte[] Result)
        {
            input1 = signed ? SignExtend(input1, (byte)(size * 2)) : ZeroExtend(input1, (byte)(size * 2));
            input2 = signed ? SignExtend(input2, (byte)(size * 2)) : ZeroExtend(input2, (byte)(size * 2));
            Result = new byte[size * 4];     //the result is going to be size*2, so we need to extend the inputs to that before and then cut down after.           
            for (int i = 0; i < size * 2; i++)
            {
                for (int j = 0; j < size * 2; j++)
                {    //times input[i] by every value in input[2] throughout the iteration  
                    int mul = (input1[i] * input2[j]) + Result[j + i];  // times the two and if there is a carry/something stored in the result already, add that(we have to do this here because otherwise we wouldn't be checking if we overflowed a byte later                         
                    Result[j + i] = (byte)(mul % 0x100); //multiplying bytes can give a 2byte result, so we add the LSB here 
                    if (mul > 0xFF) //if we need to carry
                    {
                        byte[] carry = new byte[size * 4];
                        carry[i + j + 1] = (byte)(mul / 0x100);
                        Add(Result, carry, size * 4, out Result);// then the MSB here,(a carry).    e.g the two byte A12B % 0x100 = 2B, A12B / 0x100 = A1 ( / operator is integer division)
                    }
                }
            }
            Array.Resize(ref Result, size * 2);
            byte[] UpperComparison = new byte[size * 2];
            Array.Copy(Result, UpperComparison, size);
            UpperComparison = (signed) ? SignExtend(UpperComparison, (byte)(size * 2)) : ZeroExtend(UpperComparison, (byte)(size * 2));
            bool UpperUsed = UpperComparison.CompareTo(Result, signed) != 0;
            return new FlagSet() //only these 3 are set in 
            {
                Overflow = UpperUsed ? FlagState.On : FlagState.Off,
                Carry = UpperUsed ? FlagState.On : FlagState.Off,
                Sign = Result[size - 1] > 0x7F ? FlagState.On : FlagState.Off
            };
        }
        public static FlagSet Increment(byte[] input, int size, out byte[] Result)
        {
            Result = new byte[size];//0xffffffff+0x1=0x00000000, change it otherwise
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] < 0xFF)
                {
                    input[i]++;
                    Result = input;
                    break;
                }
            }
            return new FlagSet(Result)
            {
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.On : FlagState.Off,//if we increased it and got a negative
                Auxiliary = FlagState.Off,
            };
        }
        public static FlagSet Decrement(byte[] input, int size, out byte[] Result)
        {
            Result = SignExtend(new byte[] { 0xFF }, (byte)size);//0x0-0x1=0xffffffff, change it otherwise
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 0x00)
                {
                    input[i]--;
                    Result = input;
                    break;
                }
            }
            return new FlagSet(Result)
            {
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.On : FlagState.Off,//if we increased it and got a negative
                Auxiliary = FlagState.Off,
            };
        }
        public static string GetBits(byte[] input)
        {
            string sOutput = "";
            for (int i = 0; i < input.Length; i++)
            {
                sOutput += Convert.ToString(input[i], 2).PadLeft(8, '0');
            }
            return sOutput;
        }
        public static string GetBits(byte input)
        {
            return Convert.ToString(input, 2).PadLeft(8, '0');
        }
        public static string GetBits(ushort input)
        {
            return Convert.ToString(input, 2).PadLeft(16, '0');
        }
        public static string GetBits(uint input)
        {
            return Convert.ToString(input, 2).PadLeft(32, '0');
        }
        public static string GetBits(ulong input)
        {
            return Convert.ToString((long)input, 2).PadLeft(64, '0');
        }
        public static byte[] GetBytes(string bitString)
        {
            byte[] baOutput = new byte[bitString.Length / 8];
            for (int i = 0; i < baOutput.Length; i++)
            {
                baOutput[i] = Convert.ToByte(bitString.Substring(8 * i, 8), 2);
            }
            return baOutput;
        }
        public static byte[] SignExtend(byte[] input, byte newLength)
        {
            int startIndex = input.Length;
            byte sign = (byte)((input[startIndex - 1]) > 0x7F ? 0xFF : 0x00);
            Array.Resize(ref input, newLength);
            for (int i = startIndex; i < newLength; i++)
            {
                input[i] = sign;
            }
            return input;
        }
        public static string SignExtend(string sInput, int bLength)
        {
            string sBuffer = "";
            char cExtension = (sInput[0] == '1') ? '1' : '0';
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
    }
}
