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
        public event Action RunComplete = () => { };
        public HypervisorBase(string inputName, Context inputContext, HandleParameters handleParameters = HandleParameters.None) //new handle from context
        {
            inputContext.Registers = new RegisterGroup(new Dictionary<ByteCode, RegisterGroup.Register>()
            {
                { ByteCode.SP, new RegisterGroup.Register(inputContext.Memory.SegmentMap[".stack"].StartAddr) },
                { ByteCode.BP, new RegisterGroup.Register(inputContext.Memory.SegmentMap[".stack"].StartAddr) },
            });
            Handle = new Handle(inputName, inputContext, handleParameters);
        }
        public virtual Status Run(bool Step = false)
        {
            return Handle.Run(Step);
        }
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            Task<Status> RunTask = new Task<Status>(() => Run(Step));
            RunTask.Start();
            Status Result = await RunTask;
            RunComplete.Invoke();
            return Result;
        }
        public Dictionary<string, bool> GetFlags()
        {
            FlagSet VMFlags = Handle.ShallowCopy().Flags;
            return new Dictionary<string, bool>()
                {
                {"Carry", VMFlags.Carry         == FlagState.On},
                {"Parity", VMFlags.Parity       == FlagState.On},
                {"Auxiliary", VMFlags.Auxiliary == FlagState.On},
                {"Zero", VMFlags.Zero           == FlagState.On},
                {"Sign", VMFlags.Sign           == FlagState.On},
                {"Overflow", VMFlags.Overflow   == FlagState.On},
                };
        }
        public MemorySpace GetMemory()
        {
            return Handle.ShallowCopy().Memory;
        }
        public void Dispose()
        {
            Handle.Dispose();
        }

    }
}
