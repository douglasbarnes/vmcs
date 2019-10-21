// Movx covers movsx and movzx; Move sign-extend and Move zero-extend. This opcode allows
// extension and moving to be combined into one opcode. If you want only to extend, you
// can just use movsxd rax, eax for example. As you have just seen, the two operands for
// the instruction have different sizes. It is no simple pattern either, there are zero extend
// opcodes for rm8 -> rm16/32/64 and rm16 -> rm32/64. Movsxd also has dword opcodes. This poses
// quite a problem in the implementation in the program, such that the $desiredSourceSize has
// to be specified by the caller. After many revisions, I have determined this is the simplest
// way to do this. There is no instruction for zero extending a DWORD onto a QWORD register.
// This is because the MOV operand will automatically zero extend a DWORD to fill the QWORD
// register containing it. E.g, mov eax, eax will empty the upper bytes of rax. This behaviour
// does not happen with memory pointers.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Movx : Opcode
    {
        readonly byte[] Result;
        readonly DecodedTypes.IMyDecoded Input;
        readonly RegisterCapacity DestSize;
        readonly RegisterCapacity SourceSize;
        public Movx(DecodedTypes.IMyDecoded input, string mnemonic, bool signExtend, RegisterCapacity desiredSourceSize, OpcodeSettings settings = OpcodeSettings.NONE) : base(mnemonic, input, settings)
        {
            // Store the input for use in disassembly later. This is cheaper than working out the disassembly here, as not every caller
            // is going to disassemble.
            Input = input;

            // The size of the source.
            SourceSize = desiredSourceSize;

            // Store the size of destination as it is about to be changed.
            DestSize = Capacity;

            // Switch to the size of the source before fetching it.
            Capacity = desiredSourceSize;
            byte[] SourceBytes = Fetch()[1];

            // Return back to the size the destination is.
            Capacity = DestSize;

            // Sign extend the source bytes to the size of the destination.
            Result = (signExtend) ? Bitwise.SignExtend(SourceBytes, (byte)Capacity) : Bitwise.ZeroExtend(SourceBytes, (byte)Capacity);
        }
        public override void Execute()
        {
            Set(Result);
        }
        public override List<string> Disassemble()
        {
            // There is a case where an immediate dword is fetched and sign extended, which would not be the case here.
            if (Input is DecodedTypes.ModRM modrm)
            {
                // A few steps are required to mimic the behaviour seen in the constructor in the disassembly output.

                // Initialise the modrm with the size of the source.
                modrm.Initialise(SourceSize);

                // Change the destination back to its normal size. (This opcode is always using RM(swap) encoding, so
                // modrm.Source is actually the destination. See ModRM.
                modrm.Source.Size = (DestSize);
            }

            // Disassemble as normal.
            return base.Disassemble();
        }
    }

}
