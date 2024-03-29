﻿// DecodedCompound provides the ability to chain multiple operands into a single opcode input. This allows the opcode inheritor to have a single constructor
// that can handle any number of inputs(if coded correctly). 
// Two constructors are provided for the initialisation of the DecodedCompound. The only different being that $InternalArray is created inside the constructor
// rather than in the arguments of the caller. The already information-abdundant OpcodeTable as clean as possible. So instead of writing,
//  new opcode(new DecodedCompound(new IMyDecoded[] { op1. op2 }));
// It can be written as,
//  new opcode(new DecodedCompound(op1, op2));
//
// DecodedCompound disassmbles all operands in the order of the array with Dissassemble().
// There is an added layer of complexity for setting and fetching.
// This is because of discriminative and indiscriminative selection methods. In an indiscriminative operation, only one byte[] is fetched from each IMyDecoded
// in $InternalArray. In a discriminative operation, all elements in the returned list are used. Both have use cases.
// For example, lets visualise an example $InternalArray,
//  [0] = Immediate 
//  [1] = ModRM 
//  [2] = Constant
// Immediate returns a List<byte[]> of count 1, where as ModRM can a List<byte[]> of count 2. 
// So to fetch the "3rd operand", should the Constant be returned, or the source of the ModRM?
// In a discriminative fetch, e.g DecodedCompound.Fetch()[2], would return the source of the ModRM.
// In a indiscriminative fetch(which requires storing the DecodedCompound and calling FetchIndiscriminative() on it), would return the Constant.
// This is only important in the interface when using SetSource(), because the first element would be percieved the same either way.  
// With SetSource(), a discriminative set is used.
// To set:
//  Set(byte[] data) - Call Set($data) on the first element in $InternalArray.
//  Set(byte[] data, int index) - A discriminative set of the $index-th operand
//  SetIndiscriminative(byte[] data, int index) - A discriminative set of the $index-th operand
// To fetch:
// Fetch() - Call Fetch() on the every element in $InternalArray
// FetchIndiscriminative() - Call Fetch()[0] on every item in $InternalArray
// 
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public class DecodedCompound : IMyMultiDecoded
    {
        public RegisterCapacity Size { get; private set; }
        private readonly IMyDecoded[] InternalArray;
        public DecodedCompound(IMyDecoded[] input)
        {
            InternalArray = input;
        }
        public DecodedCompound(IMyDecoded input1, IMyDecoded input2)
        {
            InternalArray = new IMyDecoded[] { input1, input2 };
        }
        public List<string> Disassemble()
        {
            List<string> Output = new List<string>();

            // Add the output of Disassemble() of each operand onto $Output.
            for (int i = 0; i < InternalArray.Length; i++)
            {
                Output.AddRange(InternalArray[i].Disassemble());
            }

            return Output;
        }
        public List<byte[]> Fetch() => _Fetch(true);
        private List<byte[]> _Fetch(bool discriminative)
        {
            // A private method for fetching, such that Fetch() can comply with the interface.

            List<byte[]> Output = new List<byte[]>();
            for (int i = 0; i < InternalArray.Length; i++)
            {
                // If discriminative, add every sub-operand of each IMyDecoded
                if (discriminative)
                {
                    Output.AddRange(InternalArray[i].Fetch());
                }

                // If indiscriminative, add only the first sub-operand of each IMyDecoded.
                else
                {
                    Output.Add(InternalArray[i].Fetch()[0]);
                }
            }

            return Output;
        }
        public List<byte[]> FetchIndiscriminative() => _Fetch(false);
        public void Initialise(RegisterCapacity size)
        {
            // Initialise every IMyDecoded
            for (int i = 0; i < InternalArray.Length; i++)
            {
                InternalArray[i].Initialise(size);
            }
        }
        public void Set(byte[] data) => InternalArray[0].Set(data);
        public void Set(byte[] data, int index)
        {
            // Itreate through every IMyDecoded
            for (int i = 0; i < InternalArray.Length; i++)
            {
                // If $i is now the index, it must be this IMyDecoded that is to be set. If it is an IMyMultiDecoded,
                // the destination will be set. 
                if (i == index)
                {
                    InternalArray[index].Set(data);
                }

                if (InternalArray[i] is IMyMultiDecoded Cursor)
                {
                    // If the next $i is $index, it points to the source of this IMyMultiDecoded.
                    if (i + 1 == index)
                    {
                        Cursor.SetSource(data);
                        return;
                    }

                    // Increment $i by two because two a source and destination operand were skipped over.
                    i++;
                }                
            }
        }
        public void SetIndiscriminative(byte[] data, int index) => InternalArray[index].Set(data);
        public void SetSource(byte[] data)
        {
            // Set the second operand, which is an undocumented yet implied convention that the
            // second operand is always the source, as in every existing case, it is.
            // As explained in the summary, this is a discriminative operation.
            
            // If the 0th operand is an IMyMultiDecoded, set the source of it.
            if (InternalArray[0] is IMyMultiDecoded Cursor)
            {
                Cursor.SetSource(data);
            }

            // Otherwise set the second operand
            else
            {
                InternalArray[1].Set(data);
            }
        }
    }
}
