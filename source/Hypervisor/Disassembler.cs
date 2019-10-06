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
                NONE = 0,
                RIP = 1,
            }
            public string DisassembledLine;
            public AddressState AddressInfo;
        }
        private readonly Context TargetContext;
        public Disassembler(int targetHandleID) : base("Disassembler", Handle.GetContextByID(targetHandleID).DeepCopy(), HandleParameters.DISASSEMBLEMODE | HandleParameters.NOJMP)
        {
            TargetContext = Handle.GetContextByID(targetHandleID);
        }
        public async Task<Dictionary<ulong, DisassembledItem>> Step(ulong count=0)
        {
            ulong IP = Handle.ShallowCopy().InstructionPointer;
            return await Step(IP, IP + count);
        }
        public async Task<Dictionary<ulong, DisassembledItem>> Step(ulong startAddress, ulong endAddress)
        {
            Handle.ShallowCopy().InstructionPointer = startAddress;
            Dictionary<ulong, DisassembledItem> Output = new Dictionary<ulong, DisassembledItem>();
            for (ulong CurrentAddr = startAddress; CurrentAddr < endAddress; Handle.Invoke(() => CurrentAddr = Handle.ShallowCopy().InstructionPointer))
            {
                DisassembledItem CurrentLine = new DisassembledItem();          
                if (CurrentAddr == TargetContext.InstructionPointer)
                {
                    CurrentLine.AddressInfo |= DisassembledItem.AddressState.RIP;
                }
                string Disassembly = JoinDisassembled((await RunAsync(true)).LastDisassembled);
                CurrentLine.DisassembledLine = $"{Disassembly}";
                Output.Add(CurrentAddr, CurrentLine);
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
        public async Task<Dictionary<ulong, DisassembledItem>> StepAll()
        {
            Context DisasContext = Handle.ShallowCopy();
            return await Step(DisasContext.Memory.EntryPoint, DisasContext.Memory.SegmentMap[".main"].End);
        }
    }

}
