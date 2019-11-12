// Decrement a register/memory location. See Inc and Bitwise.Decrement() for more information. 
// Like increment, decrement does not affect the carry flag.
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Dec : Opcode
    {
        public Dec(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("DEC", input, settings)
        { 

        }
        public override void Execute()
        {
            // Perform the decrement
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Decrement(Fetch()[0], out Result);

            // Store results
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
