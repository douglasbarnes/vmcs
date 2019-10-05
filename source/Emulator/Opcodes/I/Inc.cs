using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Inc : Opcode
    {
        readonly FlagSet ResultFlags;
        readonly byte[] Result;
        public Inc(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("INC", input, settings)
        {
            ResultFlags = Bitwise.Increment(Fetch()[0], (int)Capacity, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
