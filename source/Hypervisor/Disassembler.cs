using System;
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
        public struct AddressRange
        {
            public readonly ulong Start;
            public readonly ulong End;
            public AddressRange(ulong start, ulong end)
            {                
                Start = start;
                End = end;
                if (Start > End)
                {
                    throw new Exception();
                }
            }

        }
        public readonly ListeningDict<AddressRange, ListeningList<ParsedLine>> ParsedLines = new ListeningDict<AddressRange, ListeningList<ParsedLine>>();
        private readonly List<AddressRange> AddressRanges;
        private HypervisorBase Target;
        public Disassembler(VM target) 
            : base("Disassembler", Handle.GetContextByID(target.HandleID).DeepCopy(), HandleParameters.DISASSEMBLEMODE | HandleParameters.NOJMP | HandleParameters.NOBREAK)
        {            
            Target = target;
            AddressRanges = new List<AddressRange>
            {
                new AddressRange(Target.GetMemory().SegmentMap[".main"].StartAddr, Target.GetMemory().SegmentMap[".main"].End)
            }; 
            target.Flash += UpdateTarget;
            
            target.RunComplete += (status) =>
            {                
                if (BinarySearchRange(status.InitialRIP).present)
                {
                    ToggleSetting(status.InitialRIP, AddressInfo.RIP);
                }
                if (BinarySearchRange(status.EndRIP).present)
                {
                    ToggleSetting(status.EndRIP, AddressInfo.RIP);
                }
            };
            target.Breakpoints.OnAdd += (addr, index) => ToggleSetting(addr, AddressInfo.BREAKPOINT); 
            target.Breakpoints.OnRemove += (addr, index) => ToggleSetting(addr, AddressInfo.BREAKPOINT);
        }
        private void ToggleSetting(ulong address, AddressInfo info)
        {
            // Set AddressInfo at a particular line. Nothing happens if the line is in a range but isn't an instruction(e.g mid way through one), 
            // but one will be created if it is out of any existing range.

            // Find the address range the address lies in
            BinarySearchResult index = BinarySearchRange(address);

            if (!index.present)
            {
                DisassembleStep(address);
                ToggleSetting(address, info);
            }
            else
            {
                // Iterate through each line in the address range until the desired is found, then OR its $Info with $info.
                ListeningList<ParsedLine> Lines = ParsedLines[AddressRanges[index.index]];
                for (int i = 0; i < Lines.Count; i++)
                {
                    if (Lines[i].Address == address)
                    {
                        Lines[i] = new ParsedLine(Lines[i], info);
                    }
                }
            }            
        }
 
        public void AddAddressRange(AddressRange range)
        {
            // Method to preserve the order of the address ranges, such that every value in AddressRange[x] is greater than AddressRange[x-1].
            // lower >= x < upper
            BinarySearchResult index = BinarySearchRange(range.Start);

            // Was higher than all existing ranges
            if(index.index == -2)
            {
                AddressRanges.Add(range);
            }

            // Lower than all existing ranges
            else if(index.index == -1)
            {
                AddressRanges.Insert(0, range);
            }

            // If the intersection of AddressRanges[index] and $range has a cardinality greater than 0, create a new range which is the union of AddressRanges[index] and $range.
            else if (index.present && AddressRanges[index.index].End < range.End)
            {
                // $range.Start may be in the middle of AddressRanges[index], so the union can only be calculated by the following.
                AddressRanges.Add(new AddressRange(AddressRanges[index.index].Start , range.End));             
            }

            else if(!index.present && AddressRanges[index.index].End > range.End)
            {
                AddressRanges.Add(new AddressRange(range.End, AddressRanges[index.index].Start));
            }

            else if (!index.present)
            {
                AddressRanges.Insert(index.index, range);
            }
        }
        public void ClearAddressRange()
        {
            AddressRanges.Clear();
            AddressRanges.Add(new AddressRange(Target.GetMemory().SegmentMap[".main"].StartAddr, Target.GetMemory().SegmentMap[".main"].End));
        }
        public void UpdateTarget(Context targetMemory)
        {
            ParsedLines.Clear();
            FlashMemory(targetMemory.Memory.DeepCopy());            
            ClearAddressRange();
            DisassembleAll();
        }
        public void DisassembleAll()
        {
            ParsedLines.Clear();
            for (int i = 0; i < AddressRanges.Count; i++)
            {
                DisassembleRange(AddressRanges[i]);
            }
        }
        public void DisassembleStep(ulong address)
        {
            Handle.ShallowCopy().InstructionPointer = address;
            AddAddressRange(new AddressRange(address, Run(true).EndRIP));
            DisassembleRange(AddressRanges[BinarySearchRange(address).index]);
        }
        public void DisassembleRange(AddressRange range)
        {
            Handle.ShallowCopy().Breakpoints.Add(range.End);
            Handle.ShallowCopy().InstructionPointer = range.Start;
            List<DisassembledLine> RawLines = Run().Disassembly;

            // Don't create a new list if one is already present.
            ListeningList<ulong> TargetBreakpoints = Handle.GetContextByID(Target.HandleID).Breakpoints;
            ListeningList<ParsedLine> CurrentLines = new ListeningList<ParsedLine>();
            ulong targetRIP = Handle.GetContextByID(Target.HandleID).InstructionPointer;
            for (int i = 0; i < RawLines.Count; i++)
            {
                AddressInfo Info = 0;
                if(RawLines[i].Address == targetRIP)
                {
                    Info |= AddressInfo.RIP;
                }
                if (TargetBreakpoints.Contains(RawLines[i].Address))
                {
                    Info |= AddressInfo.BREAKPOINT;
                }
                if(i == CurrentLines.Count)
                {
                    CurrentLines.Add(new ParsedLine(RawLines[i], Info));
                }
                else
                {
                    CurrentLines[i] = new ParsedLine(RawLines[i], Info);
                }
            }
            if (ParsedLines.TryGetValue(range, out _))
            {
                ParsedLines[range] = CurrentLines;                
            }
            else
            {
                ParsedLines.Add(range, CurrentLines);
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
        private struct BinarySearchResult
        {
            public int index;
            public bool present;
        }
        private BinarySearchResult BinarySearchRange(ulong address)
        {
            // Binary search adapted to work with ranges. As would be expected, this is in logarithmic time.
            // The domain of the method would be any ulong, as values out of boundary are handled.
            // The range of the result index is x = -2, x = -1 or 0 >= x < $AddressRanges.Count.
            // The former two have different meanings:
            //  -2: The input was above all other ranges.
            //  -1: The input was below all other ranges.
            // $Present would indicate that $address is in no existing range, however if not in the above
            // category, would give the index where $address lies between.
            // And to be clear, the output would otherwise be the index in AddressRanges which holds the address.

            // Exit early if possible.
            if (AddressRanges.Count == 0)
            {
                return new BinarySearchResult { present = false, index = 0 };
            }
            // Start in the middle
            int index = AddressRanges.Count / 2;

            // Keep $last_index equal to the previous index
            int prev_index = -1;

            // If prev_index was either boundary of the function range,the input must not be in AddressRanges.
            // This has to be prev_index because the boundary index needs to be checked first.
            while (prev_index != 0 && prev_index < AddressRanges.Count - 1)
            {
                prev_index = index;

                // If it is gte to the start and lt the end, it must be in that range(lower bound is inclusive)
                if (AddressRanges[index].Start <= address && AddressRanges[index].End > address)
                {
                    return new BinarySearchResult { present = true, index = index };
                }

                // If the end was less than the input address, check middle of the upper range.
                else if (AddressRanges[index].End < address)
                {
                    index += index / 2 + index % 2;
                }
                else if (index != 0 && AddressRanges[index].Start > address && AddressRanges[index - 1].End <= address)
                {
                    return new BinarySearchResult { present = false, index = index };
                }
                // Otherwise it must have been less than the start(because it wasn't in or above the range)
                // Equivalent to 
                //  else if (AddressRanges[index].Start > address)
                else
                {
                    index /= 2;
                }
            }

            // If it isn't 0, its $AddressRange.Count-1;
            return new BinarySearchResult() { present = false, index = prev_index == 0 ? -1 : -2 };
        }
    }

}
