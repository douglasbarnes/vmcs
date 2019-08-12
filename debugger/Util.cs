using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using static debugger.ControlUnit;
using static debugger.FormSettings;
using static debugger.Primitives;
using static debugger.ControlUnit.FlagSet;
using System.Windows.Forms;

namespace debugger
{
    public class Primitives
    {
        public enum FormatType
        {
            Hex,
            Decimal,
            SignedDecimal,
            String
        }

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
            BYTE = 1,
            WORD = 2,
            DWORD = 4,
            QWORD = 8
        }
        public struct ModRM
        {
            public ulong DestPtr;
            public ulong Reg;
            //for disas only
            public long Offset;
            public byte Mod; // first 2
           // public byte Reg; // next 3
            public byte Mem; // last 3
            public SIB DecodedSIB;
            public ModRM ChangeSource(ulong newSource)
            {
                return new ModRM()
                {
                    DestPtr = DestPtr,
                    Offset = Offset,
                    Mod = Mod,
                    Mem = Mem,
                    DecodedSIB = DecodedSIB,
                    Reg = newSource
                };
            }
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
        public static Dictionary<T1,T2> DeepCopy<T1, T2>(this Dictionary<T1,T2> toClone)
        {
            Dictionary<T1,T2> Output = new Dictionary<T1, T2>();
            T1[] ClonedKeys = toClone.Keys.ToArray();
            T2[] ClonedValues = toClone.Values.ToArray();
            for (long i = 0; i < ClonedKeys.LongLength; i++)
            {
                Output.Add(ClonedKeys[i], ClonedValues[i]);
            }
            return Output;
        }
        public static List<T> DeepCopy<T>(this List<T> toClone) => toClone.ToArray().ToList();
        public static T[] DeepCopy<T>(this T[] toClone) //http://prntscr.com/op88f4
        {
            T[] CopyBuffer = new T[toClone.Length];
            Array.Copy(toClone, CopyBuffer, toClone.LongLength);
            return CopyBuffer;
        }
        public static bool IsNegative(this byte[] input)
        { // convert.tostring returns the smallest # bits it can, not to the closest 8, if it evenly divides into 8 it is negative
            return Bitwise.GetBits(input).Length % 8 == 0 & input[input.Length-1] >= 0x80;
                                                                                     //-128 64 32 16 8  4  2  1
             // twos compliment: negative number always has a greatest set bit of 1 .eg, 1  0  0  0  0  0  0  1 = -128+1 = -127
                // this way is much faster than using GetBits() because padleft iterates the whole string multiple times
              // this method is just for performance because its used alot
        }
        public static bool IsZero(this byte[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
        public static int CompareTo(this byte[] leftSide, byte[] rightSide, bool signed)
        {
            Bitwise.PadEqual(ref leftSide, ref rightSide);
            for (int i = leftSide.Length-1; i > 0; i--)
            {
                if (leftSide[i] != rightSide[i]) // if they are not the same; sign doesnt matter here
                {
                    if(signed)
                    {
                        return (sbyte)leftSide[i] > (sbyte)rightSide[i] ? 1 : -1;
                    }
                    else
                    {
                        return leftSide[i] > rightSide[i] ? 1 : -1;
                    }
                }
            }
            return 0; //equal
        }
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
                    if(input[input.Length-i-1] != 0)
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
                byte[] Buffer = new byte[input.Length-offset];
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
            public static byte[] Or(byte[] input1, byte[] input2)
            {
                string sBits1 = GetBits(input1);
                string sBits2 = GetBits(input2);
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
            public static byte[] And(byte[] input1, byte[] input2)
            {
                string sBits1 = GetBits(input1);
                string sBits2 = GetBits(input2);
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
                OpcodeUtil.SetFlags(baResult, OpcodeUtil.FlagMode.Logic, input1, input2);
                return baResult;
            }
            public static byte[] Xor(byte[] input1, byte[] input2)
            {
                string sBits1 = GetBits(input1);
                string sBits2 = GetBits(input2);
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
                OpcodeUtil.SetFlags(baResult, OpcodeUtil.FlagMode.Logic, input1, input2);
                return baResult;
            }
            public static FlagSet Add(byte[] input1, byte[] input2,  int size, out byte[] Result, bool carry=false)
            {
                Result = new byte[size];
                for (int i = 0; i < size; i++) // faster doing it my own way http://prntscr.com/ojwfs2
                {
                    int sum = input1[i] + input2[i] + (carry ? 1 : 0); //(any carries
                    if (sum > 0xFF) //overflowed that index
                    {
                        Result[i] += (byte)(sum % 0x100);//leftover value stays
                        carry = true;//add 1 to next index
                    } else
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
            public static FlagSet Subtract(byte[] input1, byte[] input2, int size, out byte[] Result, bool carry=false)
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
                    }else
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
                while(dividend.CompareTo(divisor, signed) == 1)
                {
                    Subtract(dividend, divisor, size, out dividend);
                    Increment(Quotient, size, out Quotient);
                }
                Modulo = dividend;
            }
            public static FlagSet Multiply(byte[] input1, byte[] input2, bool signed, int size, out byte[] Result)
            {
                input1 = signed ? SignExtend(input1,(byte)(size * 2)) : ZeroExtend(input1, (byte)(size * 2));
                input2 = signed ? SignExtend(input2,(byte)(size * 2)) : ZeroExtend(input2, (byte)(size * 2));
                Result = new byte[size * 4];     //the result is going to be size*2, so we need to extend the inputs to that before and then cut down after.           
                for (int i = 0; i < size*2; i++)
                {
                    for (int j = 0; j < size*2; j++)
                    {    //times input[i] by every value in input[2] throughout the iteration  
                        int mul = (input1[i] * input2[j]) + Result[j + i];  // times the two and if there is a carry/something stored in the result already, add that(we have to do this here because otherwise we wouldn't be checking if we overflowed a byte later                         
                        Result[j + i] = (byte)(mul % 0x100); //multiplying bytes can give a 2byte result, so we add the LSB here 
                        if(mul > 0xFF) //if we need to carry
                        {
                            byte[] carry = new byte[size * 4];
                            carry[i + j + 1] = (byte)(mul / 0x100);
                            Add(Result, carry, size * 4, out Result);// then the MSB here,(a carry).    e.g the two byte A12B % 0x100 = 2B, A12B / 0x100 = A1 ( / operator is integer division)
                        }                        
                    } 
                }
                Array.Resize(ref Result, size * 2);                
                byte[] UpperComparison = new byte[size*2];
                Array.Copy(Result, UpperComparison,size);
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
                    if(input[i] < 0xFF)
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
                    sOutput += Convert.ToString(input[i],2).PadLeft(8, '0');
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
                byte[] baOutput = new byte[bitString.Length/8]; 
                for (int i = 0; i < baOutput.Length; i++) 
                {
                    baOutput[i] = Convert.ToByte(bitString.Substring(8*i,8), 2);
                }
                return baOutput;
            }
            public static byte[] SignExtend(byte[] input, byte newLength)
            {
                int startIndex = input.Length;
                byte sign = (byte)((input[startIndex-1]) > 0x7F ? 0xFF : 0x00);
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
        }
        public static class Core
        {
            public static bool ListsEqual(List<string> input1, List<string> input2)
            {
                if(input1.Count() != input2.Count()) { return false; }
                for (int i = 0; i < input1.Count; i++)
                {
                    if(input1[i] != input2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public static string[] SeparateString(string inputString, string testFor, bool stopAtFirstDifferent = false) => SeparateString(inputString, new string[] { testFor }, stopAtFirstDifferent);
            public static string[] SeparateString(string inputString, string[] testFor, bool stopAtFirstDifferent=false) // output {inputstring with stuff removed, (strings of separated testFors)
            {
                string[] Output = new string[testFor.Length+1];
                Output[0] = inputString; //base
                for (int i = 0; i < testFor.Length; i++)
                {                    
                    string Separated = "";
                    int InsertIndex = Output[0].IndexOf(testFor[i]);
                    while (InsertIndex != -1 && testFor[i] != "")
                    {                       
                        Output[0] = Output[0].Remove(InsertIndex, testFor[i].Length).Insert(InsertIndex, RepeatString(" ", testFor[i].Length));
                        Separated = Separated.PadRight(InsertIndex - Separated.Length) + testFor[i]; //+= string.Join("", RepeatString(" ", )) + testFor[i];
                        int LastIndex = InsertIndex;
                        InsertIndex = Output[0].IndexOf(testFor[i]);
                        if (stopAtFirstDifferent && InsertIndex != LastIndex + 1)
                        {
                            break;
                        }
                    }
                    Output[i+1] = Separated;
                }
                return Output;

            }
            public static string FormatNumber(ulong Number, FormatType formatType, int Padding=16)
            {
                string Output;
                switch (formatType)
                {
                    case FormatType.Hex:
                        Output = $"0x{Number.ToString("X").PadLeft(Padding, '0')}";
                        break;
                    case FormatType.String:
                        byte[] Bytes = BitConverter.GetBytes(Number);
                        Output = Encoding.ASCII.GetString(Bytes);
                        break;
                    case FormatType.SignedDecimal:
                        Output = Convert.ToInt64(Number).ToString();
                        break;
                    default: //dec
                        Output = Convert.ToUInt64(Number).ToString();
                        break;
                }
                return Output;
            }
            public static string RepeatString(string inputString, int count)
            {//faster than enumerable.repeat
                string Output = "";
                for (int i = 0; i < count; i++)
                {
                    Output += inputString;
                }
                return Output;
            }
        }
        public static class Drawing
        {
            private static string FormatModifiers = "!\"£$%";
            public static void DrawFormattedText(string text, Graphics graphicsHandler, Rectangle bounds, Emphasis defaultEmphasis=Emphasis.Medium)
            {
                string[] Output = new string[5];
                string Position = "";
                Stack<int> ModifierHistory = new Stack<int>();
                ModifierHistory.Push((int)defaultEmphasis);
                bool Escaped = false;
                for (int i = 0; i < text.Length; i++)
                {
                    if(Escaped & !"!\"£$%".Contains(text[i]))
                    {
                        Output[ModifierHistory.Peek()] += "\\";
                                              
                    }

                    if(FormatModifiers.Contains(text[i]) & !Escaped)
                    {
                        if(FormatModifiers.IndexOf(text[i]) == ModifierHistory.Peek())
                        {
                            ModifierHistory.Pop();
                        }
                        else
                        {
                            ModifierHistory.Push(FormatModifiers.IndexOf(text[i]));
                        }
                    }
                    else if(text[i] == '\\' & !Escaped)
                    {
                        Escaped = true;
                    }
                    else
                    {
                        Escaped = false;
                        graphicsHandler.DrawString(Position + text[i], BaseUI.BaseFont, TextBrushes[ModifierHistory.Peek()], bounds);
                        Position += " ";
                    }
                }               
            }
            public static void DrawShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer, int penSize=1)
            {
                graphics.DrawRectangle(new Pen(LayerBrush, penSize), bounds);
                graphics.DrawRectangle(new Pen(ElevatedTransparentOverlays[(int)overlayLayer], penSize), bounds);
            }
            public static void FillShadedRect(Graphics graphics, Rectangle bounds, Layer overlayLayer)
            {
                graphics.FillRectangle(LayerBrush, bounds);
                graphics.FillRectangle(ElevatedTransparentOverlays[(int)overlayLayer], bounds);
            }
            public static Rectangle GetCenter(Rectangle bounds, string text, Font font)
            {
                Size TextSize = CorrectedMeasureText(text, font);
                return GetCenter(bounds, TextSize.Width, TextSize.Height);
            }
            public static Rectangle GetCenter(Rectangle bounds, int offsetx=0, int offsety=0) 
                => new Rectangle(
                    new Point(bounds.X + (bounds.Width - offsetx) / 2, bounds.Y + (bounds.Height - offsety) / 2), 
                    new Size(bounds.Width / 2 + offsetx, bounds.Height / 2 + offsety));
            public static Rectangle GetCenterHeight(Rectangle bounds)
                => new Rectangle(
                    new Point(bounds.X, bounds.Y + (bounds.Height / 4)),
                    new Size(bounds.Width, bounds.Height / 2));
            public static Rectangle ShrinkRectangle(Rectangle bounds, int pxSquared)
                => new Rectangle(
                    bounds.Location,
                    new Size(bounds.Width - pxSquared, bounds.Height - pxSquared));
            public static Size CorrectedMeasureText(string text, Font font)
            {
                Size ToCorrect = TextRenderer.MeasureText(text, font);
                ToCorrect.Width -= (int)font.Size / 2;
                return ToCorrect;
            }
        }
        public class OpcodeUtil
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
                //switch (fm)
                //{
                //    case FlagMode.Add:
                //        Flags.Carry = (!baResult.IsGreater(baInput1, isSigned:false)); // if result < input(val decreased)
                //        baResult = Bitwise.Cut(baResult, (int)CurrentCapacity); // must be after carry                                                            
                //        //Flags.Overflow = (baInput1.IsNegative() == baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                //        Flags.Overflow = (!baRes)
                //        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                //        // negative + negative should never be positive, vice versa
                //        break;
                //    case FlagMode.Sub:
                //        Flags.Carry = (baResult.IsGreater(baInput1)); // if result > input(val increased)
                //        baResult = Bitwise.Cut(baResult, (int)CurrentCapacity); // must be after carry
                //        Flags.Overflow = (baInput1.IsNegative() != baInput2.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                //        // if sign of added numbers != sign of result AND both operands had the same sign, there was an overflow
                //        // negative + negative should never be positive, vice versa
                //        break;
                //    case FlagMode.Mul:
                //        Flags.Carry = (baResult != Bitwise.ZeroExtend(Bitwise.Cut(baResult, (int)CurrentCapacity), (byte)CurrentCapacity));
                //        Flags.Overflow = (baResult != Bitwise.SignExtend(Bitwise.Cut(baResult, (int)CurrentCapacity), (byte)CurrentCapacity));
                //        //if this^^ is false, EDX = sign extension of EAX, we didn't overflow into the EDX register       
                //        // take half the output, sign extend it, if they aren't the same we need to use two registers to store it
                //        // dont know how this plays out with 64bit regs
                //        break;
                //    case FlagMode.Logic:
                //        Flags.Carry = false;
                //        Flags.Overflow = false;
                //        break;
                //    case FlagMode.Inc:
                //        Flags.Overflow = (!baInput1.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                //        break;
                //    case FlagMode.Dec:
                //        Flags.Overflow = (baInput1.IsNegative() && baResult.IsNegative() != baInput1.IsNegative());
                //        break;
                //}
                //Flags.Sign = baResult.IsNegative(); //+=false -=true
                //Flags.Zero = baResult.IsZero();
                //Flags.Parity = Bitwise.GetBits(baResult[0]).Count(x => x == 1) % 2 == 0; //parity: even no of 1 bits       
            }

            public static RegisterCapacity GetRegCap(RegisterCapacity defaultCapacity = RegisterCapacity.DWORD)
            {
                if (PrefixBuffer.Contains(PrefixByte.REXW))
                {
                    return RegisterCapacity.QWORD;
                }
                else if (PrefixBuffer.Contains(PrefixByte.SIZEOVR))
                {
                    return RegisterCapacity.WORD;
                }
                else
                {
                    return defaultCapacity;
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
             public static void SetDynamic(OpcodeInput Input, byte[] baData)
             {
                 if (Input.IsSwap)
                 {
                     if (Input.DecodedModRM.Mod != 3)//not register->reg
                     {
                         SetRegister((ByteCode)Input.DecodedModRM.Reg, baData);
                     }
                     else
                     {
                         SetRegister((ByteCode)Input.DecodedModRM.Reg, baData);
                     }
                 }
                 else
                 {
                     if (Input.DecodedModRM.Mod != 3)
                     {
                         SetMemory(Input.DecodedModRM.DestPtr, baData);
                     }
                     else
                     {
                         SetRegister((ByteCode)Input.DecodedModRM.DestPtr, baData);
                     }
                 }
             }
                //works with r/m/imm
             public static (byte[], byte[]) FetchDynamic(OpcodeInput input, RegisterCapacity? regCap=null, bool allowImm64=false)
             {
                RegisterCapacity RegCap = regCap ?? CurrentCapacity;
                byte[] Dest;
                byte[] Source;
                if (input.IsSwap)
                {
                    if (input.DecodedModRM.Mod != 3)
                    {
                        Source = Fetch(input.DecodedModRM.DestPtr, (int)RegCap);
                    }
                    else
                    {
                        Source = FetchRegister((ByteCode)input.DecodedModRM.DestPtr, RegCap);
                    }
                    Dest = FetchRegister((ByteCode)input.DecodedModRM.Reg, RegCap);
                }
                else
                {
                    if (input.IsImmediate)
                    {
                       if (input.IsSignExtendedByte)
                       {
                            Source = Bitwise.SignExtend(FetchNext(1), ((byte)RegCap));
                       }
                       else if (CurrentCapacity == RegisterCapacity.QWORD && !allowImm64)
                       {
                            //from intel manual V
                            //REX.W + 0D id OR RAX, imm32 I Valid N.E. RAX OR imm32 (sign-extended). (in general, sign extend when goes from imm32 to r64
                            Source = Bitwise.SignExtend(FetchNext(4), 8);
                       } 
                       else
                       {
                            Source = FetchNext((byte)RegCap);
                       }
                    }
                    else
                    {
                        Source = FetchRegister((ByteCode)input.DecodedModRM.Reg, RegCap);
                    }

                    if (input.DecodedModRM.Mod != 3)
                    {
                        Dest = Fetch(input.DecodedModRM.DestPtr, (int)RegCap);
                    }
                    else
                    {
                        Dest = FetchRegister((ByteCode)input.DecodedModRM.DestPtr, RegCap);
                    }

                }
                return (Dest, Source);
             }
            
            public static ModRM FromDest(ByteCode Dest)
            {
                return new ModRM { DestPtr = (ulong)Dest, Mod=3, Mem=(byte)Dest};
            }

            
        }
        public static class Disassembly
        {
            public static List<Dictionary<RegisterCapacity, string>> RegisterMnemonics = new List<Dictionary<RegisterCapacity, string>>
            {
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RAX" }, { RegisterCapacity.DWORD, "EAX" },{ RegisterCapacity.WORD, "AX" },{ RegisterCapacity.BYTE, "AL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RCX" }, { RegisterCapacity.DWORD, "ECX" },{ RegisterCapacity.WORD, "CX" },{ RegisterCapacity.BYTE, "CL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RDX" }, { RegisterCapacity.DWORD, "EDX" },{ RegisterCapacity.WORD, "DX" },{ RegisterCapacity.BYTE, "DL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RBX" }, { RegisterCapacity.DWORD, "EBX" },{ RegisterCapacity.WORD, "BX" },{ RegisterCapacity.BYTE, "BL" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RSP" }, { RegisterCapacity.DWORD, "ESP" },{ RegisterCapacity.WORD, "SP" },{ RegisterCapacity.BYTE, "AH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RBP" }, { RegisterCapacity.DWORD, "EBP" },{ RegisterCapacity.WORD, "BP" },{ RegisterCapacity.BYTE, "CH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RSI" }, { RegisterCapacity.DWORD, "ESI" },{ RegisterCapacity.WORD, "SI" },{ RegisterCapacity.BYTE, "DH" }},
                new Dictionary<RegisterCapacity, string> {{ RegisterCapacity.QWORD, "RDI" }, { RegisterCapacity.DWORD, "EDI" },{ RegisterCapacity.WORD, "DI" },{ RegisterCapacity.BYTE, "BH" }}
            };
            public static Dictionary<RegisterCapacity, string> SizeMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
            {
                {RegisterCapacity.BYTE, "BYTE"},
                {RegisterCapacity.WORD, "WORD"},
                {RegisterCapacity.DWORD, "DWORD"},
                {RegisterCapacity.QWORD, "QWORD"}
            };
            public static (int, string, string) DisassembleSIB(SIB input, int mod)
            {   
                string AdditionalReg = null;
                if (input.Base != 5)
                {
                    AdditionalReg = RegisterMnemonics[input.Base][input.PointerSize];
                }
                else if (mod > 0)
                {
                    AdditionalReg = RegisterMnemonics[(int)ByteCode.BP][input.PointerSize];
                }
                string BaseReg = null;
                if(input.ScaledIndex != 4)
                {
                    BaseReg = RegisterMnemonics[input.ScaledIndex][input.PointerSize];
                }
                return (input.Scale, BaseReg, AdditionalReg);
            }
            //{dest}, {src}{+/-}{offset}
            public static (string, string) DisassembleModRM(OpcodeInput Input, RegisterCapacity RegCap)
            {
                if (Input.IsSwap)
                {
                    var _tmp = Input.DecodedModRM.Reg;
                    Input.DecodedModRM.Reg = Input.DecodedModRM.Mem;
                    Input.DecodedModRM.Mem = (byte)_tmp;
                }
                Pointer DestPtr = new Pointer() { BaseReg = RegisterMnemonics[Input.DecodedModRM.Mem][RegCap] };
                if(Input.DecodedModRM.Mem == 5 && Input.DecodedModRM.Mod == 0)
                {
                    DestPtr.BaseReg = "RIP";
                }
                else if (Input.DecodedModRM.Mem == 4 && Input.DecodedModRM.Mod != 3) // sib conditions
                {
                    (DestPtr.Coefficient, DestPtr.BaseReg, DestPtr.AdditionalReg) = DisassembleSIB(Input.DecodedModRM.DecodedSIB, Input.DecodedModRM.Mod);
                }
                DestPtr.Offset = Input.DecodedModRM.Offset;
                string Source = RegisterMnemonics[(int)Input.DecodedModRM.Reg][RegCap];
                string Dest = DisassemblePointer(DestPtr);
                if(Input.DecodedModRM.Mod != 3)
                {
                    Dest = $"[{Dest}]";
                }
                if (Input.IsSwap)
                {
                    return (Source, Dest);
                } else
                {
                    return (Dest, Source);
                }

            }
            public static string DisassembleRegister(ByteCode Register, RegisterCapacity RegCap)
            {
                return RegisterMnemonics[(byte)Register][RegCap];
            }

            public struct Pointer {
                public long Offset;
                public string BaseReg;
                public string AdditionalReg;
                public int Coefficient;
                public RegisterCapacity? Size;
            }
            public static string DisassemblePointer(Pointer p)
            {
                string Output = "";
                
                Output += p.BaseReg;
                if(p.Coefficient > 1)
                {
                    Output += $"*{p.Coefficient}";
                }
                if(p.AdditionalReg != null)
                {
                    Output += $"+{p.AdditionalReg}";
                }
                if(p.Offset != 0)
                {
                    if(p.BaseReg != null)
                    {
                        Output += p.Offset > 0 ? "+" : "-";
                    }
                    Output += $"0x{p.Offset.ToString("X")}";
                }
                if (p.Size != null)
                {
                    Output = $"{SizeMnemonics[p.Size.Value]} PTR [{Output}]";
                }
                return Output   ;
            }
              //  => $"[{(p.AdditionalReg == "" ? "" : $" +{p.AdditionalReg}")}{(p.Offset==0 && p.BaseReg != "" ? "" : $"{(p.Offset > 0 ? "+" : "-")}")}{p.Offset}]";
        }       //                                              dont show coefficients of 1                 if there is an additionalReg, show it with an add   if the offset isnt 0 and there is a base reg, it has a sign        

        
    }
}
