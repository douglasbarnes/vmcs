// Add - add two operands. ADD has one variant, add with carry(ADC). This will start the addition with
// the carry set(if the carry flag is set when using ADD, it is ignored) and use it. It is very useful
// for adding large numbers, e.g 128 bit numbers. 
// Here is how you can do that,
//  Let RDX:RAX hold one 128 bit number, where RDX holds the upper bytes, and RBX:RCX, where RBX holds the upper bytes.
//  ADD RAX, RCX
//  ADC RDX, RBX
// It is that simple. At the end if there was a carry into the 129th(out of the MSB), the carry flag will be set.
// This works because if RAX and RCX carried out of the MSB, the carry flag will be set.  This is then accounted for
// int he addition of RDX and RBX. The resulting 128 bit sum will be stored in RDX:RAX. See Bitwise.Add().
using debugger.Util;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Add : Opcode 
    {
        private readonly bool UseCarry;
        public Add(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE, bool useCarry = false) : base((useCarry) ? "ADC" : "ADD", input, settings)
        {
            UseCarry = useCarry;            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();
            byte[] Result;

            // Perform addition. If (ControlUnit.Flags.Carry == FlagState.ON && UseCarry), the addition will start with a carry. See Bitwise.Add().
            FlagSet ResultFlags = Bitwise.Add(DestSource[0], DestSource[1], out Result, (ControlUnit.Flags.Carry == FlagState.ON && UseCarry));

            // Set results
            ControlUnit.SetFlags(ResultFlags);
            Set(Result);
        }
    }
}
