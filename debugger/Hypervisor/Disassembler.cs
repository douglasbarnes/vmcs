using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using debugger.Emulator;
using debugger.Util;

using static debugger.Emulator.ControlUnit;
namespace debugger.Hypervisor
{
    public class Disassembler : HypervisorBase
    {
        public struct DisassembledItem
        {
            [Flags]
            public enum AddressState
            {
                Default = 0,
            }
            public string DisassembledLine;
            public ulong Address;
            public AddressState AddressInfo;
        }
        private Handle TargetHandle;
        private Context TargetContext { get => TargetHandle.ShallowCopy(); }
        public Disassembler(Handle targetHandle) : base("Disassembler", targetHandle.ShallowCopy(), HandleParameters.DISASSEMBLEMODE)
        {
            TargetHandle = targetHandle;
        }
        public async Task<Dictionary<ulong, DisassembledItem>> Step(ulong count=0)
        {
            ulong IP = Handle.ShallowCopy().InstructionPointer;
            return await Step(IP, IP + count);
        }
        public async Task<Dictionary<ulong, DisassembledItem>> Step(ulong startAddress, ulong endAddress)
        {
            Handle.DeepCopy().InstructionPointer = startAddress;
            Dictionary<ulong, DisassembledItem> Output = new Dictionary<ulong, DisassembledItem>();
            for (ulong CurrentAddr = startAddress; CurrentAddr < endAddress; Handle.Invoke(() => CurrentAddr = Handle.ShallowCopy().InstructionPointer))
            {
                string ExtraInfo;              
                if (CurrentAddr == TargetContext.InstructionPointer)
                {
                    ExtraInfo = "←RIP";
                }
                else
                {
                    ExtraInfo = "    ";
                }
                string Disassembly = JoinDisassembled((await RunAsync(true)).LastDisassembled);
                if (Output.ContainsKey(CurrentAddr))
                {
                    Output[CurrentAddr] = new DisassembledItem()
                    {
                        Address = CurrentAddr,                                         // } 1 space (←rip/4 spaces) 15 spaces {                    
                        DisassembledLine = $"{Core.FormatNumber(CurrentAddr, FormatType.Hex)} {ExtraInfo}               {Disassembly}"
                    };
                } else
                {
                    Output.Add(CurrentAddr, new DisassembledItem()
                    {
                        Address = CurrentAddr,                                         // } 1 space (←rip/4 spaces) 15 spaces {                    
                        DisassembledLine = $"{Core.FormatNumber(CurrentAddr, FormatType.Hex)} {ExtraInfo}               {Disassembly}"
                    });
                }
                

            }
            return Output;
        }
        private string JoinDisassembled(List<string> RawDisassembled)
        {
            if (RawDisassembled.Count < 3)
            {
                return string.Join(" ", RawDisassembled);
            }
            else
            {
                string Output = $"{RawDisassembled[0]} {RawDisassembled[1]}";
                for (int i = 2; i < RawDisassembled.Count; i++)
                {
                    Output += ", " + RawDisassembled[i];
                }
                return Output;
            }
        }
        public async Task<List<DisassembledItem>> StepAll()
        {
            Context DisasContext = Handle.ShallowCopy();
            return new List<DisassembledItem>((await Step(DisasContext.Memory.EntryPoint, DisasContext.Memory.SegmentMap[".main"].End)).Values);
        }
    }

}
