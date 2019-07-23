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
        public Disassembler(Handle targetHandle) : base("Disassembler", ContextHandler.CloneContext(targetHandle))
        {

        }
        public async Task<List<DisassembledItem>> Step(ulong count)
        {            
            List<DisassembledItem> Output = new List<DisassembledItem>();
            for (ulong i = 0; i < count; i++)
            {
                Output.Add(new DisassembledItem()
                {
                    Address = InstructionPointer,
                    DisassembledLine = $"{Util.Core.FormatNumber(InstructionPointer, FormatType.Hex)}    {(await RunAsync(true)).LastDisassembled}"
                });
            }
            return Output;
        }
        public async Task<List<DisassembledItem>> StepAll()
        {

            Context DisasContext = ContextHandler.FetchContext(Handle);
            DisasContext.InstructionPointer = DisasContext.Memory.EntryPoint;
            return await Step(DisasContext.Memory.SegmentMap[".main"].LastAddr - DisasContext.Memory.EntryPoint);
        }
    }

}
