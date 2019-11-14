// The Bitwise class provides many efficient algorithms for working with bytes. In general, they are faster than converting to integers.
// Explaning how algorithms work is also an essential part of understanding assembly. There is a lot of behaviour that is not best 
// emulated by native functions, such as handling outputs. Assembly operations are generally not just two-in one-out functions,
// there are many things to consider, such as flags and sizes of results. There is a lot more too ADD EAX, EBX than you may expect.
// High level languages tend to handle this for the developer, which unfortunately distances the developer from what they are really doing.
// Byte arrays are used throughout the program as alternatives to bits(which C# gives hardly enough control over). They work exactly as
// base 256 numbers stored in little endian. As a side effect, numbers of effectively any size can now be worked on rather than any limitations
// on primitive types. A downside of this would of course be that byte arrays are stored on the heap. General access times for values will be 
// considerably slower than in a ulong for example. However, it would be incredibly unrepresentative if I did not outline the benefits, that
// I absolutely believe outweigh said drawback. 
// Initially, primitive types were used. For reference, see VM.cs in https://github.com/koreanair/debugger/commit/cdae7fa9ef90d62351f4147d22645f4dd54ee4c5
// Benefits
//  - A general method can be applied to any input
//  - Using byte arrays allows me to have much greater control over any algorithm. For example, in Add(), the CF is calculated as a byproduct of
//    the algorithm itself. No additional masking is required afterwards. Control over bits would have allowed this for the AF.
//  - To demonstrate how an addition could be done on hardware. A competent programmer could take any algorithm here and implement it with logic
//    gates.
//  - If all values were stored as ulongs, lots of memory would be wasted if the input was only of WORD size.
//  - Otherwise, methods would be needed for every size of integer. There would be little way to overcome this without the former. A work around this
//    could be to use dynamic types. This was my first thought, until I discovered that dynamic data types were about 3000x slower.
// Here is an overview of performance comparisons between options. http://prntscr.com/pbvrar
//  dynamic : 00:00:00.0046894
//  ulong   : 00:00:00.0000001
//  byte    : 00:00:00.0000016
//  This is not truly representative of the ulong speed, I would imagine it to be at least 10x faster, but the stopwatch reached its resolution.
// However a lot of this time is made back up for in other parts of the program. For example, no conversion is required after bytes are stored in
// a MemorySpace. MemorySpace is probably the most efficient way of doing its job, unsafe code could maybe have something going for it if the data
// stored in MemorySpace was large enough. However, it is non negotiable that storing memory in bytes is the only sane implementation of an address table.
// This means that if byte arrays were not used all the time, fetched memory would have to be converted with BitConverter with every operation that uses memory.
// To put this into perspective, it now becomes slightly slower than seen with a byte[] http://prntscr.com/pbw6su
//  Converted : 00:00:00.0000019
//  Primitive : 00:00:00.0000002
// This is still very generous towards using ulongs because every time a DWORD, WORD, or BYTE is fetched from memory, it would have to be zero extended in order for
// BitConverter.ToUInt64() work. Overall, it seems pretty clear now that byte arrays outperform in every aspect. Maybe its not so much that byte arrays are "faster",
// just that any other data type would require other operations elsewhere which in turn makes them slower.

