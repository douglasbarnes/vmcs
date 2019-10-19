// Disassembler is an example of a module for the VM class. It does nothing that any other HypervisorBase could not do. Despite being
// a module, it provides some of the core functionality that the standalone program provides. The class itself depends only on lower
// layer modules. Any modifications to them should consider the current preconditions the disassembler requires. Notably something to 
// consider would be the DisassembledLine struct. 
// The Disassembler works by listening on events provided by HypervisorBase for the given VM class. When the VM calls FlashMemory(), the
// disassembler will copy its context, execute it, and store the disassembled results in the ParsedLines class. A listener of the ParsedLines.OnAdd
// could then parse these lines. 
using System.Collections.Generic;
using debugger.Emulator;
using debugger.Util;
using static debugger.Emulator.ControlUnit;
namespace debugger.Hypervisor
{
    public class Disassembler : HypervisorBase
    {
        public struct ParsedLine
        {
            // ParsedLine is the intermediary struct for parsed DisassembledLines from ControlUnit. Mostly it is responsible for
            // the concatenation of the list of disassembled mnemonics. The two additional constructor parameters are very use
            // case specific and are explained in other parts of the class.
            public string DisassembledLine;
            public AddressInfo Info;
            public ulong Address;
            public int Index;
            public ParsedLine(DisassembledLine input, AddressInfo toOR=0)
            {
                DisassembledLine = JoinDisassembled(input.Line);
                Info = input.Info | toOR;
                Address = input.Address;
                Index = -1;
            }

            public ParsedLine(ParsedLine toChangeTo, AddressInfo toXor=0)
            {
                this = toChangeTo;
                Info ^= toXor;
            }
        }

        public readonly ListeningDict<AddressRange, ListeningList<ParsedLine>> ParsedLines = new ListeningDict<AddressRange, ListeningList<ParsedLine>>();
        private readonly AddressMap Map;
        private HypervisorBase Target;
        public Disassembler(VM target) 
            : base("Disassembler", Handle.GetContextByID(target.HandleID).DeepCopy(), HandleParameters.DISASSEMBLE | HandleParameters.NOJMP | HandleParameters.NOBREAK)
        {        
            // Target will hold a reference to the VM
            Target = target;

            // Create a new address range to disassemble(the loaded instructions)
            Map = new AddressMap();
            Map.AddRange(new AddressRange(Target.GetMemory().SegmentMap[".main"].Range.Start, Target.GetMemory().SegmentMap[".main"].Range.End));

            // When the target VM calls flash(), the context reference will change, so it is necessary to listen for this and update accordingly.
            target.Flash += UpdateTarget;
            
            // When the target finishes execution, update the address marked as RIP and the new RIP.
            target.RunComplete += (status) =>
            {                
                // These conditions will only pass if the address is present. It would be perfectly normal for it not to,
                // for example when all the instructions have been executed, $RIP is out of the disassembly.
                if ((Map.Search(status.InitialRIP).Info | AddressMap.BinarySearchResult.ResultInfo.PRESENT) == Map.Search(status.InitialRIP).Info)
                {
                    ToggleSetting(status.InitialRIP, AddressInfo.RIP);
                }
                if ((Map.Search(status.EndRIP).Info | AddressMap.BinarySearchResult.ResultInfo.PRESENT) == Map.Search(status.InitialRIP).Info)
                {
                    ToggleSetting(status.EndRIP, AddressInfo.RIP);
                }
            };

            // Listen for changes to breakpoints on the target VM.
            target.Breakpoints.OnAdd += (addr, index) => ToggleSetting(addr, AddressInfo.BREAKPOINT); 
            target.Breakpoints.OnRemove += (addr, index) => ToggleSetting(addr, AddressInfo.BREAKPOINT);
        }
        public void ToggleSetting(ulong address, AddressInfo info)
        {
            // Set AddressInfo at a particular line. Nothing happens if the line is in a range but isn't an instruction(e.g mid way through one), 
            // but one will be created if it is out of any existing range.

            // Find the address range the address lies in
            AddressMap.BinarySearchResult index = Map.Search(address);

            // If it not present, disassemble it and toggle it. I would imagine the only time for this to happen would be if instructions were
            // unpacked onto the stack then executed there. A rare case but one worth accounting for.
            if ((index.Info | AddressMap.BinarySearchResult.ResultInfo.PRESENT) != index.Info)
            {
                DisassembleStep(address);
                ToggleSetting(address, info);
            }
            else
            {
                // Iterate through each line in the address range until the desired is found, then OR its $Info with $info.
                ListeningList<ParsedLine> Lines = ParsedLines[Map[index.Index]];
                for (int i = 0; i < Lines.Count; i++)
                {
                    if (Lines[i].Address == address)
                    {
                        Lines[i] = new ParsedLine(Lines[i], info);
                    }
                }
            }
        }

