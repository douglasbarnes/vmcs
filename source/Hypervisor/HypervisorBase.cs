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
        public event OnFlashDelegate Flash;
        public HypervisorBase(string inputName, Context inputContext, HandleParameters handleParameters = HandleParameters.NONE) //new handle from context
        {
            Handle = new Handle(inputName, inputContext, handleParameters);
            Flash += OnFlash;
        }
        protected virtual void OnFlash(Context input)
        {

        }
        public virtual Status Run(bool Step = false) => Handle.Run(Step);
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            Task<Status> RunTask = new Task<Status>(() => Run(Step));
            RunTask.Start();
            Status Result = await RunTask;
            RunComplete.Invoke(await RunTask);
            return Result;
        }
        public virtual void FlashMemory(MemorySpace input)
        {
            Context newContext = new Context(input.DeepCopy())
            {
                Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
                      {
                                  { XRegCode.BP, input.SegmentMap[".stack"].Range.Start },
                                  { XRegCode.SP, input.SegmentMap[".stack"].Range.Start },
                      }),
                Flags = new FlagSet(),
            };
            Handle.UpdateContext(newContext);
            Flash.Invoke(newContext);
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
