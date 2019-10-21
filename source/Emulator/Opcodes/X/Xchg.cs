using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Xchg : Opcode
    {
        private readonly List<byte[]> DestSource;
        private readonly DecodedTypes.IMyMultiDecoded Input;
        public Xchg(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("XCHG", input, settings)
        {
            DestSource = Fetch();
            Input = input;
        }
        public override void Execute()
        {
            Set(DestSource[1]);
            Input.SetSource(DestSource[0]);
        }
    }
}
