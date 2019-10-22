// VM is the standard class for using the ControlUnit. 
// Its capabilities include many higher layered functions that improve the quality of life, allowing
// lower layer modules such as the ControlUnit to perform the core functions systematically, whilst
// giving the users specifically tuned control over the ControlUnit.  For example, breakpoints are
// kept as a single list. Every time a context is flashed(See base class), the reference of the new
// breakpoints list is updated to the existing one in this class. This means that the user programmer
// can use this reference constantly, without having to worry about references being lost. For more on
// references, see Util.Core.
using debugger.Emulator;
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Hypervisor
{
    public class VM : HypervisorBase
    {
        public bool Ready
        {
            // A boolean to indicate whether the VM ROM has been flashed yet.
            get;
            private set;
        } = false;
        public ListeningList<ulong> Breakpoints = new ListeningList<ulong>();
        public VM(MemorySpace inputMemory) : base("VM", new Context(inputMemory)
        {
            Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
            {
                { XRegCode.SP, inputMemory.SegmentMap[".stack"].Range.Start },
                { XRegCode.BP, inputMemory.SegmentMap[".stack"].Range.Start }
            }),
        })
        {
            Handle.ShallowCopy().Breakpoints = Breakpoints;
            Ready = true;
        }
        public VM() : base("VM", new Context())
        {
            Handle.ShallowCopy().Breakpoints = Breakpoints;
        }
        protected override void OnFlash(Context input)
        {
            // Once flashed always ready.
            Ready = true;

            // Keep the breakpoints list reference constant to the breakpoints list in this class.
            Handle.ShallowCopy().Breakpoints = Breakpoints;

            // Important to call base class to make sure event gets raised.
            base.OnFlash(input);
        }
        public Dictionary<string, ulong> GetRegisters(RegisterCapacity registerSize)
        {
            List<Register> Registers = new List<Register>();

            // Add RIP manually because it is a register independent of RegisterGroup
            Dictionary<string, ulong> ParsedRegisters = new Dictionary<string, ulong>()
            {
                { "RIP", GetRIP()}
            };

            // Use Invoke to make sure the registers from the correct context are fetched.
            Handle.Invoke(new Action(() => { Registers = ControlUnit.FetchRegisters(registerSize); }));

            // Extract the wanted data(Register mnemonics and the values stored in them) from Registers into ParsedRegisters
            for (int i = 0; i < Registers.Count; i++)
            {
                // Always zero extend for consistency throughout the program. The arithmetic value the represented by the registers is not necessarily
                // of importance at this level, therefore zero extending is more appropiate than sign extending.
                ParsedRegisters.Add(Registers[i].Mnemonic, BitConverter.ToUInt64(Bitwise.ZeroExtend(Registers[i].Value, 8), 0));
            }

            return ParsedRegisters;
        }
        public bool Jump(ulong address)
        {
            // If the address is in the valid arnge of memory. (Remember that it is a ulong so could not be less than 0)
            if (Handle.ShallowCopy().Memory.End > address)
            {
                Handle.Invoke(new Action(() => { ControlUnit.Jump(address); }));
                return true;
            }
            return false;
        }
        public void ToggleBreakpoint(ulong address)
        {
            if (Breakpoints.Contains(address))
            {
                Breakpoints.Remove(address);
            }
            else
            {
                Breakpoints.Add(address);
            }
        }
        public ulong GetRIP() => Handle.ShallowCopy().InstructionPointer;

    }
}
