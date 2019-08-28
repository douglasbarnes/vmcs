using System;
using System.Collections.Generic;
using System.ComponentModel;
using debugger.Emulator;
using debugger.Util;
namespace debugger.Hypervisor
{
    
    public class VM : HypervisorBase
    {
        
        public struct VMSettings
        {
            public int UndoHistoryLength;
            public RunCallback OnRunComplete;
        }
        private readonly MemorySpace SavedMemory;       
        public VMSettings CurrentSettings;
        public BindingList<ulong> Breakpoints { get => new BindingList<ulong>(Handle.ShallowCopy().Breakpoints); }
        public VM(VMSettings inputSettings, MemorySpace inputMemory) : base("VM", new Context(inputMemory) {
            InstructionPointer = inputMemory.EntryPoint,
            Registers = new RegisterGroup(new Dictionary<XRegCode, ulong>()
            {
                { XRegCode.SP, inputMemory.SegmentMap[".stack"].StartAddr },
                { XRegCode.BP, inputMemory.SegmentMap[".stack"].StartAddr }
            }),          
            })            
        {
            CurrentSettings = inputSettings;
            RunComplete += CurrentSettings.OnRunComplete;
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
            Context Cloned = Handle.ShallowCopy();
            Dictionary<XRegCode, byte[]> Registers = Cloned.Registers.FetchAll();
            Dictionary<string, ulong> ParsedRegisters = new Dictionary<string, ulong>
            {
                { "RIP", Cloned.InstructionPointer }
            };
            foreach (var Reg in Registers)
            {
                ParsedRegisters.Add(Disassembly.DisassembleRegister(Reg.Key, registerSize, REX.B), BitConverter.ToUInt64(Reg.Value,0));
            }
           
            return ParsedRegisters;
        }
    }   
}
