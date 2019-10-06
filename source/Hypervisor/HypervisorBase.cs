using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using debugger.Emulator;

using static debugger.Emulator.ControlUnit;
namespace debugger.Hypervisor
{
    public abstract class HypervisorBase
    {
        protected Handle Handle { get; private set; }
        public string HandleName { get => Handle.HandleName; }
        public int HandleID { get => Handle.HandleID; }
        public delegate void OnRunDelegate(Context input);
        public delegate void OnFlashDelegate(Context input);
        public event OnRunDelegate OnRunComplete = (input) => { };        
        public event OnFlashDelegate OnFlash = (input) => { };
        public HypervisorBase(string inputName, Context inputContext, HandleParameters handleParameters = HandleParameters.NONE) //new handle from context
        {
            Handle = new Handle(inputName, inputContext, handleParameters);
        }
        public virtual Status Run(bool Step = false) => Handle.Run(Step);
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            Task<Status> RunTask = new Task<Status>(() => Run(Step));
            RunTask.Start();
            Status Result = await RunTask;
            OnRunComplete.Invoke(Handle.DeepCopy());
            return Result;
        }
        public void Flash(MemorySpace input)
        {
            Context newContext = new Context(input.DeepCopy())
            {
                Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
                      {
                                  { XRegCode.SP, input.SegmentMap[".stack"].StartAddr },
                                  { XRegCode.BP, input.SegmentMap[".stack"].StartAddr }
                      }),
                Flags = new FlagSet(),
                Breakpoints = new Util.ListeningList<ulong>()
            };
            Handle.UpdateContext(newContext);
            OnFlash.Invoke(newContext);
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
    }
}
