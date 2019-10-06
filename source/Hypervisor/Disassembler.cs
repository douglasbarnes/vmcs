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
        public struct ParsedLine
        {
            public string DisassembledLine;
            public AddressInfo Info;
            public ulong Address;
            public ParsedLine(DisassembledLine input)
            {
                DisassembledLine = JoinDisassembled(input.Line);
                Info = input.Info;
                Address = input.Address;
            }
        }
        public readonly ListeningList<ParsedLine> ParsedLines = new ListeningList<ParsedLine>();
        public List<(ulong, ulong)> AddressRanges;
        public Disassembler(int targetHandleID) 
            : base("Disassembler", Handle.GetContextByID(targetHandleID).DeepCopy(), HandleParameters.DISASSEMBLEMODE | HandleParameters.NOJMP | HandleParameters.NOBREAK)
        {
            Context targetContext = Handle.GetContextByID(targetHandleID);
            AddressRanges = new List<(ulong, ulong)>
            {
                (targetContext.Memory.SegmentMap[".main"].StartAddr,
                targetContext.Memory.SegmentMap[".main"].End)
            };
        }
        public void UpdateTarget(int targetHandleID)
        {
            ParsedLines.Clear();
            Flash(Handle.GetContextByID(targetHandleID).Memory);
        }
        public void DisassembleAll()
        {
            ParsedLines.Clear();
            for (int i = 0; i < AddressRanges.Count; i++)
            {
                DisassembleRange(AddressRanges[i]);
            }
        }
        public void DisassembleRange((ulong start, ulong end) input)
        {
            Handle.ShallowCopy().Breakpoints.Add(input.end);
            Handle.ShallowCopy().InstructionPointer = input.start;
            List<DisassembledLine> RawLines = Run().Disassembly;
            for (int i = 0; i < RawLines.Count; i++)
            {
                ParsedLines.Add(new ParsedLine(RawLines[i]));
            }
        }
        
        private static string JoinDisassembled(List<string> RawDisassembled)
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
    }

}
