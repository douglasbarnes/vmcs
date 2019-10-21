// Negate an operand. This only uses one operand, and stored the result in the same place as it was fetched.
// See Bitwise.Negate().
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Neg : Opcode
    {

        public Neg(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("NEG", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Fetch operand(only one is used/provided)
            byte[] Operand = Fetch()[0];

            // Negate it
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Negate(Operand, out Result);

            // Set results.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
