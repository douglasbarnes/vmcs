// The context class contains all the necessary data to set up a control unit with data.
// All of the following variables are required for the ControlUnit to run.
// It also enables the easy manipulation of the ControlUnit, such that the context can be swapped
// out, modified, and loaded back in.
using debugger.Util;
namespace debugger.Emulator
{
    public class Context
    {
        public FlagSet Flags = new FlagSet(FlagState.OFF);
        public MemorySpace Memory;
        public RegisterGroup Registers = new RegisterGroup();
        public ulong InstructionPointer;
        public ListeningList<ulong> Breakpoints = new ListeningList<ulong>();
        public Context()
        {
            Memory = new MemorySpace(new byte[] { 0x00 });
        }
        public Context(MemorySpace memory)
        {
            Memory = memory;
            InstructionPointer = Memory.EntryPoint;
        }

        private Context(Context toClone)
        {
            // Create a deep copy of each member in the context
            // This means that each variable will have a different instance, but with the same data as that of $toClone
            // If this wasn't used, changing addresses in $this.Memory would change the same addresses in $toClone.Memory
            // as with $Breakpoints and $Registers. However this isn't necessary for $Flags and $InstructionPointer because
            // they are value types--a deep copy is taken regardless.

            Flags = toClone.Flags;
            InstructionPointer = toClone.InstructionPointer;
            Memory = toClone.Memory.DeepCopy();
            Breakpoints = toClone.Breakpoints.DeepCopy();
            Registers = toClone.Registers.DeepCopy();
        }
        public Context DeepCopy() => new Context(this);
    }
}
