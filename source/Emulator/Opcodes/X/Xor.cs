// A bitwise XOR on two operands.
// Another use of the method is to zero a register, which is a shorter alternative to mov rax, 0 or equivalent.
// E.g,
//  XOR RAX, RAX
// , is two bytes. Modern processors will recognise this and know to zero the register, so it is also as fast as mov.
// To XOR a value, apply XOR logic to each bit of the two operands in paralell,
// E.g,
//  0x4E XOR 0x2F = 0x61
//  0x4E = [01001110]
//  0x2F = [00101111]
//  0 1 0 0 1 1 1 0
//  0 0 1 0 1 1 1 1
//  ---------------
//  0 1 1 0 0 0 0 1 = 0x61
// An XOR can also be thought of as result[i] = input1[i] != input2[i],  where the arrays are bool arrays representing each bit.
// An interesting result of XOR is that the result ^ input1 = input2, such that (inputi ^ input2) ^ input1 = input2, which also
// works conversely.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Xor : Opcode
    {
        public Xor(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("XOR", input, settings)
        {
            
        }

        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // XOR the operands and store in result
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Xor(DestSource[0], DestSource[1], out Result);

            // Set the result and flags.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
