using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Xor : Opcode
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        public Xor(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base("XOR", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            ResultFlags = Bitwise.Xor(DestSource[0], DestSource[1], out Result); // fix bitwise stuff
        }

        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
