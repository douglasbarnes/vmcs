using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Dec : Opcode
    {
        readonly byte[] Result;
        readonly FlagSet Flags;
        public Dec(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base("DEC", input, settings)
        {
            Flags = Bitwise.Decrement(Fetch()[0], (int)Capacity, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(Flags);
        }
    }
}
