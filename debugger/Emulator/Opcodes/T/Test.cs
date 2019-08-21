using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Test : Opcode
    {
        readonly FlagSet ResultFlags;
        public Test(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("TEST", input, settings)
        {
            List<byte[]> DestSource = Fetch();          
            ResultFlags = Bitwise.And(DestSource[0], DestSource[1], out _); ;
        }
        public override void Execute()
        {
            //test is to and as cmp is to sub          
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
