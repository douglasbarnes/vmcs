using System;
using System.Collections.Generic;
using System.ComponentModel;
using debugger.Emulator;
using debugger.Util;
using static debugger.Emulator.RegisterGroup;
namespace debugger.Hypervisor
{    
    public class VM : HypervisorBase
    {
        public struct VMSettings
        {
            public int UndoHistoryLength;
            public Action RunCallback;
        }
        private readonly MemorySpace SavedMemory;
        private List<Context> CachedContexts;
       
        public VMSettings CurrentSettings;
        public BindingList<ulong> Breakpoints { get => new BindingList<ulong>(Handle.DeepCopy().Breakpoints); }
        public VM(VMSettings inputSettings, MemorySpace inputMemory) : base("VM", new Context(inputMemory) {
            InstructionPointer = inputMemory.EntryPoint,
            Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
            {
                { ByteCode.SP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) },
                { ByteCode.BP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) } }),          
            })            
        {
            CurrentSettings = inputSettings;
            RunComplete += CurrentSettings.RunCallback;
            SavedMemory = inputMemory.DeepCopy();
        }
        public void Reset()
        {
            Handle.Invoke(() =>
            {
                Context VMContext = Handle.DeepCopy();
                VMContext.Memory = SavedMemory.DeepCopy();
                VMContext.InstructionPointer = SavedMemory.EntryPoint;
                VMContext.Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
                    {
                                { ByteCode.SP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) },
                                { ByteCode.BP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) }
                    });
                VMContext.Flags = new FlagSet();
            });            
        }
        public Dictionary<string, ulong> GetRegisters(RegisterCapacity registerSize)
        {
            Context Cloned = Handle.ShallowCopy();
            Dictionary<ByteCode, byte[]> Registers = Cloned.Registers.FetchAll();
            Dictionary<string, ulong> ParsedRegisters = new Dictionary<string, ulong>();
            foreach (var Reg in Registers)
            {
                ParsedRegisters.Add(Disassembly.DisassembleRegister(Reg.Key, registerSize), BitConverter.ToUInt64(Reg.Value,0));
            }
            ParsedRegisters.Add("RIP", Cloned.InstructionPointer);
            return ParsedRegisters;
        }
    }   
}
