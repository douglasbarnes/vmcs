// Push a value on to the stack
// - Set *RSP to operand.
// - Decrement $RSP by length of operand.
// In x86-64 this can either be 8 bytes, 2 bytes, or in some cases 1 byte. 99.99% of the time, this will be 8 bytes.
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Push : Opcode
    {
        byte[] Result;
        //push imm8 is valid but not push r8                // no 8/32 bit mode for reg push  
        public Push(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE)
            : base("PUSH", input, settings,
                  
                  // When pushing an immediate, the default(no prefix) is to push a immediate DWORD.
                  // When pushing a register, the default is to push the QWORD register. A DWORD register cannot be pushed.
                  // Both of these can however be WORD values if a SIZEOVR prefix is present.
                  (input is DecodedTypes.Immediate) ?
                  (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.DWORD) : 
                  (ControlUnit.LPrefixBuffer.Contains(PrefixByte.SIZEOVR) ? RegisterCapacity.WORD : RegisterCapacity.QWORD)) 
        {
            Result = Fetch()[0];

            // If an operand is a non-WORD/QWORD, it is sign extended to a QWORD
            // Here is a demonstration of this,
            //  https://prnt.sc/outmtv
            // As you can see, 0x6A, which is PUSH IMM8, is sign extended to a QWORD.
            //  https://prnt.sc/ouyxwd
            // Look at the points marked X. An immediate DWORD is pushed at 0x401035 then popped into RAX.
            // RAX became 0x12345678, the DWORD pushed, even though 0x1234 was pushed right before it. Intuition would
            // suggest that the value of RAX should be 0x123456781234, but as shown, 0x12345678 was extended to be a
            // QWORD rather than a DWORD. It is shown in the first screenshot that the bytes for an immediate DWORD push,
            //  68 78 56 34 12
            //  ^
            // are used, so there is no kind of assembler interference happening.
            // Furthermore, this proves that immediate word pushes are not sign extended, even though the manual implies that
            // they ought to.  As you can infer from the second screenshot, 0x1234 was popped into BX, then 0x*f2 was popped into
            // RCX. If 0x1234 had been extended, this would have been reflected in the value of RCX, as zeroes there would be zeroes
            // there, not the exact value pushed at 0x40102f.
            if (Result.Length == 1 || Result.Length == 4) 
            {
                Result = Bitwise.SignExtend(Result, 8);
            }
        }
        public override void Execute()
        {
            StackPush(Result);
        }
    }

}
