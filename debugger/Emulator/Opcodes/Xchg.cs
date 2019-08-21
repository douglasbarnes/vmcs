using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Xchg : Opcode
    {
        readonly List<byte[]> DestSource;
        public Xchg(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base("XCHG", input, settings)
        {
            DestSource = Fetch();
        } 
        public override void Execute()
        {
            Set(DestSource[1]);
            SetSource(DestSource[0]);
        }
    }
}
