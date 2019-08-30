using System;
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
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
                Auxiliary = FlagState.UNDEFINED
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
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
                Auxiliary = FlagState.UNDEFINED
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
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
                Auxiliary = FlagState.UNDEFINED
            };
        }
        public static FlagSet Add(byte[] input1, byte[] input2, int size, out byte[] Result, bool carry = false)
        {
            input1 = SignExtend(input1, (byte)size);
            input2 = SignExtend(input2, (byte)size);
            Result = new byte[size];
            bool CarryBit3 = ((input1[0] & 0b111) + (input2[0] & 0b111)) > 0b111; // if we will carry into bit 4 of 1st byte
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
                Carry = carry ? FlagState.ON : FlagState.OFF,
                Overflow = (input1.IsNegative() == input2.IsNegative() && Result.IsNegative() != input1.IsNegative()) ? FlagState.ON : FlagState.OFF,//adding two number of same sign and not getting the same sign as a result
                Auxiliary = CarryBit3 ? FlagState.ON : FlagState.OFF
            };
        }
        public static FlagSet Subtract(byte[] input1, byte[] input2, int size, out byte[] Result, bool borrow = false)
        {
            input1 = SignExtend(input1, (byte)size);
            input2 = SignExtend(input2, (byte)size);
            Result = new byte[size];
            bool BorrowBit4 = (input1[0] & 0b100) < (input2[0] & 0b100); //if input2 had bit 3 on and input1 had it off, there was a borrow from the BCD bit
            for (int i = 0; i < size; i++)
            {
                int sum = input1[i] - input2[i] - (borrow ? 1 : 0);
                if (sum < 0)
                {
                    Result[i] = (byte)(sum + 256);
                    borrow = true;
                }
                else
                {
                    Result[i] = (byte)sum;
                    borrow = false;
                }
            }
            //carry, sign changed
            //overflow, negative - positive = positive
            return new FlagSet(Result) // negative - positive = positive   or positive - negative = negative
            {
                Carry = borrow ? FlagState.ON : FlagState.OFF,
                Overflow = input1.IsNegative() != input2.IsNegative() && Result.IsNegative() != input1.IsNegative() ? FlagState.ON : FlagState.OFF,//adding two number of same sign and not getting the same sign as a result
                Auxiliary = BorrowBit4 ? FlagState.ON : FlagState.OFF // subtracted a bigger number from a smaller number and didnt get a negative
            };
        }
        public static void Divide(byte[] dividend, byte[] divisor, bool signed, int size, out byte[] Quotient, out byte[] Modulo)
        {
            Quotient = new byte[size];
            //divide doesnt set flags
            while (dividend.CompareTo(divisor, signed) == 1) // dividend > divisor
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
                Overflow = UpperUsed ? FlagState.ON : FlagState.OFF,
                Carry = UpperUsed ? FlagState.ON : FlagState.OFF,
                Sign = signed ? (Result[size - 1] > 0x7F ? FlagState.ON : FlagState.OFF) : FlagState.UNDEFINED
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
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,//if we increased it and got a negative
                Auxiliary = FlagState.OFF,
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
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,//if we increased it and got a negative
                Auxiliary = FlagState.OFF,
            };
        }
        public static FlagSet ShiftLeft(byte[] input, byte count, byte mask, int size, out byte[] Result)
        {
            count &= mask;
            int StartCount = count; // shifting more than 0x3f times in 64bit ins or 0x1f otherwise, isnt allowed                   
            Result = input;
            Array.Resize(ref Result, size);
            if (count == 0) {
                return new FlagSet();
            }
            //Arith
            bool Pull = false;
            for (; count > 0; count--)
            {
                Pull = false;
                for (int i = 0; i < Result.Length; i++)
                {
                    int PullMask = Pull ? 0b1 : 0;
                    Pull = (MSB(Result[i]) == 1);
                    Result[i] = (byte)(((Result[i]) << 1) | PullMask); 
                }
                
            }
            //Flags
            FlagSet OutputFlags = new FlagSet(Result);
            if(StartCount == 1)
            {
                OutputFlags.Overflow = (MSB(Result) == 1) ^ Pull ? FlagState.ON : FlagState.OFF;
            }
            OutputFlags.Carry = Pull ? FlagState.ON : FlagState.OFF;
            return OutputFlags;
        }
        public static FlagSet ShiftRight(byte[] input, byte count, byte mask, int size,  out byte[] Result, bool arithmetic)
        {
            count &= mask;
            int StartCount = count; // shifting more than 0x3f times in 64bit ins or 0x1f otherwise, isnt allowed 
            Result = new byte[size];
            Array.Copy(input, Result, input.Length);
            if (StartCount == 0)
            {
                return new FlagSet();
            }            
            //Arith
            for (; count > 0; count--)
            {
                bool Push = false;
                for (int i = Result.Length - 1; i >= 0; i--)
                {
                    int PushMask = Push ? 0b10000000 : 0;
                    Push = ((Result[i] & 1) == 1);
                    Result[i] = (byte)((Result[i] >> 1) | PushMask);
                }
                
            }
            //Flags
            int InputMSB = (input.Length == size) ? MSB(input) : 0; // if the input was less than the intended size, the msb would be 0
            if (arithmetic) // sar keeps the sign of the input in the result, shl keeps it in of
            {
                Result[Result.Length - 1] |= (byte)(InputMSB << 8);
            }
            FlagSet OutputFlags = new FlagSet(Result);
            if(StartCount == 1)
            {
                if (arithmetic) // sar keeps the sign of the input in the result, shl keeps it in of
                {
                    OutputFlags.Overflow = FlagState.OFF;
                }
                else
                {
                    OutputFlags.Overflow = InputMSB == 1 ? FlagState.ON : FlagState.OFF;
                }
            }
            return OutputFlags;
        }
        public static byte MSB(byte input) => (byte)((input >> 7) & 1);
        public static byte MSB(byte[] input) => (byte)((input[input.Length - 1] >> 7) & 1);//readable i guess? some reason its a good idea
        public static byte LSB(byte input) => (byte)((input >> 7) & 1);
        public static byte LSB(byte[] input) => (byte)((input[0] >> 7) & 1); 
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
            if(input.Length < newLength)
            {
                int startIndex = input.Length;
                byte sign = (byte)(input[startIndex - 1] > 0x7F ? 0xFF : 0x00);
                Array.Resize(ref input, newLength);
                for (int i = startIndex; i < newLength; i++)
                {
                    input[i] = sign;
                }
            }            
            return input;
        }
        public static string SignExtend(string bits, int newLength)
        {
            string Buffer = "";
            char SignBit = (bits[0] == '1') ? '1' : '0';
            for (int i = bits.Length; i < newLength; i++)
            {
                Buffer += SignBit;
            }
            return Buffer;
        }
        public static byte[] ZeroExtend(byte[] input, byte length) // http://prntscr.com/ofb1dy
        {
            Array.Resize(ref input, length);
            return input;
        }
    }
}
