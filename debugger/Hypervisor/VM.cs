using System;
using System.Collections.Generic;
using System.ComponentModel;
using debugger.Emulator;
using debugger.Util;
namespace debugger.Hypervisor
{
    
    public class VM : HypervisorBase
    {
        
        private readonly MemorySpace SavedMemory;       
        public BindingList<ulong> Breakpoints { get => new BindingList<ulong>(Handle.ShallowCopy().Breakpoints); }
        public VM(MemorySpace inputMemory) : base("VM", new Context(inputMemory) {
            InstructionPointer = inputMemory.EntryPoint,
            Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
            {
                { XRegCode.SP, inputMemory.SegmentMap[".stack"].StartAddr },
                { XRegCode.BP, inputMemory.SegmentMap[".stack"].StartAddr }
            }),          
            })            
        {
            SavedMemory = inputMemory.DeepCopy();
        }
        public void Reset()
        {
            Handle.Invoke(() =>
            {
                Context VMContext = Handle.ShallowCopy();
                VMContext.Memory = SavedMemory.DeepCopy();
                VMContext.InstructionPointer = SavedMemory.EntryPoint;
                VMContext.Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
                    {
                                { XRegCode.SP, SavedMemory.SegmentMap[".stack"].StartAddr },
                                { XRegCode.BP, SavedMemory.SegmentMap[".stack"].StartAddr }
                    });
                VMContext.Flags = new FlagSet();
            });            
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
