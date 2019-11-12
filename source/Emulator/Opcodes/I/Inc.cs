// Increment a register/memory location by one, without setting the carry flag. This is an opcode you will see
// often, mostly because it is one byte shorter than adding one. It used to be only one byte in 32 bit, but
// lost its place(along with dec) in the single-byte opcode table to the introduction of rex prefixes.
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Inc : Opcode
    {
        public Inc(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("INC", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Calculate the result of incrementation
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Increment(Fetch()[0], out Result);

            // Set the result. Bitwise.Increment() does not set the carry flag.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
