// VM is the standard class for using the ControlUnit. 
// Its capabilities include
//  -Creating a context out of a MemorySpace instance.
//  -
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
            Ready = true;
            Handle.ShallowCopy().Breakpoints = Breakpoints;
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
        public void Jump(ulong address)
        {
            if (Handle.ShallowCopy().Memory[address] == 0x00)
            {

            }
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