        public void ClearAddressRange()
        {
            // When clearing the range, make sure that the initial code segment is added back to the address ranges. This would be called
            // for example when new instructions are loaded onto the VM. The disassembler will have to adjust the ranges it disassembled to
            // match new code.
            Map.Clear();
            Map.AddRange(new AddressRange(Target.GetMemory().SegmentMap[".main"].Range.Start, Target.GetMemory().SegmentMap[".main"].Range.End));
        }
        public void UpdateTarget(Context targetMemory)
        {
            // UpdateTarget is called when the VM instance calls FlashMemory(). This means that there are new instructions that need to be 
            // disassembled, so the disassembler will, in this particular order,
            //  -Clear what it already has.
            //  -Flash the new instructions of the VM
            //  -Clear its address range entries(must be after, see ClearAddressRange()
            //  -Disassemble the new range.
            // From this explanation it is clear why this order is needed, as the first 3 methods set up the preconditions for disassembly
            // to take place.
            // It can also be called by an external class, as there may be a time where this is necessary.
            ParsedLines.Clear();
            FlashMemory(targetMemory.Memory.DeepCopy());
            ClearAddressRange();
            DisassembleAll();
        }
        public void DisassembleAll()
        {
            // Clear any existing lines.
            ParsedLines.Clear();

            for (int i = 0; i < Map.Count; i++)
            {
                DisassembleRange(Map[i]);
            }
        }
        public void DisassembleStep(ulong address)
        {
            // Set the instruction pointer to $address 
            Handle.ShallowCopy().InstructionPointer = address;

            // Run with step true because only this address is being disassembled.
            Status RunResult = Run(true);

            // Dissassemble and add the lines to a new range which is the starting address and $rip afterwards(the entire instruction)
            AddLines(new AddressRange(address, RunResult.EndRIP), ParseLines(RunResult.Disassembly));
        }
        public void DisassembleRange(AddressRange range)
        {
            // Set breakpoints at the start and end of the range(address at $range.End will not be executed)
            Handle.ShallowCopy().Breakpoints.Add(range.End);
            Handle.ShallowCopy().InstructionPointer = range.Start;

            // Run and parse the instructions then add them to the collection.
            AddLines(range, ParseLines(Run().Disassembly));            
        }
        private void AddLines(AddressRange range, List<ParsedLine> lines) => SetLines(range, new ListeningList<ParsedLine>(lines));
        private void SetLines(AddressRange range, ListeningList<ParsedLine> lines)
        {
            // SetLines() generalises adding lines to ParsedLines. It is much simpler and consistent than having a specific method tailored to each use case.
            // The result of TryGetValue() is discarded because it is not useful, the value is about to be overwritten. It is also slightly faster than ContainsKey().
            if (ParsedLines.TryGetValue(range, out _))
            {
                ParsedLines[range] = lines;
            }
            else
            {
                ParsedLines.Add(range, lines);
            }
        }
        private List<ParsedLine> ParseLines(List<DisassembledLine> rawLines)
        {
            // Firstly get any useful information from the target context. $TargetContext is only used to avoid calling GetContextByID() twice.
            Context TargetContext = Handle.GetContextByID(Target.HandleID);

            // The breakpoints list will be useful for setting the AddressInfo value when necessary.
            ListeningList<ulong> TargetBreakpoints = TargetContext.Breakpoints;

            // Similar to above.
            ulong targetRIP = TargetContext.InstructionPointer;
            
            List<ParsedLine> OutputLines = new List<ParsedLine>();

            // Every raw line is iterated as every line is to be parsed.
            for (int i = 0; i < rawLines.Count; i++)
            {
                // Start with the assumption that it is an ordinary line. This information will be useful to other classes that
                // use the output of the Disassembler.
                AddressInfo Info = 0;

                // If the address is the same as $RIP, mark it as RIP.
                if (rawLines[i].Address == targetRIP)
                {
                    Info |= AddressInfo.RIP;
                }

                // If it is a breakpoint, mark it like so.
                if (TargetBreakpoints.Contains(rawLines[i].Address))
                {
                    Info |= AddressInfo.BREAKPOINT;
                }

                // Add the result to the output. The constructor of ParsedLine will do the rest of the work.
                OutputLines.Add(new ParsedLine(rawLines[i], Info));                
            }

            return OutputLines;
        }
        private static string JoinDisassembled(List<string> RawDisassembled)
        {
            // Simply enforce a little bit of convention. Take the following examples,
            //  INC EAX
            //  MOV EAX, 0x10
            //  IMUL EAX, EBX, 0x20            
            // Each item in the input list will be a part of the resulting disassembly. E.g,
            //  { "INC", "EAX" }
            //  { "MOV", "EAX", "0x10" }
            //  { "IMUL", "EAX, "EBX", "0x20" }
            // As shown earlier, if there are less than 3 of these parts, there is no comma.
            // Otherwise, every comma after zero index 1 has one afterwards.
            if (RawDisassembled.Count < 3)
            {
                return string.Join(" ", RawDisassembled);
            }
            else
            {
                // Start the string with the two that will not have a comma.
                string Output = $"{RawDisassembled[0]} {RawDisassembled[1]}";

                // Append the rest with a preceeding ", ".
                for (int i = 2; i < RawDisassembled.Count; i++)
                {
                    Output += ", " + RawDisassembled[i];
                }
                return Output;
            }
        }        
    }
}
