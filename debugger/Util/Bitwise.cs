using System;
using debugger.Emulator;
namespace debugger.Util
{
    //Please note that throughout the explanation I deal exclusively with single bytes! Greater word values work the exact same but saves me
    //writing 0x7FFFFFFFFF.... all the time.
    public static class Bitwise
    {         
        public static byte[] ReverseEndian(byte[] input)
        {
            Array.Reverse(input); // Flip the byte order around
            return input;
        }
        public static byte[] Cut(byte[] input, int count) // time difference between this and linq.take is huge, 
        {                                                           // http://prntscr.com/od20o4 // linq: http://prntscr.com/od20vw  //my way: http://prntscr.com/od21br
            Array.Resize(ref input, count); // A performant way to cut any excess bytes off whilst at the same time ensuring the desired size
            return input;
        }
        public static byte[] Subarray(byte[] input, int offset)
        {
            byte[] Buffer = new byte[input.Length - offset]; // Create a new buffer with the size of the output
            Array.Copy(input, offset, Buffer, 0, input.Length - offset); // Copy every byte starting at index $offset from input into the buffer(like substring)
            return Buffer;
        }
        public static void PadEqual(ref string input1, ref string input2)
        {
            if (input1.Length > input2.Length) // If the length of input1 is greater than input2, input2 needs to be padded
            {
                input2 = SignExtend(input2, input1.Length);
            }
            else if (input2.Length > input1.Length) //Otherwise do the opposite(or nothing if they are equal)
            {
                input1 = SignExtend(input1, input2.Length);
            }
        }
        public static void PadEqual(ref byte[] input1, ref byte[] input2)
        {
            if (input1.Length > input2.Length) // If the length of input1 is greater than input2, input2 needs to be padded
            {
                input2 = SignExtend(input2, (byte)input1.Length);
            }
            else if (input2.Length > input1.Length) //Otherwise do the opposite(or nothing if they are equal)
            {
                input1 = SignExtend(input1, (byte)input2.Length);
            }
        }
        public static FlagSet Negate(byte[] input1, out byte[] Result)
        {
            // To negate is an equivalent operation as to XOR by -1,
            // In layman terms, each bit will be flipped, providing a twos compliement equivalent(be it positive or negative)
            FlagSet ResultFlags = Xor(input1, SignExtend(new byte[] { 0xFF }, (byte)input1.Length), out Result);
            if (input1.IsZero())
            {
                ResultFlags.Carry = FlagState.ON;
            }
            return ResultFlags;
        }
        public static FlagSet Or(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits); // Convert the input into bit-like strings to abstract the need for working with multiple byte arrays
            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' || input2Bits[i] == '1') // Here use a logical OR, if either input1[i] or input2[i] == '1' then result[i] == '1'
                {
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits); // Convert the bit-like string back into bytes
            return new FlagSet(Result) // Carry and overflow are always cleared in bitwise boolean operations
            {
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
            };
        }
        public static FlagSet And(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits); // Convert the input into bit-like strings to abstract the need for working with multiple byte arrays

            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' && input2Bits[i] == '1') // Here use a logical AND, if input1[i] and input2[i] == '1' then result[i] = '1'
                {
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits); // Convert the bit-like string back into bytes
            return new FlagSet(Result) // Carry and overflow are always cleared in bitwise boolean operations
            {
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
            };
        }
        public static FlagSet Xor(byte[] input1, byte[] input2, out byte[] Result)
        {
            string input1Bits = GetBits(input1);
            string input2Bits = GetBits(input2);
            PadEqual(ref input1Bits, ref input2Bits); // Convert the input into bit-like strings to abstract the need for working with multiple byte arrays

            string Bits = "";
            for (int i = 0; i < input1Bits.Length; i++)
            {
                if (input1Bits[i] == '1' ^ input2Bits[i] == '1') // Here use a logical OR, if input1[i] is not the same as input2[i] then result[i] = input1[i] or input2[i]
                { 
                    Bits += "1";
                }
                else
                {
                    Bits += "0";
                }
            }
            Result = GetBytes(Bits); // Convert the bit-like string back into bytes
            return new FlagSet(Result) // Carry and overflow are always cleared in bitwise boolean operations
            {
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
                Auxiliary = FlagState.UNDEFINED
            };
        }
        public static FlagSet Add(byte[] input1, byte[] input2, int size, out byte[] Result, bool carry = false)
        {
            input1 = SignExtend(input1, (byte)size);
            input2 = SignExtend(input2, (byte)size); // First ensure both operands are both the intended size so we can iterate both in parallel later
            Result = new byte[size];
            for (int i = 0; i < size; i++)                                     // Instead of using built-in methods, using my own algorithm compatible with byte arrays increased performance 
            {// V this must be an integer because we anticipate values > 0xFF  // on critical operations that are frequently used. http://prntscr.com/ojwfs2
                int sum = input1[i] + input2[i] + (carry ? 1 : 0); // If we carried on the previous byte(or operation even, if the carry was set in the method call), this is when we would add that
                if (sum > 0xFF) //overflowed that index            // carry. Consider 9 + 1, the least significant digit overflows to 0 whilst the 1 carries to the next. It is impossible for the carry
                {                                                  // to represent a value greater than one in the next column(e.g 9+9=18), at least in non-floating point arithmetic.
                    Result[i] += (byte)(sum % 0x100);                       // Leftover value stays in the current column. For example, 9 + 5, the left over value is 4, making 14. The "left over" can be calculated by 
                    carry = true;//We overflowed therefore carry to the next// taking the modulo of the new sum and the greatest amount a byte can represent + 1. Literally, we are dividing the sum and finding the remainder.
                }
                else
                {
                    Result[i] += (byte)sum; // Otherwise we add normally, nothing special.
                    carry = false; // If there was a carry, we remove it.
                }
            }
            return new FlagSet(Result)
            {
                Carry = carry ? FlagState.ON : FlagState.OFF, // If the last iteration carried, the addition is incomplete. Setting the flag allows the developer to handle this.
                Overflow = (input1.IsNegative() == input2.IsNegative() && Result.IsNegative() != input1.IsNegative()) ? FlagState.ON : FlagState.OFF,// If we added two numbers and got a result with a different sign, this could screw us over if we are doing signed addition.
                Auxiliary = ((input1[0] & 0b111) + (input2[0] & 0b111)) > 0b111 ? FlagState.ON : FlagState.OFF                                       // For example, if I add two numbers A and B to make C, if both A and B were positive, z should also be positive. Now if I
                //The auxiliary flag is mostly a compatability feature for older programs that use binary coded decimal to represent                 // wanted to add A and -B, we could never have a situation where we get an incorrect sign due to twos compliment. Say I take
                //numbers with decimal places(non-integers). This used the lower nibble of a word to represent decimal digits.                                     // the largest negative byte 0x80 and add the largest positive 0x7F, I get 0xFF, which proves the sum can never overflow to 0x100.
            };  //Shortly put, BCD did not work too well with binary, the lower nibble could only be used to represent 1-9, any greater
        }       //would be invalid and undefined behaviour. To align with accurate emulation, I have implemented the flag, even though
                //BCD isn't actually supported in 64bit mode of a x86-64 processor. It is possible an end-user is dealing with BCD
                //through their own implementation--my program can do just that.
                //To check if the auxiliary flag is set, I mask the first byte of both inputs to get the lower 3 bits. If the sum of
                //these bits its greater than 0b111, or 7, it is clear that we overflowed into the 4th bit.
        public static FlagSet Subtract(byte[] input1, byte[] input2, int size, out byte[] Result, bool borrow = false)
        {
            input1 = SignExtend(input1, (byte)size);
            input2 = SignExtend(input2, (byte)size); // First ensure both operands are both the intended size so we can iterate both in parallel later
            Result = new byte[size];
            for (int i = 0; i < size; i++)
            {
                int sum = input1[i] - input2[i] - (borrow ? 1 : 0); // Subtract the two, if we borrowed already, take off another. (Remember this borrowed value would represent 0xFF in the lower adjacent column)
                if (sum < 0)                                        // or more mathematically, 0x1(1 bit was borrowed) << 8(move this borrowed bit to the MSB of the byte beneath it)
                {                                                   // To show this graphically, consider the values in brackets as bit representations of bytes
                                                                    //                                Borrow    
                    Result[i] = (byte)(sum + 256);//Also applies->  // [..0] [00000001] [01000000] [..]  --> [..] [00000000] [10100000] [..]   
                    borrow = true;                                  // As you can see ^ this bit seemingly shifted down onto the next byte. This is purely just an implementation of such,
                }                                                   // however by tagging this onto the sum, I avoid having to create a nested loop for this sub-subtraction(in the scenario
                else                                                // that multiple borrows are needed, consider the long subtraction for base 10 numbers,
                {                                                   //   1 1
                    Result[i] = (byte)sum;//No extra calculation    //  1 0 0 -
                    borrow = false;       // was needed, input1[i]  //      1
                }                         // was greater than       //  -----
            }                             // input2[i]              //  0 9 9
            //carry, sign changed                                   // as you can see, I needed to borrow twice to get the 1 
            //overflow, negative - positive = positive
            return new FlagSet(Result) // negative - positive = positive   or positive - negative = negative
            {
                Carry = borrow ? FlagState.ON : FlagState.OFF, // Was there a borrow left over from the last loop? Then our subtraction isn't complete.
                                                               // Now its up to the developer to handle the rest, e.g use SBB on the upper bytes.
                Overflow = input1.IsNegative() != input2.IsNegative() && Result.IsNegative() != input1.IsNegative() ? FlagState.ON : FlagState.OFF,//Similar to with add, but the opposite. If I subtract a positive and a negative and end up with a negative, something went wrong, for more explanation see add().
                Auxiliary = ((input1[0] & 0b1000) | (input2[0] & 0b1000)) == (Result[0] & 0b1000) ? FlagState.OFF : FlagState.ON 
            }; // ^ A little harder to deduce than add, but if bit 4 was on in either input1 or input2, in a BCD operation, we need to know
        }      // if this state was preserved in the result. If the expression evaluates false, that means there was initially bit 4 set
               // in one of the operands, which was then lost in the result, or vice versa.
        public static void Divide(byte[] dividend, byte[] divisor, bool signed, int size, out byte[] Quotient, out byte[] Modulo)
        {
            //Unfortunately my divide implementation is almost completely impractical. On a latest-gen £2000 computer, it took around
            //40 minutes to evaluate the division of a 128bit dividend by a 64bit divisor. This is absolutely not ideal for the end user
            //nor do I intend to target the program at such expensive hardware. For compatibility and the patient, I have left the
            //algorithm and opcodes available. Dword/word and lower worked almost instantaneously(Would also work the same for operands
            //with larger capacities holding smaller values). Now ask, why not resort to using native methods such as BitConverter.ToUInt64?
            //This is not possible due to the nature of division on x86-64. If you take a look at page 292 of chapter 3 volume 2a in the Intel x86-64 manual, 
            //you will find that the dividend is twice the value of the divisor(due to methods explained in other parts of the source. Try SplitRegisterHandle.cs). 
            //This means that it in the case of 128 bit division, there will be no c# native way of doing this.(I would even imagine the same for any language that
            //doesn't have control of registers). The next option would of course be another algorithm, which honestly I cannot think of, I'm sure there
            //is but I want to keep my source as my own creation, anything else becomes a jungle to maintain. This is open source software, if you want it added,
            //add it.
            
            Quotient = new byte[size / 2];
            Modulo = new byte[size / 2];
            if (divisor.IsZero()) // Are we trying to divide by zero? This would be bad, so throw a divide error.
            {
                ControlUnit.RaiseException(Logging.LogCode.DIVIDE_BY_ZERO);
                return;
            }
            bool LastSign = dividend.IsNegative(); // Get the sign of the dividend. This will be useful shortly.(Negative == true)
            bool NegativeInput = signed && LastSign; // Are we doing signed division? If so are we dividing a negative, or just a really big unsigned number?
            if(NegativeInput) // If this is signed division, I don't want to deal with negatives! More importantly, at least with this algorithm, it can't.
            {                 // Why? I use a repeated subtraction method to divide akin to long divison on paper. It's obvious when to stop subtracting on paper
                              // but a computer cannot see obvious! I have to tell it the rules for such. However for now I will just make it positive, not
                              // neccesarily negate it, because I am only turning the last bit off, not flipping every bit. Now I can forget about the sign until
                              // after division and determine it afterwards. This way I can stick to the rule of "If the dividend becomes negative, I subtracted
                              // too much and need to go back, and leave the rest as a remainder".
                dividend[dividend.Length - 1] &= 0b01111111;
                LastSign = false;
            }        
            //Going forward, think of every number as unsigned. The need for signed division specific operation has been abstracted from the imminent
            //while loop.
            while (true) // When I reach the end of the loop iteration, I dont know whether I can subtract again, I need to do the subtraction first then
            {            // see if its less than zero or not, if it is then we break. This is the simplest way to do so, unfortunately leaving a while true loop.
                byte[] Buffer; // 1. Create a temporary buffer where I store the result of the subtraction
                Subtract(dividend, divisor, size, out Buffer);
                bool NewSign = Buffer.IsNegative(); // 2. Is the buffer negative?
                if (LastSign == NewSign || LastSign) // If these had two different signs, e.g subtracting went below 0, there is a good chance we are finished dividing.
                {
                    LastSign = NewSign; // See a few lines below
                    dividend = Buffer; // We verified that our subtraction produced a positive, now we can commit this to the dividend.
                    Increment(Quotient, size, out Quotient); // We just divided by the divisor once, so we ned to reflect this in the quotient.
                    //Now lets address the elephant in the room. I said treat the numbers as unsigned, so that means the sign should just be completely
                    //ignored, so why am I using it in unsigned arithmatic? There is a good reason. I can still take advantage of overflows to tell me
                    //about the state of an unsigned number. If I subtract 0x1 from 0x0, I get 0xFF right? That still tells me that I overflowed regardless
                    //of signed or unsigned interpretation, and thats really all it is, interpretation. The result of signed/unsigned operations is rarely affected
                    //by the sign of the inputs, for example when I add two unsigned numbers, I would always just ignore the overflow flag. Its still set, the processor
                    //doesn't know whether I'm calculating signed or unsigned numbers, it's just there if I want it. However it is not all that simple. What if I start
                    //with an unsigned number, that is a negative signed number, such as 0x80? This is where the logical OR in the if statement comes into play.
                    //If LastSign is true at any time, I know I'm dealing with a large unsigned number, because I already eliminated the possibility of a negative
                    //signed number. 
                    //Therefore, if LastSign is ever true, that number is going to go through three phases
                    // 1. A large unsigned number that COULD be interpreted as a negative signed number, LastSign = true
                    // 2. Become a number < 0x80 such that it can no longer be interpreted as negative, LastSign=NewSign makes this false, " || LastSign" let this get into the if block
                    // 3. Once again, become a number that is unsigned, but could be interpreted as a negative signed number, NewSign=true LastSign=false = division done!
                    //Any unsigned number that cannot be interpreted as a signed negative will only go through phases 2 and 3. 
                    //This three phase process ensures I always always always know when to stop subtracting. 
                    //(Technically since I never check for equality of the dividend and 0, the loop will always be ran once more than necessary, this is negligible and doesn't affect the result)
                }
                else
                {
                    break;                
                }
            }
            if (NegativeInput ^ Quotient.IsNegative())
            {
                Negate(Quotient, out dividend);
            }
            Array.Copy(dividend, Modulo, size / 2);       
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
            Result = new byte[size];
            Array.Copy(input, Result, input.Length);
            bool CarryBit3 = ((input[0] & 0b111) + 1) > 0b111; // if we will carry into bit 4 of 1st byte
            for (int i = 0; i < Result.Length; i++)
            {
                if (Result[i] < 0xFF)
                {
                    Result[i]++;
                    break;
                }
                else
                {
                    Result[i] = 0;
                }
            }
            return new FlagSet(Result)
            {
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,//if we increased it and got a negative
                Auxiliary = CarryBit3 ? FlagState.ON : FlagState.OFF,
            };
        }
        public static FlagSet Decrement(byte[] input, int size, out byte[] Result)
        {
            Result = new byte[size];
            Array.Copy(input, Result, input.Length);

            for (int i = 0; i < Result.Length; i++)
            {
                if (Result[i] > 0x00)
                {
                    Result[i]--;
                    break;
                }
                else
                {
                    Result[i] = 0xFF;
                }
            }
            return new FlagSet(Result)
            {
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,//if we increased it and got a negative
                Auxiliary = (input[0] & 0b1000) == (Result[0] & 0b1000) ? FlagState.OFF : FlagState.ON
            };
        }
        public static FlagSet ShiftLeft(byte[] input, byte count, int size, out byte[] Result)
        {
            count &= (byte)((size == 8) ? 0b00111111 : 0b00011111); // shifting more than 0x3f times in 64bit ins or 0x1f otherwise, isnt allowed, in reality is cut down for performance
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
        public static FlagSet ShiftRight(byte[] input, byte count, int size,  out byte[] Result, bool arithmetic)
        {
            count &= (byte)((size == 8) ? 0b00111111 : 0b00011111); // shifting more than 0x3f times in 64bit ins or 0x1f otherwise, isnt allowed, in reality is cut down for performance
            int StartCount = count; 
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
        public static FlagSet RotateLeft(byte[] input, byte bitRotates, RegisterCapacity size, bool useCarry, bool carryPresent, out byte[] Result)
        {
            byte StartCount;
            if(useCarry)
            {
                StartCount = size switch
                {
                    RegisterCapacity.BYTE => (byte)((bitRotates & 0x1F) % 9),
                    RegisterCapacity.WORD => (byte)((bitRotates & 0x1F) % 17),
                    RegisterCapacity.DWORD => (byte)(bitRotates & 0x1F),
                    RegisterCapacity.QWORD => (byte)(bitRotates & 0x3F),
                    _ => throw new Exception(),
                };
            }
            else
            {
                StartCount = (byte)(bitRotates & ((size == RegisterCapacity.QWORD) ? 0x3F : 0x1F));
            }            
            Result = new byte[(int)size];
            if (StartCount == 0) { return new FlagSet(); }
            Array.Copy(input, Result, input.Length);
            bool Carry = carryPresent;
            byte tempMSB = (byte)((carryPresent && useCarry) ? 1 : 0);
            for (byte RotateCount = 0; RotateCount < StartCount; RotateCount++)
            {                
                for (int i = 0; i < (int)size; i++)
                {
                    byte Mask = tempMSB;
                    tempMSB = MSB(Result[i]);
                    Result[i] = (byte)((Result[i] << 1) | Mask);
                    
                }
                if (useCarry)
                {
                    Result[0] |= (byte)(Carry ? 1 : 0);
                    Carry = tempMSB > 0;                    
                }
                else
                {
                    Result[0] |= tempMSB;
                }
                tempMSB = 0;
            }
            FlagSet ResultFlags = new FlagSet();
            if(useCarry)
            {
                ResultFlags.Carry = Carry ? FlagState.ON : FlagState.OFF;
            }
            else
            {
                ResultFlags.Carry = LSB(Result) > 0 ? FlagState.ON : FlagState.OFF;
            }
            if(StartCount == 1)
            {
                ResultFlags.Overflow = (MSB(Result) ^ (ResultFlags.Carry == FlagState.ON ? 1 : 0)) == 0 ? FlagState.OFF : FlagState.ON;
            }                        
            return ResultFlags;
        }
        public static FlagSet RotateRight(byte[] input, byte bitRotates, RegisterCapacity size, bool useCarry, bool carryPresent, out byte[] Result)
        {
            byte StartCount;
            if (useCarry)
            {
                StartCount = size switch
                {
                    RegisterCapacity.BYTE => (byte)((bitRotates & 0x1F) % 9),
                    RegisterCapacity.WORD => (byte)((bitRotates & 0x1F) % 17),
                    RegisterCapacity.DWORD => (byte)(bitRotates & 0x1F),
                    RegisterCapacity.QWORD => (byte)(bitRotates & 0x3F),
                    _ => throw new Exception(),
                };
            }
            else
            {
                StartCount = (byte)(bitRotates & ((size == RegisterCapacity.QWORD) ? 0x3F : 0x1F));
            }
            Result = new byte[(int)size];
            if (StartCount == 0) { return new FlagSet(); }
            Array.Copy(input, Result, input.Length);
            bool Carry = carryPresent;
            byte tempLSB = (byte)((carryPresent && useCarry) ? 0b10000000 : 0);
            for (byte RotateCount = 0; RotateCount < StartCount; RotateCount++)
            {
                for (int i = (int)size-1; i >= 0; i--) // a222222219999108 -> d11111110cccc884
                {
                    byte Mask = tempLSB;
                    tempLSB = (byte)(LSB(Result[i]) << 7);
                    Result[i] = (byte)((Result[i] >> 1) | Mask);
                }
                if (useCarry)
                {
                    Result[Result.Length-1] |= (byte)(Carry ? 0b10000000 : 0);
                    Carry = tempLSB > 0;
                }
                else
                {
                    Result[Result.Length-1] |= tempLSB;
                }
                tempLSB = 0;
            }
            FlagSet ResultFlags = new FlagSet();
            if (useCarry)
            {
                ResultFlags.Carry = Carry ? FlagState.ON : FlagState.OFF;
            }
            else
            {
                ResultFlags.Carry = MSB(Result) > 0 ? FlagState.ON : FlagState.OFF;
            }
            if (StartCount == 1)
            {
                ResultFlags.Overflow = (MSB(Result) ^ GetBit(Result, Result.Length*8-1)) == 0 ? FlagState.OFF : FlagState.ON;
            }
            return ResultFlags;
        }
        public static (byte,byte) GetBitMask(byte bit) => ((byte)(bit/8), (byte)(0x80 >> bit % 8));
        public static byte GetBit(byte[] input, int bit) => (byte)(input[bit / 8] & (0x80 >> bit % 8));
        public static byte MSB(byte input) => (byte)(input >> 7);
        public static byte MSB(byte[] input) => (byte)(input[input.Length - 1] >> 7);//readable i guess? some reason its a good idea
        public static byte LSB(byte input) => (byte)(input & 1);
        public static byte LSB(byte[] input) => (byte)(input[0] & 1); 
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
