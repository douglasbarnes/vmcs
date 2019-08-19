using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator
{
    public class Context
    {
        public FlagSet Flags = new FlagSet(FlagState.OFF);
        public MemorySpace Memory;
        public RegisterGroup Registers;
        public ulong InstructionPointer;
        public List<ulong> Breakpoints = new List<ulong>();
        public Context(MemorySpace memory)
        {
            Memory = memory;
            InstructionPointer = Memory.EntryPoint;
            Registers = new RegisterGroup();
        }

        private Context(Context toClone)
        {
            Flags = toClone.Flags;
            Memory = toClone.Memory.DeepCopy();
            InstructionPointer = toClone.InstructionPointer; // val type
            Breakpoints = toClone.Breakpoints.DeepCopy();
            Registers = toClone.Registers.DeepCopy();
        }
        public Context DeepCopy()
        {
            return new Context(this);
        }
    }
}