// Note that throughout the explanations I deal almost exclusively with single bytes unless specified otherwise.  Comments may not
// exactly 1:1 apply to other word lengths, but saves me writing 0x7FFFFFFFFF.... all the time etc.
using debugger.Emulator;
using System;
namespace debugger.Util
{
    public static class Bitwise
    {
        public static bool IsNegative(this byte[] input)
        {
            // Two tricks for determining if a byte array is negative or not.
            // Firstly, if $input.Length is not a power of two, it cannot be negative. This could be the case where another method returned the least number of bytes in $input possible,
            //  e.g instead of returning { [0x22] [0x44] [0x66] [0x88] [0xAA] [0x00] [0x00] [0x00] }, it returned, { [0x22] [0x44] [0x66] [0x88] [0xAA] } (cut of the trailing null bytes),
            //  it would appear that the byte array represented a negative number because the MSByte (0xAA) has its sign bit(MSB) set, [10101010].
            // This trick works by doing a bitwise AND on $input.Length and $input.Length - 1. This works because any number that is a power of two has one bit set, and a number that is one
            // less than a power of two has all bits lower than said power of two set.
            // For example, 
            //  0x08 = [1000] 
            //  0x07 = [0111] 
            //  0x08 & 0x07 = [0000]
            //  or,
            //  0x80 = [10000000]
            //  0x7F = [01111111]
            // The second trick is simply to check if the MSB of $input is on. Masking by 0x80([10000000]) does exactly that. Naturally as seen in the example,
            // this wouldn't work if the previous trick hadn't been done prior.
            return (input.Length & input.Length - 1) == 0 && (input[input.Length - 1] & 0x80) == 0x80;
        }
        public static bool IsZero(this byte[] input)
        {
            // Iterate through every byte in the array, checking if it is zero. 
            for (int i = 0; i < input.Length; i++)
            {
                // If any index in $input is not equal to zero, the byte array cannot represent a zero.
                if (input[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        public static byte[] ReverseEndian(byte[] input)
        {
            // Flip the byte order around
            Array.Reverse(input);

            return input;
        }
        public static byte[] Cut(byte[] input, int count)
        {
            // Time difference between this and linq.take is huge, http://prntscr.com/od20o4 
            // linq: http://prntscr.com/od20vw  
            // my way: http://prntscr.com/od21br

            // A performant way to cut any excess bytes off whilst at the same time ensuring the desired size
            Array.Resize(ref input, count);
            return input;
        }
        public static byte[] Subarray(byte[] input, int offset)
        {
            // Create a new buffer with the size of the output
            byte[] Buffer = new byte[input.Length - offset];

            // Copy every byte starting at index $offset from input into the buffer(like substring)
            Array.Copy(input, offset, Buffer, 0, input.Length - offset);
            return Buffer;
        }
        public static void PadEqual(ref byte[] input1, ref byte[] input2, bool signed)
        {
            // If the length of input1 is greater than input2, input2 needs to be padded
            if (input1.Length > input2.Length)
            {
                if (signed)
                {
                    input2 = SignExtend(input2, (byte)input1.Length);
                }
                else
                {
                    input2 = ZeroExtend(input2, (byte)input1.Length);
                }
            }
            //Otherwise do the opposite(or nothing if they are equal)
            else if (input2.Length > input1.Length)
            {
                if (signed)
                {
                    input1 = SignExtend(input1, (byte)input2.Length);
                }
                else
                {
                    input1 = ZeroExtend(input1, (byte)input2.Length);
                }
            }
        }
        public static FlagSet Negate(byte[] input1, out byte[] Result)
        {
            
            // To negate is an equivalent operation as to XOR by -1 and add one.

            // Simply, each bit will be flipped
            Xor(input1, SignExtend(new byte[] { 0xFF }, (byte)input1.Length), out Result);

            // Add one to finish the twos complement negation.
            FlagSet ResultFlags = Increment(Result, out Result);

            if (input1.IsZero())
            {
                ResultFlags.Carry = FlagState.ON;
            }
            return ResultFlags;
        }
        public static FlagSet Or(byte[] input1, byte[] input2, out byte[] Result)
        {
            Result = new byte[input1.Length];

            // Perform a series of logical ANDs on every byte in $input1[] and $input2[], store the result in $Result
            for (int ByteCursor = 0; ByteCursor < input1.Length; ByteCursor++)
            {
                Result[ByteCursor] = (byte)(input1[ByteCursor] | input2[ByteCursor]);
            }
            return new FlagSet(Result)
            {
                // Carry and overflow are always cleared in bitwise boolean operations
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
            };
        }
        public static FlagSet And(byte[] input1, byte[] input2, out byte[] Result)
        {
            Result = new byte[input1.Length];

            // Perform a series of logical ANDs on every byte in $input1[] and $input2[], store the result in $Result
            for (int ByteCursor = 0; ByteCursor < input1.Length; ByteCursor++)
            {
                Result[ByteCursor] = (byte)(input1[ByteCursor] & input2[ByteCursor]);
            }
            return new FlagSet(Result)
            {
                // Carry and overflow are always cleared in bitwise boolean operations
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
            };
        }
        public static FlagSet Xor(byte[] input1, byte[] input2, out byte[] Result)
        {
            Result = new byte[input1.Length];

            // Perform a series of logical XORs on every byte in $input1[] and $input2[], store the result in $Result
            for (int ByteCursor = 0; ByteCursor < input1.Length; ByteCursor++)
            {
                Result[ByteCursor] = (byte)(input1[ByteCursor] ^ input2[ByteCursor]);
            }
            return new FlagSet(Result)
            {
                // Carry and overflow are always cleared in bitwise boolean operations
                Carry = FlagState.OFF,
                Overflow = FlagState.OFF,
            };
        }
        public static FlagSet Add(byte[] input1, byte[] input2, out byte[] Result, bool carry = false)
        {
            // The two inputs must be the same length.
            Result = new byte[input1.Length];

            // Instead of using built-in methods, I use my own algorithms compatible with byte arrays increased performance 
            // on critical operations that are frequently used. http://prntscr.com/ojwfs2
            for (int i = 0; i < Result.Length; i++)
            {
                // Declare a sum integer, which must be an integer because I anticipate values > 0xFF, which are handled using carries shortly.
                // If there was a carry on the previous byte in the array(or operation even, if the CF was set), add that to the sum.
                // Consider 9 + 1, the least significant digit overflows to 0 whilst the 1 carries to the next. It is impossible for the carry
                // to represent a value greater than one in the next column(e.g 9+9=18) in addition.
                int sum = input1[i] + input2[i] + (carry ? 1 : 0);

                // If the sum was greater than 0xFF, it can't be stored in the byte array without losing its value
                if (sum > 0xFF)
                {
                    // Leftover value stays in the current column. For example, 9 + 5, the left over value is 4, making 14. The "left over" can be calculated by 
                    // taking the modulo of the new sum and the greatest amount a byte can represent + 1. Literally, dividing the sum and finding the remainder.                    
                    Result[i] += (byte)(sum % 0x100);

                    // Then account for this overflow by carrying to the next.
                    carry = true;
                }
                else
                {
                    // Otherwise add normally, nothing special.
                    Result[i] += (byte)sum;

                    // If there was a carry, turn it off because it was already accounted for when the sum was calculated.
                    carry = false;
                }
            }
            return new FlagSet(Result)
            {
                // If the last iteration carried, the addition is incomplete. Setting the flag allows the developer to handle this.
                Carry = carry ? FlagState.ON : FlagState.OFF,

                // If two numbers were added and the result had different sign, this could screw us over if we are doing signed addition.
                // For example, if I add two numbers A and B to make C, if both A and B were positive, C should also be positive. So if this is
                // the case, tell the developer by setting the OF.
                // Now if I wanted to add A and -B, there would never be a situation where the sign is incorrect. This is due to twos compliment. Say I take
                // the largest negative byte 0x80 and add the largest positive 0x7F, I get 0xFF, which proves the sum can never overflow to 0x100.
                Overflow = (input1.IsNegative() == input2.IsNegative() && Result.IsNegative() != input1.IsNegative()) ? FlagState.ON : FlagState.OFF,

                // The auxiliary flag is mostly a compatability feature for older programs that use binary coded decimal to represent        
                // numbers with decimal places(non-integers). This method used the lower nibble of a word to represent decimal digits.              
                // Shortly put, BCD was not too efficient in terms of numbers, the lower nibble could only be used to represent 1-9, any greater
                // would be invalid and had undefined behaviour. To align with accurate emulation, I have implemented the flag, even though
                // BCD is actually only supported when the x86-64 processor is in compatibility mode. It is possible an end-user is dealing with BCD
                // through their own implementation--my program can do just that.
                // To check if the auxiliary flag is set, I mask the first byte of both inputs to get the lower 3 bits. If the sum of
                // these bits its greater than 0b111, or 7, it is clear that there was an overflow into the 4th bit.
                Auxiliary = ((input1[0] & 0b111) + (input2[0] & 0b111)) > 0b111 ? FlagState.ON : FlagState.OFF
            };
        }
        public static FlagSet Subtract(byte[] input1, byte[] input2, out byte[] Result, bool borrow = false)
        {
            // The two inputs must be the same length
            Result = new byte[input1.Length];
            for (int i = 0; i < Result.Length; i++)
            {
                // Subtract the two, if we borrowed already, take off another. (Remember this borrowed value would represent 0xFF in the lower adjacent column)
                // or more mathematically, 0x1(1 bit was borrowed) << 8(move this borrowed bit to the MSB of the byte beneath it)
                // To show this graphically, consider the values in brackets as bit representations of bytes
                //                                Borrow    
                // [..0] [00000001] [01000000] [..]  --> [..] [00000000] [10100000] [..]   
                // As you can see ^ this bit seemingly shifted down onto the next byte. This is purely just an implementation of such,
                // however by tagging this onto the sum, I avoid having to create a nested loop for this sub-subtraction(in the scenario
                // that multiple borrows are needed, consider the long subtraction for base 10 numbers,
                //   1 1
                //  1 0 0 -
                //      1
                //  -----
                //  0 9 9
                // as you can see, I needed to borrow twice to get the 1 
                int sum = input1[i] - input2[i] - (borrow ? 1 : 0);
                if (sum < 0)
                {

                    Result[i] = (byte)(sum + 256);
                    borrow = true;
                }
                else
                {
                    //No extra calculation was needed, input1[i] was greater than input2[i]              
                    Result[i] = (byte)sum;
                    borrow = false;
                }
            }
            return new FlagSet(Result)
            {
                // Was there a borrow left over from the last loop? Then our subtraction isn't complete.
                // Now its up to the developer to handle the rest, e.g use SBB on the upper bytes.
                Carry = borrow ? FlagState.ON : FlagState.OFF,

                // Similar to in Add(), but the opposite. If I subtract a negative from a positive and end up with a negative, something went wrong, for more explanation see Add().
                Overflow = input1.IsNegative() != input2.IsNegative() && Result.IsNegative() != input1.IsNegative() ? FlagState.ON : FlagState.OFF,

                // A little harder to deduce than add, but if bit 4 was on in either input1 or input2, in a BCD operation, I need to know
                // if this state was preserved in the result. If the expression evaluates false, that means there was initially bit 4 set
                // in one of the operands, which was then lost in the result, or vice versa.
                Auxiliary = ((input1[0] & 0b1000) | (input2[0] & 0b1000)) == (Result[0] & 0b1000) ? FlagState.OFF : FlagState.ON
            };
        }
        public static void Divide(byte[] dividend, byte[] divisor, int size, bool signed, out byte[] Quotient, out byte[] Modulo)
        {
            // Unfortunately my divide implementation is almost completely impractical. On my computer, it took around
            // 4 minutes to evaluate the division of a 128bit dividend by a 64bit divisor. This is absolutely not ideal for the end user
            // nor do I intend to target the program at such expensive hardware. For compatibility and the patient, I have left the
            // algorithm and opcodes available. Dword/word and lower worked almost instantaneously (Would also work the same for operands
            // with larger capacities holding smaller values). Now ask, why not resort to using native methods such as BitConverter.ToUInt64?
            // This is not possible due to the nature of division on x86-64. If you take a look at page 292 of chapter 3 volume 2a in the Intel x86-64 manual, 
            // you will find that the dividend is twice the size of the divisor(due to methods explained in other parts of the source. Try SplitRegisterHandle.cs). 
            // This means that it in the case of 128 bit division, there will be no c# native way of doing this.(I would even imagine the same for any language that
            // doesn't have control of registers). The next option would of course be another algorithm, which honestly I cannot think of, I'm sure there
            // is but I want to keep my source as my own creation, anything else becomes a jungle to maintain. This is open source software, if you want it added,
            // add it.

            Quotient = new byte[size];
            Modulo = new byte[size];

            // Dividing by zero would be bad, so throw a divide error.
            if (divisor.IsZero())
            {
                ControlUnit.RaiseException(Logging.LogCode.DIVIDE_BY_ZERO);
                return;
            }

            // Get the sign of the dividend. This will be useful shortly.(Negative == true)   
            // True if dividend is a signed negative
            bool LastSign = dividend.IsNegative();

            // Is this a signed division operation? If so, am I dividing a negative, or just a really big unsigned number?
            // True if and only if dividend is a signed negative
            bool NegativeDividend = signed && LastSign;

            // If this is signed division, I don't want to deal with negatives. More importantly, at least with this algorithm, it can't.
            // Why? I use a repeated subtraction method to divide akin to long divison on paper. It's obvious when to stop subtracting on paper
            // but a computer cannot see obvious! I have to tell it the rules for such. This would require a completely different algorithm that
            // runs if $NegativeDividend is true, and otherwise run my current algorithm. That seems insane, I would rather one algorithm that
            // I can understand and maintain easier. At any rate, for now I will just negate any negatives to get their absolute value
            // This way I can stick to the rule of "If the dividend becomes negative, I subtracted too much and need to go back
            // , and leave the rest as a remainder". Then can later determine the sign of the result based on the signs of the inputs.
            if (NegativeDividend)
            {
                Negate(dividend, out dividend);

                // This is a little extra hack that I will explain later, but an unsigned number does have cause to be percieved as a negative signed,
                LastSign = false;
            }
            bool NegativeDivisor = divisor.IsNegative();
            if (NegativeDivisor)
            {
                Negate(divisor, out divisor);
            }
            dividend = ZeroExtend(dividend, (byte)size);
            divisor = ZeroExtend(divisor, (byte)size);

            //Going forward, think of every number as unsigned. The need for signed division specific operation has been abstracted from the upcoming while loop.

            // When I reach the end of the loop iteration, I dont know whether I can subtract again, I need to do the subtraction first then
            // see if its less than zero or not, if it is then break. This is the simplest way to do so, unfortunately leaving an infamous while true loop.
            while (true)
            {
                // Create a temporary buffer where I store the result of the subtraction.
                byte[] Buffer;

                // Subtract the divisor from the dividend and store it in the buffer.
                Subtract(dividend, divisor, out Buffer);

                // Is the buffer negative? 
                bool NewSign = Buffer.IsNegative();

                // If these had two different signs, i.e dividend - divisor < 0, there is a good chance we are finished dividing.
                if (LastSign == NewSign || LastSign)
                {
                    LastSign = NewSign;

                    // Verify that the subtraction produced a positive and can be committed to the dividend.
                    // The former dividend array will then be disposed behind the scenes.
                    dividend = Buffer;

                    // A single step of the divison is complete, so this needs to be reflected in the quotient.
                    Increment(Quotient, out Quotient);

                    // Now,  I said treat the numbers as unsigned, so that means the sign should just be completely ignored, so why am I using it in unsigned arithmetic? 
                    // There is a good reason. I can still take advantage of overflows to tell me about the state of an unsigned number. If I subtract 0x1 from 0x0, I get 0xFF right? 
                    // That still tells me that I overflowed regardless of wheter the number is interpreted as signed or unsigned, and thats really all it is, interpretation. 
                    // The result of signed/unsigned operations is rarely affected by the sign of the inputs, for example when I add two unsigned numbers, I would always just ignore 
                    // the overflow flag. Its still set, the processor doesn't know whether I'm calculating signed or unsigned numbers, it's just there if I want it. 
                    // However it is not all that simple. What if I start with an unsigned number, that could also be seen as a negative signed number, such as 0x80? 
                    // This is where the logical OR in the if statement comes into play. If LastSign is true just after the subtraction took place, I know I'm dealing with a large unsigned number, 
                    // because I already eliminated the possibility of a negative signed number. 
                    // Therefore, if LastSign is ever true, that number is going to go through three "phases"
                    //  1. A large unsigned number that COULD be interpreted as a negative signed number, LastSign = true
                    //  2. Become a number < 0x80 such that it can no longer be interpreted as negative, LastSign=NewSign makes this false, " || LastSign" let this get into the if block
                    //  3. Once again, become a number that is unsigned, but could be interpreted as a negative signed number, but this time it's no false alarm,
                    //     NewSign =true LastSign=false = division done!
                    // Any unsigned number that cannot be interpreted as a signed negative will only go through phases 2 and 3. 
                    // This three phase process ensures I always always always know when to stop subtracting. 
                    // (Technically since I never check for equality of the dividend and 0, the loop will always be ran once more than necessary, this is negligible and doesn't affect the result)

                    // Could this method be applied to other arithmetic operators? Absolutely. Consider 10 + -1000, I could write this as -(1000-10). Fortunately because of twos compliment,
                    // this isn't necessary, I can rely on overflows to do the job for me. However if the numbers were encoded in one compliment or sign magnitude, this method would be needed
                    // on all signed arith operations.
                }
                else
                {
                    // This else indicates the number turned negative, or in context NewSign != LastSign and LastSign == false,
                    // therefore the temporary dividend stored in the buffer was subtracted below zero, which means it's time to stop subtracting now because the quotient was found. 
                    break;
                }
            }

            // Now I need to get the sign back if the number was initially to be interpreted as a negative signed number. Negative signed   
            // numbers work in the same way as signed numbers for most arithmetic, but not so with division(multiplication just requires different flags)
            // So to solve this problem, I just create the ideal scenario of a dividing a positive instead, but the result still needs to be converted back
            // to the form it was before. Firstly, I will cover the condition, as not every time will require a change back. If it was unsigned to begin with
            // , leave it be, It was never changed before the division. Secondly, XOR signs of both the dividend and divisor. This is where you need to
            // take a mathematical approach to the problem. Think about how signs affect a fraction.
            // Here I will make 3 statements that should be very obvious for you, but I will explain regardless for the KISS principle, and also to demonstrate
            // why this will work for any base number. The proof is trivial but in the links following.
            // (Assume an ideal scenario: no zeros, natural numbers)
            //  1. -x/y == -(x/y) http://prntscr.com/p8cymm
            //  2. -x/y == x/-y http://prntscr.com/p8ctdk
            //  3. -x/-y = x/y http://prntscr.com/p8cwrr
            // Therefore, this can be used to predict the sign of the outcome using the signs of the initial dividend and divisor
            // As statement one shows, if exclusively one of x or y are negative, the result is negative.
            // Statement two, the result is not dependent on which side of the fraction is negative.
            // Statement three, two negatives divided make a positive.
            // An XOR of the signs could be concluded from statement one, but statement 2 and 3 really make that clear.
            // So, to match statement 1(which statement two proves would be the same for x/-y) if there is exactly one negative input,
            // the result has to be negated, or if there are two negates, as statement three shows, do nothing.
            if (signed && (NegativeDividend ^ NegativeDivisor))
            {
                Negate(Quotient, out Quotient);
                Negate(dividend, out dividend);
            }

            // Size variable is the size of twice the result.      
            Array.Copy(dividend, Modulo, size / 2);
        }
        public static FlagSet Multiply(byte[] input1, byte[] input2, bool signed, int size, out byte[] Result)
        {
            // The inputs need to be twice the intended size during the multiplication because
            // the output is twice the length of the input, therefore I need to make sure that the
            // input arrays are long enough to be iterated over to complete the multiplication.
            input1 = signed ? SignExtend(input1, (byte)(size * 2)) : ZeroExtend(input1, (byte)(size * 2));
            input2 = signed ? SignExtend(input2, (byte)(size * 2)) : ZeroExtend(input2, (byte)(size * 2));

            // The result is going to be size*2, so to match the length of the inputs doubling, double the result length as well then resize it later.
            Result = new byte[size * 4];

            // Firstly I will explain the ideas behind this multiplication algorithm.
            // It acts in a very similar way to as if you wrote out long multiplication
            // For example if I wanted to multiply 123 by 45,
            // Consider it written out, http://prntscr.com/p8douj
            // Then consider these annotations http://prntscr.com/p8emib
            // Bear this process in mind when reading the code ahead, how this process takes place on paper.

            // Iterate over every number on the bottom row. Think of the digits 4 and 5 in the example
            for (int ColumnPos = 0; ColumnPos < size * 2; ColumnPos++)
            {
                // Take said digits then iterate over every digit in the top row.
                for (int BytePos = 0; BytePos < size * 2; BytePos++)
                {
                    // Times the bottom row digit at "ColumnPos" by the top row digit at "BytePos". 
                    int mul = (input1[ColumnPos] * input2[BytePos]) + Result[BytePos + ColumnPos];
                    // What is the meaning of Result[BytePos+ColumnPos]?
                    // Well, this serves a few purposes, remember how when I multiplied 123 and 45, when I moved on to the digit 4, 
                    // I added an extra zero in the CursorPos=1 column (http://prntscr.com/p8epnj) where I would write ther result of (1*4) + (2*4) + (3*4)
                    // This is doing exactly that, then add BytePos to give the horizontal movement in that row
                    // Mathematically this is accounting for the fact that the values in the current column have their
                    // full value "cut off", when multiplying 4 by the numbers in the BytePos row, really I multiply
                    // by 40, but adding the ColumnPos, we are shifting that number of indexes to get the number in the right column
                    // If this was on a long for example rather than an array, you could think of it as a bitwise shift.
                    // But why is the value of Result[BytePos+ColumnPos] added on? Remember how at the end of long multiplication,
                    // I add all the ColumnPos row results together to get 5535?, well this just removes that step doing that.
                    // The first ColumnPos iteration is stored in the result initially, then the next can just add on top of that
                    // to save extra computation time for adding at the end.
                    // Also, whats with the modulo? Well similar to adding, two bytes added can make a result greater than a single byte,
                    // However, when I added, I showed that the carry would also be one(9+9=18), but that is obviously not the case for
                    // multiplication, so for now, take the LSB and leave that in the result, deal with everything else shortly. 
                    Result[BytePos + ColumnPos] = (byte)(mul % 0x100);

                    //If the result was in fact greater than a single byte 
                    if (mul > 0xFF)
                    {
                        // This carry needs to be added to the result like a large number. Let Add() handle all the subcarries(carries within Result+Carry).
                        byte[] carry = new byte[size * 4];

                        // Set the current byte position + 1 to the value of the multiplication / by the smallest value a byte cannot be
                        // simply, how many times over the max value did we go, or the little superscript numbers shown in the annotated example.
                        carry[ColumnPos + BytePos + 1] = (byte)(mul / 0x100);

                        // Finally add the MSB here,(a carry). For example, A12B % 0x100 = 2B, A12B / 0x100 = A1(where / is integer division)
                        // 2B goes in the current byte position, A1 goes into the next.                                                     
                        Add(Result, carry, out Result);
                    }
                }
            }
            // Throughout this explanation I sort of missed out a crucial part that I hope you didn't notice. My "columns" or indexes in the byte array really represent
            // base 256 numbers not base 10. It doesn't actually change anything, but I would imagine it to be very confusing if I use long multiplication to explain my method
            // then put multiple "digits" in one column. It really is no different aside from the few corners I cut as mentioned before.

            // Resize back the array that I dont want(it was only used as extra space for carries as the inputs were resized initially)
            Array.Resize(ref Result, size * 2);
            byte[] UpperComparison = new byte[size * 2];

            // Copy the bottom bytes of result to upper comparison to guarentee both have the bottom half equal
            Array.Copy(Result, UpperComparison, size);

            // Fill the rest of the array with what unused upper bytes would look like, e.g FF,FF,FF,.. from a sign extension of a signed negative result or just 0,0,0,0,0,.. in any other case
            UpperComparison = (signed) ? SignExtend(UpperComparison, (byte)(size * 2)) : ZeroExtend(UpperComparison, (byte)(size * 2));

            // Then compare the the two, if the result isn't zero, that means those upper bytes were used
            bool UpperUsed = UpperComparison.CompareTo(Result, signed) != 0;

            // Intel's explanation of this reads 
            // "The CF and OF flags are set when the signed integer value of the intermediate product differs from the sign
            // extended operand-size-truncated product, otherwise the CF and OF flags are cleared."
            // If this explanation is harder to understand, consider the "intermediate product" to be the result
            // and the "sign extended operand-size-truncated product" to be the UpperComparison array.
            // Another much better explanation that was replaced by the December 2015 edition of the manual read,
            // "the CF and OF flags are set when significant bits are carried into the upper half
            //  of the result and cleared when the result fits exactly in the lower half of the result. For the two- and three-operand
            //  forms of the instruction, the CF and OF flags are set when the result must be truncated to fit in the destination
            //  operand size and cleared when the result fits exactly in the destination operand size. "
            // A purpose of this would be to say whether the MSB in the lower bytes reflect the true MSB.
            // This links back to how multiplication opcodes are implemented. There are basically two different scenarios that this affects,
            // 1. I want to multiply the A register by a R/M(Register or memory) of equal size, and store the lower bytes of that big number in the A register and the upper in D register,
            //    so this would tell me when the result in the A register is truncated and that I needed to check the D register to get the rest of my result.
            // 2. I multiplied a register by a R/M, but the result was too big so the value in the result register is truncated and incorrect. Obviously there is no solution to this because
            //    you would be asking for larger registers that didn't exist at that time. Filling up an arbitary register with your upper bytes after you specifically chose another register
            //    to put your answer in(other than the A register) would be a really crazy idea.

            // Never set PF or ZF. I dont know the reason intel decided this, but I would assume it is to do with the result being stored in two registers
            // (or atleast was the case when the implicit A register opcode was the only one to exist)
            // P.S strictly speaking the PF,AF, and ZF are "undefined". This means that a particular processor MAY set these based on something,
            // but it would be unreliable and very anti-portable to base your code on it.
            return new FlagSet()
            {
                // If the upper bytes of result were used, tell the developer              
                Overflow = UpperUsed ? FlagState.ON : FlagState.OFF,

                // Carry is set to the same as the overflow flag
                Carry = UpperUsed ? FlagState.ON : FlagState.OFF,

                //As of the May 2019 release of the documentation, setting the sign flag is now "undefined". 

                // If signed multiplication, get the sign of the truncated lower bytes in the lower half of the result
                //Sign = signed ? (Result[size - 1] > 0x7F ? FlagState.ON : FlagState.OFF) : FlagState.UNDEFINED 
            };
        }
        public static FlagSet Increment(byte[] input, out byte[] Result)
        {
            Result = new byte[input.Length];

            // Create a buffer and copy the result into. Why? This is a problem .NET implementation. Instead of
            // creating lists as object orientated counterparts to arrays, arrays are also instance based, and a
            // reference to the instance was passed when the method was called, not the whole array. Since we are
            // already thinking about low level code, it could be said(although the compiler is likely to do many
            // weird and wonderful things that may not be word for word what I say, but in theory), that a pointer 
            // to the "input" array is pushed on to the stack, then when I want to, say, change the byte at index
            // 3 in the array, I could do,
            //   mov byte ptr [rsp+3], 0x12
            // The offset being 3 because the array is a byte array, if it was an dword array,
            //   mov dword ptr [rsp+12], 0x78563412
            // The offset being 12 because the index I want is 3, but each element of the array is 4 bytes, 3*4=12            
            // Then once I return from the function, and I try to access my input again
            //   mov al, byte ptr [rsp+3](Consider a static stack frame)
            // Now I have 12 in $al, not what I had before. This really is more of a general .NET problem rather
            // than specific to my program, there is no scenario in assembly where this would affect anything,
            // but it's important to be aware of the framework you are programming in. There are scenarios which are
            // exactly this in other parts of this program that I wont spoil now, but hope you look forward to reading about.
            // Here is a short annotated demonstration written in C#: http://prntscr.com/p8tb5s
            Array.Copy(input, Result, input.Length);

            // A little more streamlined than add, but takes a different perspective.
            // Firstly I just increment the byte offset by $i.
            // From here, there are two situations that matter:
            //  1. Result[i] is zero
            //  2. Result[i] is not zero
            // If it is zero, well what natural number can be incremented to get zero?
            // None! Therefore, we know the byte overflowed. If the byte overflowed,
            // I need to make up for this by adding 1 to the next iteration. Just
            // like in addition. However, it takes a little intuition to see how this
            // can be done more effectively. If I want to add one to a number, and I
            // know that if I want to do this, I need to make sure I handle byte
            // overflows properly. Why not take advantage of them? If I add 1 to 9,
            // I don't get 19, I get 10. I let the 9 overflow to a 0 because now the
            // 1 in the second column holds the value. This implementation does exactly
            // that, then afterwards, I check, was it equal to 0x00? Because I know if it
            // is, the value before was 0xFF. If it wasn't 0xFF, that means it can safely
            // be incremented by one and I'm done!
            // But what if the input value is an array filled with 0xFF? Read ahead.
            for (int i = 0; i < Result.Length; i++)
            {
                Result[i]++;
                if (Result[i] != 0x00)
                {
                    break;
                }
            }
            return new FlagSet(Result)
            {
                // This overflow check is the exact same as checking if the input
                // byte array is equal to -1(All 0xFFs). This is the only case where
                // incrementing could give an invalid result, in that case, let the
                // developer know by setting the flag. They can handle it with
                // a conditional jump, or maybe they are iterating 256 times, and using
                // test ecx, ecx to detect an overflow. This really is why assembly
                // is awesome and puts try catch blocks to shame, errors can totally
                // be used to an advantage in the right hands.
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,

                // Another approach to setting the AF under the same condition in Add(). Since the second value to add is known, the equation can be
                // simplified. The auxiliary flag by definition is set when there is a carry into the 4th bit(not specifically
                // of the first byte, but due to how it is implimented in my program, it does happen to be the first byte). 
                // So think which values can cause a carry into the 4th bit. It must be seven right? Think about 999 + 1, the
                // answer is 1000 but how did you get there? All the columns before had to be at their highest possible value
                // ,9. This caused the 1 to carry over to a new column and set everything in between to 0 in the process.
                // To really break it down, let's break down the addition.
                //     Starting with the first column, 9 + 1 = 10. However I can't write 99[10], that's just now how numbers work
                //     , specifically, a number in a column cannot be the same value as it's base. Decimal numbers are base 10,
                //     so when I get 10 in an addition, I need to carry over. Binary is also called base 2 and works exactly the same.
                // Now it should be clear that the only value in the lower nibble that cannot take an extra '1' before carrying into the
                // 4th bit is 0b111. If I add 1 to 0b111, where would the 1 go? I can't write 0b112, that's not binary, so by carrying over
                // I get 0b1000, boom, our binary coded decimal was just ruined.
                Auxiliary = (input[0] & 0b111) == 0b111 ? FlagState.ON : FlagState.OFF
            };
        }
        public static FlagSet Decrement(byte[] input, out byte[] Result)
        {
            // See Increment()
            Result = new byte[input.Length];
            Array.Copy(input, Result, input.Length);

            // An almost identical to approach to Increment(). Instead, 0xFF is the buzz value. Think about
            // which number satisfies x - 1 = infinity. Nothing right? So instantly I know something went
            // 'wrong'. 0x00 must have been the input value decremented. Mathematically speaking, that value 
            // needs to be borrowed from the next column so it can be decremented.
            for (int i = 0; i < Result.Length; i++)
            {
                Result[i]--;
                if (Result[i] != 0xFF)
                {
                    break;
                }
            }
            return new FlagSet(Result)
            {
                // A flip scenario of in Increment(). Here, I'm looking for the input MSB to be 0x80 and the rest zero.
                // In twos complement, 0x80 00 00 .. 00 is the lowest number possible, therefore to take one from this
                // would result in the greatest number possible, 0x7F FF .. FF, which would be an invalid result. A negative 
                // minus one should always be negative, therefore to tell the developer, the OF is set.
                // However just like in Increment() there are absolutely scenarios where I can take advantage(Very similar ones too). 
                // Say I wanted a loop, instead of using two registers to represent the current
                // iterator value and the maximum iterator value, I could run code like..
                //  0x00 mov al, 0x31
                //  0x02 [Do stuff]
                //  ~
                //  0x20 dec al
                //  0x22 jo 0x00
                // (jo = jump if overflow) Look, I can loop round 0x32 times(because the flag is
                // set when the result is 0xFF not 0x00) without having to use a comparison operator
                // and since it's already calculated I would be wasting time and space if I didn't 
                // make use of it!
                Overflow = (Result.IsNegative() && !input.IsNegative()) ? FlagState.ON : FlagState.OFF,

                // Identical method to Subtract() here.                                                               
                Auxiliary = (input[0] & 0b1000) == (Result[0] & 0b1000) ? FlagState.OFF : FlagState.ON
            };
        }
        public static FlagSet ShiftLeft(byte[] input, byte count, out byte[] Result)
        {
            // A massive performance boost can made here by doing *something* to $count. 
            // Think about how many bits are in an 8 byte value, on x86-64, 64. If I shift 64 times, what am I doing? 
            // Wasting cycles. This is really clever trick to improve performance at a hardware level. If I bitshift a QWORD
            // by any number greater than 64, I get (Hold the thought of where the carry flag is used), 0. There is no room
            // for those bits to go after 63. To shift by 63 is to make the LSB the MSB. If you are trying to zero a register,
            // I believe the best way is xor rax, rax. 
            // Now for when the size isn't a QWORD, it is only "optimised" for DWORDs. If I could guess as to why, I would
            // say it's because you would hardly ever shift a WORD or BYTE, there isn't much space to move. Shifts are
            // often used to multiply by a big power of two quickly. So, WORDs and BYTEs may waste cycles, but DWORDs
            // follow the exact same logic as described for QWORDs.
            count &= (byte)((input.Length == 8) ? 0b00111111 : 0b00011111);

            // This gets a little fiddley, flags later are checked based on this, so I ought to save a copy.   
            int StartCount = count;

            Result = input.DeepCopy();

            // Don't bother shifting by 0, just return now without changing the flags(as said by Intel)
            if (count == 0)
            {
                return new FlagSet();
            }

            // Pull is a term coined by myself to denote a carry in shift instructions. I think it describes the idea a little more, because it wouldn't really
            // be a carry. It has to be initialised here or compiler moans because ""it may not be initialised"". I already returned from the method if
            // count was zero!!! 
            bool Pull = false;

            // So, what's my thinking with this algorithm. 
            // Briefly, I will explain what a bitwise shift is.
            // Know how multiplying an integer by ten is really easy? All I do is add a zero 
            // to the end. This is a cool phenomenon that happens when you multiply a number
            // by the base its represented in. So a single shift could be described as this notion
            // of multiplying by two, but most of the time we're going to want to shift a bunch
            // to get some big numbers. So, this could be written as 
            //   (the number I want to multiply) * 2^(the number of shifts)
            // This is exactly the same as 
            //   (the number I want to shift) << (the number of shifts)
            // But how does this to translate to being easy? 10 shift 2 is 40!
            // It's only easy when the result is represented in the base as well.
            // Lets show this example graphically,
            //   10 << 0 = 10 = [0001010]
            //   10 << 1 = 20 = [0010100]
            //   10 << 2 = 40 = [0101000]
            //                     ^ Look, these just jumped along each time
            // Or another way of thinking of it would be
            //   10 << 0 = 10 = 1010
            //   10 << 1 = 20 = 10100
            //   10 << 2 = 40 = 101000
            // They are both correct but the former really is a better way to get it into your
            // head because when we're dealing with numbers mathematically, there are no numbers
            // but we need to apply the hardware limitations(or advantages depending on how you see it)

            // Now the algorithm.
            // What does pull really describe?
            // A "pull" describes the situation where a bit has to jump from one byte in the
            // array to the next in the direction of the MSB. Nobody else has every dreamed
            // of this because it's just not a problem that exists in lower level languages.
            // Lets show see this graphically
            // I want to shift 128 by 1, but I have a problem
            //   128 = [10000000]
            //   128 << 1 = 0??? = [0000000]
            // Yes, it definitely would if I'm shifting a byte, but fortunately for purposes of
            // explanation we will be dealing with a word capacity, so more accurately 128 could be written as
            //   128 = [00000000] [10000000]
            // So pull denotes the idea of this jump, where 128 becomes 256
            //   128 << 1 = 256 = [00000001] [00000000]
            // Fortunately if we are clever there is a good way to predict this.
            // Each outer iteration, where count is decremented, denotes a shift. I handle one
            // shift at a time, I don't think it would be impossible to do it more efficiently, but I challenge you
            // to do so. So immediately it becomes obvious how I can predict if there will be a Pull, simply by the
            // fact the MSB is 1. If I shift the byte as-is, I'm going to lose that MSB, so I'm going to save it 
            // in the Pull boolean and come back to it next nested iteration. Now I will explain how the PullMask
            // works. If I shift a number, what is one thing I can be absolutely sure of? That the number will
            // be even. Moreover, the LSB will be 0. Remember that I am multiplying by 2 each time, just very effectively.
            // So, if I need to pull a byte up a power(remember that each index in the array represents a power of 256),
            // I can bitwise OR the next index by one to preserve the bit pushed out of the previous byte by the shift.
            // This is exactly how the PullMask works, if there is no Pull, the mask is 0, it does nothing.
            // Take careful note of how the shift is in brackets. I don't know or care of the operator priority here,
            // but I'm trying to make it realy clear that I shift BEFORE ORing. There is no guarentee that the LSB will
            // be zero if I mask before shifting, and I will also get the wrong value because the bit that was already
            // shifted by the pull mechanism will be shifted once more. (Also the most likely outcome in my code is 
            // that without brackets, 1 will be ORed by PullMask, which would always be 1)
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

            // Create some flags, use the constructor for a generic arithmetic flag set, e.g set SF, ZF, and PF as per usual.
            FlagSet OutputFlags = new FlagSet(Result);
            if (StartCount == 1)
            {
                // The overflow flag is actually defined to be set as the MSB ^ CF but since the carry flag is
                // equal to Pull, I just substituted for ease. Think of it as the CF.
                // Here I will explain what exactly these flags represent. 
                // The overflow flag is absolutely useless. I can think of no reason or purpose for
                // it here, I can only assume it meant something tens of years ago and was just kept
                // for compatability, like you will find with a lot of things in x86_64.
                // Then the CF just shows the value of the last bit to be pushed out of the result.
                // This could still have some use on shifts greater than 1, but I imagine the most
                // common use is to check for a sign bit that got pushed out.
                OutputFlags.Overflow = (MSB(Result) == 1) ^ Pull ? FlagState.ON : FlagState.OFF;
            }
            OutputFlags.Carry = Pull ? FlagState.ON : FlagState.OFF;
            return OutputFlags;
        }
        public static FlagSet ShiftRight(byte[] input, byte count, out byte[] Result, bool arithmetic)
        {
            // See ShiftfLeft()
            count &= (byte)((input.Length == 8) ? 0b00111111 : 0b00011111);
            int StartCount = count;
            Result = new byte[input.Length];
            Array.Copy(input, Result, input.Length);
            if (StartCount == 0)
            {
                return new FlagSet();
            }

            // Instead of thinking about pulls in ShiftLeft, now I need to
            // implement the opposite, where the LSB of a byte is pushed in the
            // direction of the actual LSB. Literally, a shift right is a shift
            // left but in the opposite direction. The inverse of ShiftLeft
            // (in scenarios where no bits are pushed out the result), or dividing 
            // by a power of 2. However you want to think of it. Lets show the example
            // from earlier. I shifted 128 by 1 to get 256, but now I want to go back.
            //  256 = [00000001] [00000000]
            // Obviously if I shifted the value as is, the bit would just be cropped off
            // because C# doesn't understand that the byte array represents one value,
            // so I have to predict if this will happen. I can do exactly that by masking
            // the value by 1, and seeing if the answer is equal to 1. Essentially I'm
            // ignoring all other bits and just checking if the LSB is set. If it is,
            // the Push boolean will be on and then shift as normal. The result,
            //  128 = [00000000] [10000000]
            // does have no bits set in the upper byte, so losing this bit in the upper
            // byte does happen--I just have to be aware of when it does.
            // So then on the next iteration(because the loop works backwards), if the push
            // is set, the result of the shift gets masked by 128(which sets the MSB)
            // Read ShiftLeft() if unsure about any of this. They are two very similar
            // operations but I don't want to repeat myself.
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
            // This(if not earlier) is where things go tragic if you lied in the size argument.                                                           
            int InputMSB = MSB(input);

            // If I was shifting a value for part of an arithmetic operation, but I had a signed negative, what would happen? I would
            // lose that sign bit for every shift greater than 0. That means any negative shifted would be useless in arithmetic, and
            // the developer would have to implement their own mechanism to do it themselves, which would be a waste.
            // Aside from the overflow flag, this is the difference between shift right(SHR) and shift arithmetic right(SAR)
            // Briefly, for shifting left, this problem is not solvable. Since we would be shifting in the direction of the MSB,
            // what if bit 7 of the input was on, and shifted into the sign bit? What if bit 8 was on and 7 was off, we just lost
            // our sign bit! Either way poses a risk of invaldating the result, the only safe bit to change when shifting left is
            // the LSB, because it is guarenteed it would be off. But that doesn't have much use to us, so, this is a limitation
            // for using shifts as a way to multiply by powers of 2, it's only going to work for unsigned numbers, despite having
            // two mnemonics, "SAL" and "SHL", they both are the exact same operation. They aren't even two different opcodes,
            // you could use objdump that would disassemble it as SAL, then use GDB that tells you SHL. 
            // So when we want to shift arithmetic right, what do we do? Remember how when shifting left, I could be absolutely
            // certain that the LSB would be off, because all the bits moved a place in that direction, here we can apply the exact same
            // idea. Since I did the reverse, I can be absolutely certain that the MSB will be off. This is where the whole "sign issue"
            // comes from. So, check the sign bit of the input, the MSB, and then OR the last bit of the Result by it(which is 100% off).
            // There are two scenarios here,
            //  1. The InputMSB was off; ORing by 0 changes nothing, in this case leaves it as 0.
            //  2. The InputMSB is on; ORing by 1 sets the LSB of the last byte on. Wait what? That's not what I want. Therefore, I have
            //     to shift it by 8. Since we know what shifting does, it is clear that I'm making sure it takes up the 8th bit not the first, 
            //     because MSB() just tells us whether the sign bit is on or off, it doesn't actually give me the value of the sign bit(intentionally so).
            if (arithmetic)
            {
                Result[Result.Length - 1] |= (byte)(InputMSB << 8);
            }
            FlagSet OutputFlags = new FlagSet(Result);
            if (StartCount == 1)
            {
                if (arithmetic)
                {
                    OutputFlags.Overflow = FlagState.OFF;
                }
                else
                {
                    // So, you may have noticed the InputMSB was calculated whether SAL or not.
                    // this is because in a SHR, the developer still told the sign, just
                    // don't set it for them. It's done by setting the overflow flag to what
                    // we would have set the MSB to in SAR, it's just in a SAR instruction, 
                    // we save the developer the time of masking the result themself.
                    OutputFlags.Overflow = InputMSB == 1 ? FlagState.ON : FlagState.OFF;
                }
            }
            return OutputFlags;
        }
        public static FlagSet RotateLeft(byte[] input, byte bitRotates, bool useCarry, bool carryPresent, out byte[] Result)
        {
            // Firstly, we need to make sure we know what a bitwise rotation does. If you don't know what a shift is, read ShiftLeft() first.
            // When we shift, there is a good chance our bits are going to be pushed out the result. What if we could shift a value, then
            // take all the bits pushed out and put them in the empty space created by the shift, which would be equal to the number of bits
            // pushed out. Consider we are working on some single bytes,
            //  255      = [11111111]
            //  255 << 1 = [11111110]
            // When I left shifted 255 by 1, I created an empty LSB and the MSB went into oblivion. What if I kept that MSB and said,
            // I want to wrap this back around, so if I did this mystery operation on 128, I would get 1.
            //  128   = [10000000]
            //  128 ? 1 [00000001]
            // This socalled "mystery operation" is exactly Rotate Left. Lets go back to 255,
            //  255       = [11111111]
            //  255 << 1  = [11111110]
            //  255 ROL 1 = [11111111]
            //  255 ROL 9123192389 trillion = [11111111]
            // Naturally this works for more than one bit,
            //  96          = [01100000]
            //  96 ROL 1    = [11000000]
            //  96 ROL 2    = [10000001]
            //  96 ROL 3    = [00000011]
            //  96 ROL 1027 = [00000011]
            // But how did I do that in my head? Read ahead.
            byte StartCount;

            // What exactly is the purpose of useCarry?
            // There is a certain variation of ROL called RCL, Roll carry left juggles the value that would be pushed to bit 1
            // into the carry flag instead. In effect, we get an extra bit to work with.
            // Lets go back to 128, the extra [] represent the CF.
            //  128       =    [10000000]
            //  128 ROL 1 =    [00000001]
            //  128 RCL 1 = [1][00000000]
            //  128 ROL 2 =    [00000010]
            //  128 RCL 2 = [0][00000001]
            // This effectively gives us a 65-bit number to work with.
            if (useCarry)
            {
                // Like with shifting, we can save a lot of time by masking the bitRotates
                // input to eliminate any uneccesary operations. Let's say I want to ROL
                // a value by 8. Well, all the bits are going to go in a full circuit and
                // give the same result.
                //  128       = [10000000] 
                //  128 ROL 8 = [10000000]
                // Consider the example earlier, where I did 96 ROL 1027 in two seconds. 
                // Well, I know 1024 is some multiple of 8, so I think, how many OVER that
                // did I go, that's 3 right? 7-4 = 3. So if I modulo the count by 8, I can
                // save a tonne of time right? 
                StartCount = (RegisterCapacity)input.Length switch
                {
                    RegisterCapacity.BYTE => (byte)((bitRotates & 0x1F) % 9),
                    RegisterCapacity.WORD => (byte)((bitRotates & 0x1F) % 17),
                    RegisterCapacity.DWORD => (byte)(bitRotates & 0x1F),
                    RegisterCapacity.QWORD => (byte)(bitRotates & 0x3F),
                    _ => throw new Exception(),
                };
                // Now you may have noticed, I moduloed by 9 not 8. It even took myself a minute
                // to realise that this reduction is only done in RCL not ROL! Honestly, this is
                // a blind copy from the Intel provided pseudo code on page 523 chapter 4 Vol 2B
                // of their 2019 manual, it would be nice if they included this optimisation for
                // ROL, and I would guess they do at a hardware level, but let's go with what
                // we know.
                // If you're still unsure as to why its 9 and 17, think of the CF as an extra bit
                // and apply the exact same principle.
                //  128       = [0][10000000]
                //  128 RCL 8 = [0][01000000]
                //  128 RCL 9 = [0][10000000]
            }
            else
            {
                // Unlike before, we only optimise by masking not using modulo. There is a possibility that on a hardware level, to modulo
                // by 8 is simply not worth the performance tradeoff.
                StartCount = (byte)(bitRotates & (((RegisterCapacity)input.Length == RegisterCapacity.QWORD) ? 0x3F : 0x1F));
            }
            
            // Exit early if no shifts
            if (StartCount == 0)
            {
                Result = input;
                return new FlagSet();
            }

            // Shift a deep copy of the input to prevent the input from being changed because of its reference.
            Result = input.DeepCopy();

            // Set this if there is a carry flag already. If the instruction is ROL, this is set but never used.
            bool Carry = carryPresent;

            // Pre-set if there is already a carry flag. This is the value that would be pushed into LSB
            // This is done a little differently if I'm not using the carry flag. Or could be said that I only have to worry about this if I am using the carry flag.
            bool Pull = carryPresent && useCarry;

            // Each iteration of $RotateCount rotates the result by one, so do that as many times  
            // as I want to rotate in total.
            for (byte RotateCount = 0; RotateCount < StartCount; RotateCount++)
            {
                // Iterate over each byte--bits can be handle at a bigger scale.
                for (int i = 0; i < input.Length; i++)
                {
                    byte Mask = (byte)(Pull ? 0b1 : 0);
                    Pull = MSB(Result[i]) == 1;
                    Result[i] = (byte)((Result[i] << 1) | Mask);
                }

                // Since I'm rotating a byte, I care about every bit in said byte, so I need to handle the pull before the damage is done.
                // If there is a carry,
                //  1. Set the LSB of the result if there is a carry
                //  2. Set the carry to be the previous MSB(before rotation). This will be stored in the pull boolean
                //     and if I was shifting, I would just discard that right here because it wouldn't be needed, but
                //     since I want to rotate, it loops right back round.
                // If there is no carry,
                //  1. Don't have to worry about step 1 of before, I just set the LSB to the MSB before rotation.
                // Lastly, turn the pull off because now I'm going to do a completely different rotation.
                // The algorithm isn't interdependent on iterations--you could take the result out of any $RotateCount
                // iteration and get a result equal to the input array rotated $RotateCount times. Compare this to division
                // and the difference becomes obvious.
                if (useCarry)
                {
                    Result[0] |= (byte)(Carry ? 1 : 0);
                    Carry = Pull;
                }
                else
                {
                    Result[0] |= (byte)(Pull ? 0b1 : 0);
                }
                Pull = false;
            }

            // "The SF, ZF, AF, and PF flags are always unaffected." - Intel manual May 2019 edition
            FlagSet ResultFlags = new FlagSet();
            if (useCarry)
            {
                // This is where the carry flag is set in the output. I don't have to worry about setting it each
                // time in the loop, I can get away with using a boolean. This is because my program performs what is called atomic operation. 
                // It really gets advanced here, but a processor can skip ahead in the code and do operations that are not interdependent on each other.
                // Let's say I have this code,
                //  mov eax, 0x10
                //  rol eax, 0x1F
                //  mov ebx, 0x20
                //  rol ebx, 0x1F
                // Lets also pretend that our processor isn't very fast. It definitely still applies to even the fastest processors
                // , little tricks like this make our flagship models really fast. But despite being really slow, our pretend processor
                // has 2 cores. We have all been told that "multiple cores don't make single processes faster" but it's really wrong.
                // Lets look a little deeper at the code given, I'm moving 10 into $eax, and rotating it left by 31, then setting $ebx
                // to 32 and doing the same. But these two operations are completely independent of each other. The first two instructions
                // affect the second two in no way at all. So, why not delegate these to core 2, so effectively we can execute this piece
                // of code theoretically in half the time. However what about with RCL?
                //  0x0 mov al, 0x80
                //  0x2 rcl al, 1
                //  0x4 mov bl, 0x80
                //  0x6 rcl bl, 1
                //  0x8 nop
                // Remember that 0x80 in binary is [10000000]
                // If we ran this atomically(in series as you see it), 0x2 would set the carry flag on. Then at 0x6 the carry flag would already
                // be set, so it would loop round to the LSB, but also the MSB would be pushed into the carry flag. So at 0x8, our results would be
                //  $AL = 0
                //  $BL = 1
                //  $CF = 1
                // And this is absolutely correct, but what if I tried this inatomically and say that I delegate instructions 0x0 and 0x2 to core 1, 0x4 and 0x6 to
                // core 2. This would give a different result! Assuming that the CF was not already set, the core 2 wouldn't know that after core 1
                // finishes, the CF would be set, so as a result there would have been no CF set to rotate into $BL, therefore the result would be
                //  $AL = 0
                //  $BL = 0
                //  $CF = 1(because the both cores would set the CF on, that doesn't mean it would be equal to 2. The CF is actually just a bit in a
                //          register. Think of it as setting a boolean to true twice, you did the same thing twice but nothing changes)
                // So this demonstrates why both RCL instructions cannot be executed inatomically in this scenario. However, some things could. 
                // Whilst 0x2 is being executed by core 1, core 2 could execute 0x4, then once core 1 is finished it can move on to 0x6.
                // Despite being a super cool low level concept, I don't incorporate it into my program. Why? It defeats the purpose entirely.
                // Imagine trying to debug some code, then randomly another part of your program starts executing and changing stuff, that would be a massive pain.
                // Most importantly, when I'm debugging assembly code, I don't care about speed. Its never going to take any amount of considerable
                // time to execute a single instruction. Performance is only really important to the end-user. Also, you have to think about why you
                // are learning assembly. If I didn't care about how I can take full advantage of the processor, I wouldn't be writing my code in assembly.  
                ResultFlags.Carry = Carry ? FlagState.ON : FlagState.OFF;
            }
            else
            {
                // If the carry flag wasn't used in rotation because ROL was used, set it to the LSB of the result. As to why, I don't know. I can't
                // think of a scenario where I would use this, but Intel writes it as "For ROL and ROR instrucrtions, the original value of the CF
                // is not part of the result, but the CF flag recieves a copy of the bit that was shifted from one end to the other".
                ResultFlags.Carry = LSB(Result) > 0 ? FlagState.ON : FlagState.OFF;
            }

            if (StartCount == 1)
            {
                // As with in ShiftLeft(), I don't know when this would be used. I assume compatibility.  
                // The Intel manuals are very cookie-cutter, they don't explain much, they just state what happens.
                ResultFlags.Overflow = (MSB(Result) ^ (ResultFlags.Carry == FlagState.ON ? 1 : 0)) == 0 ? FlagState.OFF : FlagState.ON;
            }
            return ResultFlags;
        }
        public static FlagSet RotateRight(byte[] input, byte bitRotates, bool useCarry, bool carryPresent, out byte[] Result)
        {
            // See RotateLeft() before reading
            byte StartCount;
            if (useCarry)
            {
                StartCount = (RegisterCapacity)input.Length switch
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
                StartCount = (byte)(bitRotates & (((RegisterCapacity)input.Length == RegisterCapacity.QWORD) ? 0x3F : 0x1F));
            }
            
            // Exit early if no rotates
            if (StartCount == 0)
            {
                Result = input;
                return new FlagSet();
            }
            Result = input.DeepCopy();
            bool Carry = carryPresent;

            // If theres a carry, its going to become the MSB of the result.
            bool Push = carryPresent && useCarry;

            // Like ShiftRight(), work backwards.
            for (byte RotateCount = 0; RotateCount < StartCount; RotateCount++)
            {
                for (int i = input.Length - 1; i >= 0; i--)
                {
                    // Instead of setting the LSB, I want to set the MSB of the byte in the array,
                    // due to the direction of bit movement. To think of it simply, every bit in
                    // the input is being shifted one place right, then the LSB from before(preserved
                    // in $Push used to OR the MSB.
                    byte Mask = (byte)(Push ? 0b10000000 : 0);
                    Push = LSB(Result[i]) == 1;
                    Result[i] = (byte)((Result[i] >> 1) | Mask);
                }

                // Very similar to RotateLeft, except instead of having shifting left, I shifted
                // right, so the CF represents the initial LSB. If the CF was set already,
                // that means the MSB will be set. Read RotateLeft() and ShiftRight(), both 
                // algorithms are almost identical.
                if (useCarry)
                {
                    Result[Result.Length - 1] |= (byte)(Carry ? 0b10000000 : 0);
                    Carry = Push;
                }
                else if (Push)
                {
                    Result[Result.Length - 1] |= 0b10000000;
                }
                Push = false;
            }
            FlagSet ResultFlags = new FlagSet();
            if (useCarry)
            {
                // Set the real CF afterwards as part of the result(not just the boolean).
                ResultFlags.Carry = Carry ? FlagState.ON : FlagState.OFF;
            }
            else
            {
                // If the instruction was ROR, set the CF to the MSB.
                ResultFlags.Carry = MSB(Result) > 0 ? FlagState.ON : FlagState.OFF;
            }
            if (StartCount == 1)
            {
                // XOR of the sign bit and the second-to-last bit of the result. I don't know when this would be useful, but it's there.
                ResultFlags.Overflow = (MSB(Result) ^ GetBit(Result, Result.Length * 8 - 1)) == 0 ? FlagState.OFF : FlagState.ON;
            }
            return ResultFlags;
        }
        public static (byte, byte) GetBitMask(int bit)
        {
            // Calculate the position of a bit in a byte array, return it in the format (index, mask)
            // To find the index is easy, just divide the input by 8. There are 8 bits in a x86-64 byte 
            // so that is how many indexes I need to skip over. 
            // Now to create a mask to get the bit I want out of this element. Well, first I take the 
            // modulo of the bit I want. This represent how many bits short I was of being bang on, e.g
            // if I want the 9th bit, that's just over the 1st index by one, so 9 % 8 = 1, i.e the first
            // bit of the next byte is what I want. If I wanted the 10th, 10 % 8 = 2, 2nd bit of the next
            // byte. So how do I make a mask out of this? Well since I only want 1 bit, I can shift 1
            // by the offset to get a mask for the bit I want. So to get the mask I have to modulo
            // by 8, because if I did want bit 8, I wouldn't shift 1 at all, it would be the first bit
            // of the 2nd byte. This is also necessary to mask the MSB. There is still a problem though,
            // I already have 1 in the first column, so I need to subtract 1 from $bit to make up for this,
            // as $bit % 8 dictates which bit I want, if I were to shift 1 by that I would be going one too far.
            // Now to use this mask, I could bitwise AND my variable and the mask to get the VALUE of the bit. Remember that once this method
            // returns the job isn't completely done. I then need to left shift the result of the bitwise AND $bit % 8 times. 
            // Just because I am selecting a single bit doesn't mean I take away its value. A bit in the 2nd column will always represent
            // two, 4 in the 3rd column and so forth.
            // (Note that throughout this explanation, I don't consider bits to be 0 index based. Naturally
            // the array is, but in a qword I would refer to the MSB as the 64th bit and the LSB as the 1st)
            return ((byte)(bit / 8), (byte)(1 << ((bit - 1) % 8)));
        }
        public static byte GetBit(byte[] input, int bit)
        {
            // An example implementation of GetBitMask()
            (byte Index, byte Mask) = GetBitMask(bit);
            // This bit could represent 128,64,32,16,8,4,2 or 1. Regardless, I want it to be one. You could work around this in every usage of the method
            // but it would be a lot more consistent to output either a 1 or 0.
            int WeightedBit = input[Index] & Mask;
            // Another way of turning the WeightedBit into a 1 or 0 is to use integer division. A number divided by itself is always 1(save zero, but I know that the value
            // of $Mask will be nonzero because I'm left shifting 1 by a value less than 8, which means it could not be shifted out of the byte.) So if the WeightedBit is
            // set, dividing it by itself will give one. If it isn't, I would be dividing 0 by the mask, which would give 0.
            return (byte)(WeightedBit / Mask);
        }
        public static byte MSB(byte input)
        {
            // The same method de-weighting a bit as detailed in GetBitMask(). Since I know the MSB is always the 8th bit in x86-64, shifting it
            // 7 places to the right means that it will either be 1 or 0.
            return (byte)(input >> 7);
        }
        public static byte MSB(byte[] input)
        {
            // Get the MSB of an array-represented value.
            return MSB(input[input.Length - 1]);
        }
        public static byte LSB(byte input)
        {
            // LSB is a little easier than MSB because the LSB is either 1 or 0 anyway. If I bitwise AND $input by 1, I can only get 1 or 0, I don't have to deweight that value.
            return (byte)(input & 1);
        }
        public static byte LSB(byte[] input)
        {
            // Get the LSB of a value represented as an array.
            return LSB(input[0]);
        }
        public static bool GetParity(byte input)
        {
            bool Even = true;
            for (int i = 0; i < 8; i++)
            {
                if ((input & (1 << i)) > 0)
                {
                    Even ^= true;
                }
            }
            return Even;
        }
        public static byte[] SignExtend(byte[] input, byte newLength)
        {
            // Resize an array using sign extension.

            // If the input is a longer than the newLength, something has probably gone horribly wrong. However, there is a good chance they're even. In which case, just return now.
            // because nothing needs to be done
            if (input.Length < newLength)
            {
                // I don't want to overwrite the whole array, so start at the current length(this gives the index after the last byte in the array)
                int startIndex = input.Length;

                // Then resize the array so there is space for the new bytes.
                Array.Resize(ref input, newLength);

                // Now I need to determine the sign. Is the sign bit(the MSB) greater than 0x7F? Because if it is, twos compliment says it's going to be negative.
                // To demonstrate this I will write out each bit in a signed byte,
                // [-128][64][32][16][8][4][2][1]
                // Now If I have 0x7F, this is the same as,
                // [0][1][1][1][1][1][1][1]
                // So by looking at that I can be 100% sure that any value greater than 0x7F used that last -128 bit.
                // -128 + 64 + 32 + 16 + 8 + 4 + 2 + 1 = -1, so if the -128 bit is set, it must be negative.

                // Here I take a little shortcut because I know how Array.Resize() works.
                // It does exactly two things.
                //  1. Assign the input to a new array of the length passed as an argument.
                //     Since the input was passed by reference, this can be done. If it wasn't,
                //     the new array would be lost after the function returns because a new instance
                //     was assigned to the input array. This is exactly how "new" works, don't use it
                //     so blindly, it will destroy the reference. So for us this would mean that the
                //     array after the method would be the same as the array before(to the caller) as
                //     the array created would be local to the callee.
                //  2. A byproduct of step 1, the new array initialised will have all elements equal to
                //     the default value of the type it holds. What this means though is that if the 
                //     type contained is a value type(int, byte, long) it will be 0, or if it inherits
                //     from object(e.g a class), it will be null.(There may be some weird exception somewhere
                //     that I don't know of, but these are pretty safe assumptions). So, we can take advantage
                //     of this, because if a positive number is sign extended, like zero extension, it will
                //     just be filled with zeros until it is the desired length. In short, if it is a signed positive
                //     resize the array and return.

                //  This could also be done with a bit mask, but there is no significant difference.
                if (input[startIndex - 1] > 0x7F)
                {
                    // If it was a negative, every value after and including the startIndex needs
                    // to be set to 0xFF. This is probably the best way of doing so.                          
                    for (int i = startIndex; i < newLength; i++)
                    {
                        input[i] = 0xFF;
                    }
                }
            }
            return input;
        }
        public static byte[] ZeroExtend(byte[] input, byte length)
        {
            // A performance test shows this is as fast as zero extension in C# can get http://prntscr.com/ofb1dy
            // Unfortunately, this does not hold a candle to zero extension in assembly.
            // See SignExtension() for a full explanation of why this works.
            Array.Resize(ref input, length);
            return input;
        }
    }
}
