// Xchg, presumable short for exchange, switches two values around.
// E.g,
//  MOV EAX, 0x10
//  MOV EBX, 0x20
//  XCHG EAX, EBX
// At the end, EAX=20 EBX=10.
// This can be done with addresses also. When the A register is swapped with another register,
// it has its own one byte opcode.
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Xchg : Opcode
    {
        private readonly DecodedTypes.IMyMultiDecoded Input;
        public Xchg(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("XCHG", input, settings)
        {
            // Store the input such that the source can be set later.
            Input = input;
        }
        public override void Execute()
        {
            // Fetch the operands, set them to the values of each other.
            // By convention Fetch()[0] = $destination, Fetch()[1] = $source.
            List<byte[]>  DestSource = Fetch();
            Set(DestSource[1]);
            Input.SetSource(DestSource[0]);
        }
    }
}
