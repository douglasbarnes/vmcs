using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Neg : Opcode
    {        
        private readonly FlagSet ResultFlags;
        private readonly byte[] Result;
        public Neg(DecodedTypes.IMyDecoded input, OpcodeSettings settings=OpcodeSettings.NONE) : base("NEG", input, settings)
        {
            byte[] Operand = Fetch()[0];
            ResultFlags = Bitwise.Negate(Operand, out Result);
        }
        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
