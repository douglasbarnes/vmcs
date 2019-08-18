using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace debugger.Util
{
    public enum FormatType
    {
        Hex,
        Decimal,
        SignedDecimal,
        String
    }
    public static class Core
    {
        public static Dictionary<T1, T2> DeepCopy<T1, T2>(this Dictionary<T1, T2> toClone)
        {
            Dictionary<T1, T2> Output = new Dictionary<T1, T2>();
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
            return Bitwise.GetBits(input).Length % 8 == 0 & input[input.Length - 1] >= 0x80;
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
            for (int i = leftSide.Length - 1; i > 0; i--)
            {
                if (leftSide[i] != rightSide[i]) // if they are not the same; sign doesnt matter here
                {
                    if (signed)
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
        public static bool ListsEqual(List<string> input1, List<string> input2)
        {
            if (input1.Count() != input2.Count()) { return false; }
            for (int i = 0; i < input1.Count; i++)
            {
                if (input1[i] != input2[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static string[] SeparateString(string inputString, string testFor, bool stopAtFirstDifferent = false) => SeparateString(inputString, new string[] { testFor }, stopAtFirstDifferent);
        public static string[] SeparateString(string inputString, string[] testFor, bool stopAtFirstDifferent = false) // output {inputstring with stuff removed, (strings of separated testFors)
        {
            string[] Output = new string[testFor.Length + 1];
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
                Output[i + 1] = Separated;
            }
            return Output;

        }
        public static string FormatNumber(ulong Number, FormatType formatType, int Padding = 16)
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
        public static string Atoi<T>(T[] toConvert, bool addSpaces=false)
        {
            string Output = "";
            for (int i = 0; i < toConvert.Length; i++)
            {
                if(ulong.TryParse(toConvert[i].ToString(), out ulong Parsed))
                {
                    Output += Parsed.ToString("X");
                    if(i+1 != toConvert.Length && addSpaces)
                    {
                        Output += " ";
                    }
                }
            }
            return Output;
        }
        public static string Atoi<T>(List<T> toConvert, bool addSpaces=false)
        {
            string Output = "";
            for (int i = 0; i < toConvert.Count; i++)
            {
                if (ulong.TryParse(toConvert[i].ToString(), out ulong Parsed))
                {
                    Output += Parsed.ToString("X");
                    if (i + 1 != toConvert.Count && addSpaces)
                    {
                        Output += " ";
                    }
                }
            }
            return Output;
        }
    }
}
