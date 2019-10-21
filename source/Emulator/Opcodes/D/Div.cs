// Divide two operands. This is one of few(alongside imul/mul) which has specific signed integer counterparts.
// Using IDIV on a signed number will result in meaningless results. Due to the nature of division, the computational
// approach cannot be generalised. It is best described in Bitwise.Divide().
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public class Div : Opcode
    {
        public Div(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE)
            : base((settings | OpcodeSettings.SIGNED) == settings ? "IDIV" : "DIV", input, settings)
        {
            
        }
        public override void Execute()
        {
            // Fetch operands
            List<byte[]> DestSource = Fetch();

            // Determine the half-lengths of operands. This is because half of the capacity will be
            // for the modulo, the other for the dividend.
            int HalfLength = (int)Capacity;

            // Create a new array to store the result, which is two half lengths, it will have the quotient and modulo
            // copied into it.
            byte[] Result = new byte[(int)Capacity * 2];

            // Two arrays to hold the quotient and modulo.
            byte[] Quotient;
            byte[] Modulo;

            // Perform the division
            Bitwise.Divide(DestSource[0], DestSource[1], (Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity * 2, out Quotient, out Modulo);

            // Copy the results into the result array.
            Array.Copy(Quotient, Result, HalfLength);
            Array.Copy(Modulo, 0, Result, HalfLength, HalfLength);

            // Set the result. This (should) be a split register handle, which will split the Result array in two and set
            // each of the destination operands(D:A) appropriately.
            Set(Result);
        }
    }
}
