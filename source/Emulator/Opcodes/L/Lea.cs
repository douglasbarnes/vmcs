// Load effective address. Calculate the resulting pointer(effective address) of an operand and store it in the destination.
// This is mostly used with SIBs, e.g
//  LEA RAX, [2*RBX+RCX]
// $RAX will now be set to 2*RBX+RCX instead of dereferencing that point and fetching memory at *(2*$RBX+$RCX).
// This is a classic opcode to be used in hand written assembly, as certain arithmetic operations can be combined into a
// much smaller size, although the execution time is likely to be the same. 
// E.g instead of,
//  SHL RAX, 2
//  ADD RAX, 0x12345678
//  MOV RBX, RAX
// (13 bytes)
//  LEA RBX,[RAX*2+0x12345678]
// (8 bytes)
using debugger.Util;
using System;
namespace debugger.Emulator.Opcodes
{
    public class Lea : Opcode
    {
        readonly byte[] SourceAddress;
        public Lea(DecodedTypes.ModRM input, OpcodeSettings settings = OpcodeSettings.NONE) : base("LEA", input, settings)
        {
            // EffectiveAddress property in the ModRM can be very helpful in the opcode. It is always a ulong, therefore does
            // have to be cut to the correct size(nothing will happen in QWORD operation).
            SourceAddress = Bitwise.Cut(BitConverter.GetBytes(input.EffectiveAddress), (int)Capacity);
        }
        public override void Execute()
        {
            Set(SourceAddress);
        }
    }
}
