// RET - Return from a call. 
// More specifically,
// - Pop 8 bytes from the stack
// - Jump to the effective address of the popped bytes.
// A variation of the return opcode also has an immediate word operand. This immediate operand is added on to
// $RSP after the next address is popped off the stack.
using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Ret : Opcode
    {        
        public Ret(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("RET", input, settings, RegisterCapacity.WORD)
        {

        }
        public override void Execute()
        {
            // Pop off a QWORD from the stack and jump to it.
            ControlUnit.Jump(BitConverter.ToUInt64(StackPop(RegisterCapacity.QWORD), 0));

            // If there is an immediate operand, add it on to RSP.
            List<byte[]> Operands = Fetch();
            if (Operands.Count > 0)
            {               
                // Create a new handle to RSP
                ControlUnit.RegisterHandle StackPointer = new ControlUnit.RegisterHandle(XRegCode.SP, RegisterTable.GP, RegisterCapacity.QWORD);

                // Add the zero extended immediate. Important that it is not sign extended, or it could decrement the stack pointer.
                byte[] NewSP;
                Bitwise.Add(StackPointer.FetchOnce(), Bitwise.ZeroExtend(Operands[0], 8), out NewSP);

                // Set the new SP.
                StackPointer.Set(NewSP);
            }
        }
    }
}
