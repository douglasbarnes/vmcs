// The Disassembly class file provides some useful disassembly methods. Over time it has become more deprecated, initially it provided most
// of the disassembly, however now that RegisterHandle disassembles registers itself(for the greater good), it has seen less and less use.
// However, it still remains an important generalisation of disassembly across the program for a few reasons.  The standard of disassembly
// is very unstandardised, for example every disassembler will output slightly different disassembly. This is not for case for essential
// parts such as opcode mnemonics and register mnemonics(with the exception of gas syntax), but there are two substantially different ways
// I have seen. One is the gcc/objdump style explicit pointer statements as opposed to nasm. It is a small difference, but when the size of
// a pointer cannot be inferred, e.g
//  mov rax, [rax]
// It is not clear whether you are moving a whole QWORD from [rax] into $rax, or moving a BYTE/DWORD and sign extending it, so in gcc syntax
// you must write,
//  mov rax, QWORD PTR [rax]
// or in nasm,
//  mov rax, QWORD [rax]
// It is a small difference and mostly personal preference, but neither are cross compatible with each other, i.e if you don't write PTR in
// gcc it will throw an error, if you write PTR in nasm it will throw an error. Currently gcc style is used.
// Another is the naming of condition mnemonics. This is an absolute mess, and pretty much you will have to remember all of them because
// every other program will use a different combination.
// Here is a short table of conditions,
//   MNEMONIC                    MEANING
//  B, NAE,C        Below, not equal or above, carry
//  NB, AE, NC      Not below, equal or above, no carry
//  E, Z            Equal, zero
//  NE,NZ           Not equal, not zero
//  BE, NA          Below or equal, not above
//  NBE, A          Not below or equal, above
//  S               Sign
//  NS              No sign
//  PE, P           Parity even, parity
//  PO, NP          Parity odd, no parity
//  NGE, L          Not greater than, less
//  NL, GE          Not less, greater than
//  NG, LE          Not greater, less or equal
//  NLE, G          Not less than or equal, greater
// The ones used in this program are the rightmost in the table. I generally prefer conditions that reference the flags rather than "below" as
// it is clearer to me what they are going to do, but maybe in more circumstances it could be easier to infer the meaning from JB opposed to JC.
// Nasm and gcc accept all of the above. Since the mnemonic it was assembled with cannot be inferred from the machine code, it is a subjective
// decision. If any user modules require condition disassembly, I would definitely recommend using the provided DisassembleCondition() to keep
// it consistent throughout the program.
using debugger.Emulator;
using debugger.Emulator.Opcodes;
using System.Collections.Generic;
namespace debugger.Util
{
    public static class Disassembly
    {
        private readonly static Dictionary<RegisterCapacity, string> SizeMnemonics = new Dictionary<RegisterCapacity, string>() // maybe turn regcap into struct?
        {
            {RegisterCapacity.BYTE, "BYTE"},
            {RegisterCapacity.WORD, "WORD"},
            {RegisterCapacity.DWORD, "DWORD"},
            {RegisterCapacity.QWORD, "QWORD"}
        };
        private readonly static Dictionary<Condition, string> ConditionMnemonics = new Dictionary<Condition, string>()
        {
            { Condition.A, "A" },
            { Condition.NA, "NA" },
            { Condition.C, "C" },
            { Condition.NC, "NC" },
            { Condition.RCXZ, "RCXZ" },
            { Condition.Z, "Z" },
            { Condition.NZ, "NZ" },
            { Condition.G, "G" },
            { Condition.GE, "GE" },
            { Condition.L, "L" },
            { Condition.LE, "LE" },
            { Condition.O, "O" },
            { Condition.NO, "NO" },
            { Condition.S, "S" },
            { Condition.NS, "NS" },
            { Condition.P, "P" },
            { Condition.NP, "NP" }
        };
        public static string DisassembleCondition(Condition condition) => ConditionMnemonics[condition];
        public struct DeconstructedPointer
        {
            // An immediate offset added on to the final result of the pointer
            public long Offset;

            // The Index in a SIB byte. When not scaled, is interchangable with AdditionalReg
            public string IndexReg;

            // The additional reg in a SIB byte. Sometimes called the base.
            public string AdditionalReg;

            // NOT the coefficient of the index reg, rather the power two that gives that coefficient,
            // e.g $IndexScale=log2($coefficient). This is because the scale bits of the SIB byte will be
            // this value.
            public int IndexScale;

            // The number of bytes the pointer points to. See summary for reason why.
            public RegisterCapacity? Size;
        }
        public static string DisassemblePointer(DeconstructedPointer inputPointer)
        {
            // Create an easy and generalised way to disassemble pointers.
            // This is designed to work with any kind of pointer x86-64 permits, including crazy SIB pointers.

            // Start with nothing. If the input was null this is what you will get back.
            string Output = "";

            // If this index register is not null, disassemble it. This would be the scaled index in a SIB byte,
            // or could just be a normal register pointer. It depends on what the caller put in the struct it doesn't matter
            // for pointers with no scale coefficient, however if it is a SIB the scaled index must go in $IndexReg.
            if (inputPointer.IndexReg != null)
            {
                Output += inputPointer.IndexReg;

                // If the scale is greater than zero, add it, i.e don't add eax*1 and alike.
                if (inputPointer.IndexScale > 0)
                {
                    Output += $"*{(int)System.Math.Pow(2, inputPointer.IndexScale)}";
                }

                // If there is another register, an add shows that this is added on. If there is
                // an offset, that will handle this itself.
                if (inputPointer.AdditionalReg != null)
                {
                    Output += '+';
                }
            }

            // If there is an additional reg, add it on to the end.
            if (inputPointer.AdditionalReg != null)
            {
                Output += $"{inputPointer.AdditionalReg}";
            }

            // If there is an offset, 
            if (inputPointer.Offset != 0)
            {
                // If there is a reg being offset, not an absolute pointer, do some quality of life changes.
                if (inputPointer.IndexReg != null || inputPointer.AdditionalReg != null)
                {
                    // Determine its sign and add a +/- accordingly, then take the absolute value of the offset and add it like that.
                    // This just makes it easier to understand what is happening, rather than having to convert twos compliment in your head.
                    Output += $"{(inputPointer.Offset > 0 ? "+" : "-")}0x{System.Math.Abs(inputPointer.Offset).ToString("X")}";
                }
                else
                {
                    // Otherwise it must have been an absolute pointer that could not be negative so add it normally.
                    Output = $"0x{inputPointer.Offset.ToString("X")}";
                }
            }

            return Output;
        }
        public static string DisassembleSize(RegisterCapacity size) => SizeMnemonics[size];
    }

}

