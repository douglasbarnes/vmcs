// Rotate right/rotate carry right. The carry flag will be considered as an extra bit in the rotation if RCR is used. See Bitwise.RotateRight()
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Rxr : Opcode
    {
        private bool UseCarry;        
        public Rxr(DecodedTypes.IMyDecoded input, bool useCarry, OpcodeSettings settings = OpcodeSettings.NONE) : base(useCarry ? "RCR" : "ROR", input, settings)
        {
            UseCarry = useCarry;
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> Operands = Fetch();                

            // Perform the rotate and store the results. The callee will ignore "ControlUnit.Flags.Carry == FlagState.ON" if $UseCarry == false.
            byte[] Result;
            FlagSet ResultFlags = Bitwise.RotateRight(Operands[0], Operands[1][0], UseCarry, ControlUnit.Flags.Carry == FlagState.ON, out Result);

            // Set the results.
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
