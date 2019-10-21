// SplitRegisterHandle allows the concatenation of two registers to form one operand. 
// By design it has a few limitations,
//  1. Registers must be from the GP table, as there is no instance of an instruction where this is not the case.
//  2. The two registers must be of the same size.
// The purpose of a SplitRegisterHandle is for operands such as MUL/IMUL and DIV/IDIV which require two registers
// as operands.
// There is one setting for a SplitRegisterHandle,
//  FETCH_UPPER - By default, the upper register is never appended to the byte[] returned in Fetch(). By enabling
//                the setting, the two will be concatenated to form a single byte[] of twice the size.
//                For instance, a SplitRegisterHandle in DIV is used a a source operand. A REX.W + DIV uses both
//                RAX and RDX as parts of said SplitRegisterHandle, where the D register holds the upper bytes.
//                This is denoted as RDX:RAX. Obviously to divide this value(the concatenation of both), the 
//                fetched byte[] would have to contain both.
//                It is not used in every case though. For example, REX.W + MUL uses RAX:RDX as its destination,
//                but only RAX is used as a coefficient not RDX, so RDX would never need to be fetched, only set
//                with the upper bytes of the result. 
//                Some examples,             
//                 
//                 MUL EBX
//                 In MUL, A:D can be left as implied, be used to seeing it either way. In this instruction                  
//                            
//                 IMUL AX, CL
//                 A special exception. When the operand is a BYTE(e.g $CL), $AX will be the dest instead of
//                 AL:DL(this never exists, it is always AX). Also when IMUL is disassembled, the destination
//                 register(s) are usually left in the disassembly because there are opcodes where the 
//                 destination can be encoded as another register in the opcode. Read the MUL class file for more.
//
//                 IDIV RDX
//                 Divide RAX:RDX by RDX(this is valid, and so would RAX). and store the result in RAX. As opposed to
//                 IMUL having the ability to specify the destination, IDIV does not, therefore the dividend and
//                 destination are always implied. Read the DIV class files for some more awesome stuff.
// SplitRegisterHandle only supports two different registers. If bytes were to be split across two halves of the same
// register, a RegisterHandle of twice the size with a NO_INIT bit set would do the job.
// The size used to initialise the SplitRegisterHandle should be the size of each register NOT the two registers combined.
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public enum SplitRegisterHandleSettings
    {
        NONE = 0,
        FETCH_UPPER = 1,
    }
    public class SplitRegisterHandle : IMyDecoded
    {
        public RegisterCapacity Size { get; private set; }
        public SplitRegisterHandleSettings Settings;
        private readonly ControlUnit.RegisterHandle Upper;
        private readonly ControlUnit.RegisterHandle Lower;
        public SplitRegisterHandle(XRegCode lower, XRegCode upper, SplitRegisterHandleSettings settings = SplitRegisterHandleSettings.NONE)
        {
            // Create the handles for the two registers ready so they do not have to be created more than once.
            Upper = new ControlUnit.RegisterHandle(upper, RegisterTable.GP);
            Lower = new ControlUnit.RegisterHandle(lower, RegisterTable.GP);
            Settings = settings;
        }
        public void Initialise(RegisterCapacity size)
        {
            Size = size;
            Upper.Initialise(size);
            Lower.Initialise(size);
        }
        public List<string> Disassemble()
            => new List<string>() { $"{Upper.Disassemble()[0]}:{Lower.Disassemble()[0]}" };

        public List<byte[]> Fetch()
        {
            if ((Settings | SplitRegisterHandleSettings.FETCH_UPPER) == Settings)
            {
                // Create an output buffer of twice the size(because $upper and $lower have the same size)
                byte[] Output = new byte[(int)Size * 2];

                // Copy the contents of $lower into the lower half of the output.
                System.Array.Copy(Lower.FetchOnce(), Output, (int)Size);

                // Copy the contents of $upper into the upper half of the output.
                System.Array.Copy(Upper.FetchOnce(), 0, Output, (int)Size, (int)Size);
                return new List<byte[]> { Output };
            }
            else
            {
                // Just return the lower bytes if the FETCH_UPPER bit is not set.
                return new List<byte[]> { Lower.FetchOnce() };
            }
        }

        public void Set(byte[] data)
        {
            // The input data must be the size of $size * 2. Otherwise there would be no need for a SplitRegisterHandle.

            // Set $upper to the upper bytes of $data (Take $size bytes from the end of $data)
            Upper.Set(Bitwise.Subarray(data, (int)Size));

            // Set $lower to the lower $size bytes of $data.
            Lower.Set(Bitwise.Cut(data, (int)Size));
        }
    }
}
