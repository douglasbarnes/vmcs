using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Cmp : Opcode
    {
        readonly FlagSet Result;
        public Cmp(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("CMP", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            Result = Bitwise.Subtract(DestSource[0], DestSource[1], (int)Capacity, out _);
        }
        public Cmp(DecodedTypes.StringOperation input, OpcodeSettings settings = OpcodeSettings.NONE)
            : base("CMPS", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            Result = Bitwise.Subtract(DestSource[0], DestSource[1], (int)Capacity, out _);
        }
        public override void Execute()
        {
            //basically all cmp does flags-wise is subtract, but doesn't care about the result               
            ControlUnit.SetFlags(Result);
        }
    }
}
