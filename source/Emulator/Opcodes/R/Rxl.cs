// Rotate left, or Rotate carry left. RCL uses the carry flag as an extra bit in the rotation. See Bitwise.RotateLeft().
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Rxl : Opcode
    {
        private bool UseCarry;
        public Rxl(DecodedTypes.IMyDecoded input, bool useCarry, OpcodeSettings settings = OpcodeSettings.NONE) : base(useCarry ? "RCL" : "ROL", input, settings)
        {
            UseCarry = useCarry;
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> Operands = Fetch();

            // Perform rotation. See Bitwise.RotateLeft().
            byte[] Result;
            FlagSet ResultFlags = Bitwise.RotateLeft(Operands[0], Operands[1][0], UseCarry, ControlUnit.Flags.Carry == FlagState.ON, out Result);

            // Set results.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
