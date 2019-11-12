// Subtract operand1 from operand0 and store in operand0. See Bitwise.Subtract()
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Sub : Opcode
    {
        private bool UseBorrow;
        public Sub(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE, bool useBorrow = false) : base((useBorrow) ? "SBB" : "SUB", input, settings)
        {
            // Whether the carry flag will be used as a borrow.
            UseBorrow = useBorrow;            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // Subtract operands[1] from operands[0]. If the carry flag is set, and would be used as a borrow, tell Bitwise.Subtract to start with a borrow.
            byte[] Result;
            FlagSet ResultFlags = Bitwise.Subtract(DestSource[0], DestSource[1], out Result, (ControlUnit.Flags.Carry == FlagState.ON && UseBorrow));

            // Set the results
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
