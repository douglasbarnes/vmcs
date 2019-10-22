// HypervisorBase is the base class for hypervisor classes. Hypervisor classes are classes which make use of the
// adjacent layers in order to improve and generalise their methods of communication. This is done through
// procedures and functions encapsulated in methods. Concurrency can also be achieved through the provided
// RunAsync() method, which can be used to execute instructions off the UI thread, such that further processing
// can be done whilst waiting for the instructions to finish executing.
// To do this, I would recommend,
// - Creating a task with an anonymous method to run RunAsync
// - Do other work
// - At the end of the other work, when the result is needed, use $task.Wait
// If you do need the result immediately afterwards, awaiting RunAsync() is better than Run() because the work
// will not be done on the UI thread, which would affect the user the most. Unless an absurd amount of processing 
// is being carried out, any operations off the UI thread I have found to be fine in my experience.
// When the result is not immediately needed, RunAsync() can be called without any sort of awaiting, and the process
// can continue. The result of which can then be recieved through events and their callbacks. An listener for the
// RunComplete event should handle the result, rather than the caller in general. Of course there are scenarios when
// it is simpler to just call and await.
// Most user level features should be implemented in a HypervisorBase derived class rather than the ControlUnit. The
// ControlUnit(and equivalent layered classes) should be reserved for x86-64 specification features, such as the
// implementation of a new opcode. Hypervisors should implement desired features, such as how the user sets a breakpoint
// or the user modification of memory. There is no point in the x86-64 specification which dictates this behaviour,
// as it does not concern itself with these tasks. Naturally, to make life easier, some "quality of life" components,
// notably disassembly have been integrated into ControlUnit. However, I think the number of causes to do so is very
// finite. The output of disassembly is also in a very basic fashion, it is just a list of separate lexical mnemonics
// that form an instruction. This is also something to consider.
using debugger.Emulator;
using System.Collections.Generic;
using System.Threading.Tasks;
using static debugger.Emulator.ControlUnit;
namespace debugger.Hypervisor
{
    public abstract class HypervisorBase
    {
        protected Handle Handle { get; private set; }
        public string HandleName { get => Handle.HandleName; }
        public int HandleID { get => Handle.HandleID; }
        public delegate void OnRunDelegate(Status input);
        public delegate void OnFlashDelegate(Context input);
        public event OnRunDelegate RunComplete = (input) => { };
        public event OnFlashDelegate Flash = (context) => { };
        public HypervisorBase(string inputName, Context inputContext, HandleParameters handleParameters = HandleParameters.NONE)
        {            
            // Automatically create a new handle for the derived class. The derived class should rarely have to worry about handles,
            // but for the scope of advanced usage in the future, it is left as protected. I don't wish to restrict the modular 
            // capabilities of the program.
            Handle = new Handle(inputName, inputContext, handleParameters);
        }
        protected virtual void OnFlash(Context input)
        {
            // In general, it is good .NET convention to let the derived class be the
            // first to know of an event, such that it can be handled before an event
            // is invoked.
            Flash.Invoke(input);
        }
        protected virtual void OnRunComplete(Status result)
        {
            // See OnFlash()
            RunComplete.Invoke(result);
        }
        public virtual Status Run(bool Step = false) => Handle.Run(Step);
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            // Task.Run() is generally accepted as the best way to perform asynchrous tasks.
            Status Result = await Task.Run(() => Run(Step));

            // Raise event
            OnRunComplete(Result);

            return Result;
        }
        public virtual void FlashMemory(MemorySpace input)
        {
            // Create a new context from $input.
            // Automatically assign the stack and base pointer registers to the stack start.
            Context newContext = new Context(input.DeepCopy())
            {
                Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
                      {
                                  { XRegCode.BP, input.SegmentMap[".stack"].Range.Start },
                                  { XRegCode.SP, input.SegmentMap[".stack"].Range.Start },
                      }),
            };

            // Use Handle to update the context with the new
            Handle.UpdateContext(newContext);

            // Raise OnFlash event
            OnFlash(newContext);
        }

        public Dictionary<string, bool> GetFlags()
        {
            // Create a copy of the flags. ShallowCopy() a much better performance than DeepCopy(). As
            // FlagSet is a struct, there is no reason not to. Use ShallowCopy() where possible.
            FlagSet VMFlags = Handle.ShallowCopy().Flags;

            // Return string representations of each flag state.
            return new Dictionary<string, bool>()
                {
                {"Carry", VMFlags.Carry         == FlagState.ON},
                {"Parity", VMFlags.Parity       == FlagState.ON},
                {"Auxiliary", VMFlags.Auxiliary == FlagState.ON},
                {"Zero", VMFlags.Zero           == FlagState.ON},
                {"Sign", VMFlags.Sign           == FlagState.ON},
                {"Overflow", VMFlags.Overflow   == FlagState.ON},
                {"Direction", VMFlags.Direction   == FlagState.ON},
                };
        }
        public MemorySpace GetMemory() => Handle.ShallowCopy().Memory;

    }
}
