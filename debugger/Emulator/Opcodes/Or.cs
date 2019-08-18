using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Or : Opcode
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        public Or(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base("OR", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            ResultFlags = Bitwise.Or(DestSource[0], DestSource[1], out Result); // fix bitwise stuff
        }

        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
