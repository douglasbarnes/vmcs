// Call, call to a function somewhere else in memory. Call is very similar to a jump, in that it
// is another way for the programmer to change the value of RIP. 
// Really, it is a jump with an extra push.
// - Push $RIP
// - Jump to operand
// It is important to note some things.
// - When performing a relative jump, the relative value is add to rip at the end of the call
//   instruction, not the beginning or half way through, always at the end. At this point, RIP
//   would be pointing to the first byte of the next instruction.
// - The value of RIP pushed on the stack is the address of the next instruction after the call
//   For instance,
//    0x100 - Call +0x10
//    0x105 - Add eax, eax
//   The address pushed to the stack would be 0x105(zero extended to 8 bytes)
//   This address will be used by the ret instruction to return back to this position, so it is
//   very important that the stack is managed carefully, if you pop once more than pushed in the
//   stack frame, the ret instruction will return to a different address. This is the premise of
//   stack buffer overflow attacks, an exploitation technique for an attacker to run their own
//   instructions by returning to their own code rather than the address intended by the programmer,
//   so it is very important you don't pwn yourself.
using System;
namespace debugger.Emulator.Opcodes
{
    public class Call : Opcode
    {
        public Call(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE)
            : base("CALL", input, settings,
                  input is DecodedTypes.Immediate ? RegisterCapacity.DWORD : RegisterCapacity.QWORD)
        {
        }
        public override void Execute()
        {
            // Push $RIP. The opcode base class/inputs being initialised would have already read all the bytes in the instruction,
            // so it is ok to assume that here.
            StackPush(BitConverter.GetBytes(ControlUnit.InstructionPointer));

            // Jump to the operand. If it is relative, that is the responsibility of the IMyDecoded to work that out.
            ControlUnit.Jump(BitConverter.ToUInt64(Fetch()[0], 0));
        }
    }
}
