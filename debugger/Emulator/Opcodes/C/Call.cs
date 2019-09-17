using System;
namespace debugger.Emulator.Opcodes
{
    public class Call : Opcode
    {
        public Call(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base("CALL", input, settings,
                  input is DecodedTypes.Immediate ? RegisterCapacity.DWORD : RegisterCapacity.QWORD )
        {
        } 
        public override void Execute()
        {
            StackPush(BitConverter.GetBytes(ControlUnit.InstructionPointer));
            ControlUnit.Jump(BitConverter.ToUInt64(Fetch()[0], 0));
        }
    }
}
