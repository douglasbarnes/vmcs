using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Primitives;
using static debugger.ControlUnit;
namespace debugger
{
    public class Disassembler : EmulatorBase
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
        public Disassembler(Handle targetHandle) : base("Disassembler", targetHandle.ShallowCopy())
        {
            TargetHandle = targetHandle;
        }
        public async Task<List<DisassembledItem>> Step(ulong count=0)
        {
            ulong IP = Handle.ShallowCopy().InstructionPointer;
            return await Step(IP, IP + count);
        }
        public async Task<List<DisassembledItem>> Step(ulong startAddress, ulong endAddress)
        {
            Handle.DeepCopy().InstructionPointer = startAddress;
            List<DisassembledItem> Output = new List<DisassembledItem>();
            for (ulong i = startAddress; i < endAddress; i++)
            {
                string ExtraInfo;
                ulong CurrentAddr = 0;
                Handle.Invoke(() => CurrentAddr = Handle.ShallowCopy().InstructionPointer);
                if (CurrentAddr == TargetContext.InstructionPointer)
                {
                    ExtraInfo = "←RIP";
                }
                else
                {
                    ExtraInfo = "    ";
                }
                Output.Add(new DisassembledItem()
                {
                    Address = CurrentAddr,                                         // } 1 space (←rip/4 spaces) 15 spaces {                    
                    DisassembledLine = $"{Util.Core.FormatNumber(CurrentAddr, FormatType.Hex)} {ExtraInfo}               {(await RunAsync(true)).LastDisassembled}"
                }); ;

            }
            return Output;
        }
        public async Task<List<DisassembledItem>> StepAll()
        {
            Context DisasContext = Handle.ShallowCopy();
            return await Step(DisasContext.Memory.EntryPoint, DisasContext.Memory.SegmentMap[".main"].End);
        }
    }

}
