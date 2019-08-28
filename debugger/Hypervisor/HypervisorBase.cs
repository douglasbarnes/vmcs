using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using debugger.Emulator;

using static debugger.Emulator.ControlUnit;
namespace debugger.Hypervisor
{
    public abstract class HypervisorBase : IDisposable
    {
        protected internal readonly Handle Handle;
        public delegate void RunCallback(Context input);
        public event RunCallback RunComplete = (input) => { };
        public HypervisorBase(string inputName, Context inputContext, HandleParameters handleParameters = HandleParameters.NONE) //new handle from context
        {
            inputContext.Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>
            {
                { XRegCode.SP, inputContext.Memory.SegmentMap[".stack"].StartAddr },
                { XRegCode.BP, inputContext.Memory.SegmentMap[".stack"].StartAddr },
            });
            Handle = new Handle(inputName, inputContext, handleParameters);
        }
        public virtual Status Run(bool Step = false) => Handle.Run(Step);
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            Task<Status> RunTask = new Task<Status>(() => Run(Step));
            RunTask.Start();
            Status Result = await RunTask;
            RunComplete.Invoke(Handle.DeepCopy());
            return Result;
        }
        public Dictionary<string, bool> GetFlags()
        {
            FlagSet VMFlags = Handle.ShallowCopy().Flags;
            return new Dictionary<string, bool>()
                {
                {"Carry", VMFlags.Carry         == FlagState.ON},
                {"Parity", VMFlags.Parity       == FlagState.ON},
                {"Auxiliary", VMFlags.Auxiliary == FlagState.ON},
                {"Zero", VMFlags.Zero           == FlagState.ON},
                {"Sign", VMFlags.Sign           == FlagState.ON},
                {"Overflow", VMFlags.Overflow   == FlagState.ON},
                };
        }
        public MemorySpace GetMemory() => Handle.ShallowCopy().Memory;
        public void Dispose()
        {
            Handle.Dispose();
        }

    }
}
