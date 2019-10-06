using System;
using System.Collections.Generic;
using debugger.Emulator;
using debugger.Util;
namespace debugger.Hypervisor
{    
    public class VM : HypervisorBase
    {
        public bool Ready { get; private set; } = false;
        public ListeningList<ulong> Breakpoints;
        public VM(MemorySpace inputMemory) : base("VM", new Context(inputMemory) {
            Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
            {
                { XRegCode.SP, inputMemory.SegmentMap[".stack"].StartAddr },
                { XRegCode.BP, inputMemory.SegmentMap[".stack"].StartAddr }
            }),          
            })            
        {
        }
        public VM() : base("VM", new Context())
        {
            OnFlash += (context) =>
            {
                Ready = true;
                context.Breakpoints = Breakpoints;
            };
            Breakpoints = Handle.ShallowCopy().Breakpoints;
        }
        public Dictionary<string, ulong> GetRegisters(RegisterCapacity registerSize)
        {
            List<Register> Registers = new List<Register>();
            Dictionary<string, ulong> ParsedRegisters = new Dictionary<string, ulong>()
            {
                { "RIP", GetRIP()}
            };
            Handle.Invoke(new Action(() => { Registers = ControlUnit.FetchAll(registerSize); } ));
            for (int i = 0; i < Registers.Count; i++)
            {
                ParsedRegisters.Add(Registers[i].Mnemonic, BitConverter.ToUInt64(Bitwise.ZeroExtend(Registers[i].Value,8),0));
            }           
            return ParsedRegisters;
        }
        public ulong GetRIP() => Handle.ShallowCopy().InstructionPointer;
    }   
}
