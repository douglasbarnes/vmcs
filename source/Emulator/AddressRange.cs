// AddressRange along with AddressMap provides a very efficent way of storing metadata about a collection. The merits are best shown in collections that
// have large gaps in addresses. For example, by keeping the address map coherent with a collection, instead of having to search the whole collection
// for a value, an address range can narrow that down immensively if used correctly. A collection can be coherent by keeping its ranges synchronised
// with AddressRange using the AddRange() method. A simple example is with a dictionary. Assume a dictionary with AddressRange as the key type and ints
// as the value. If I set the range 0x100<x<0x200 to a series of 0xAAs, instead of having to store 0xFF bytes of 0xAAs, it could just be the range 0x100<x<0x200
// and the value 0xAA. When using the binary search to access 0x120, the Index field of the output would say which index of the internal list to access to get 
// its range(outside of the class it would be $AddressMap[$AddressRange]). Then the dictionary could be accessed with this range to get the corresponding byte.
// As this struct was imported from another program of my own, it has a been adapted to fit a slightly different use case which is explained in Disassembler.cs
using System;
using System.Collections.Generic;

namespace debugger.Emulator
{
    public struct BinarySearchResult
    {
        // The index of the result in the internal list
        public readonly int Index;

        // Whether the index represents an existing range in the list or the index where the range should be inserted.
        public readonly bool Present;

        // Whether the input address was adjacent to another range or not. See TryMerge()
        public readonly bool Adjacent;
        public BinarySearchResult(int index, bool present, bool adjacent = false)
        {
            Index = index;
            Present = present;
            Adjacent = adjacent;
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

            // Address ranges must have a size greater than zero.
            if (Start >= End)
            {
                throw new Exception();
            }
        }

    }
    public class AddressMap
    {
        private readonly List<AddressRange> AddressRanges = new List<AddressRange>();
        public AddressRange this[int index]
        {
            get => AddressRanges[index];
        }
        public int Count => AddressRanges.Count;
        public void AddRange(AddressRange range)
        {
            // If there are no existing ranges it is obvious where $range will go.
            if(AddressRanges.Count == 0)
            {
                AddressRanges.Add(range);
                return;
            }

            // Method to preserve the order of the address ranges, such that every value in AddressRange[x] is greater than AddressRange[x-1].
            // lower >= x < upper
            BinarySearchResult Result = Search(range.Start);

            // Was higher than all existing ranges
            if (Result.Index == -2)
            {
                AddressRanges.Add(range);
            }

            // Lower than all existing ranges
            else if (Result.Index == -1)
            {
                AddressRanges.Insert(0, range);
            }

            // If the intersection of AddressRanges[index] and $range has a cardinality greater than 0, create a new range which is the union of AddressRanges[index] and $range.
            else if (Result.Present && AddressRanges[Result.Index].End < range.End)
            {
                // $range.Start may be in the middle of AddressRanges[index], so the union can only be calculated by the following.
                AddressRanges.Add(new AddressRange(AddressRanges[Result.Index].Start, range.End));
            }

            // If the index is not present and above range overlaps, ensure that the new range does not overlap with the one currently at $index
            else if (!Result.Present && AddressRanges[Result.Index].End > range.End)
            {
                AddressRanges.Add(new AddressRange(range.End, AddressRanges[Result.Index].Start));
            }

            else if (!Result.Present)
            {
                AddressRanges.Insert(Result.Index, range);
            }
        }
        public bool TryMerge(ulong address)
        {
            // Extend an existing(and existing) range to cover $address, if it is adjacent. Extending a higher range is prioritised. Combining both adjacent ranges
            // could introduce other side effects and lessen the performance of the address range concept as larger ranges have to be searched. If there was an adjacent
            /// range to be merged, return true. 

            BinarySearchResult Result = Search(address);

            // If the address is adjacent to an existing range, expand the existing range to cover $address.
            // Effectively the range is either incremented downwards or upwards.
            // This(if possible) reduces the clutter in the address range list. However there are cases where AddRange() would still be used.
            if (Result.Adjacent)
            {
                // If it was adjacent to the higher range
                if(AddressRanges[Result.Index].Start - 1 == address)
                {
                    AddressRanges[Result.Index] = new AddressRange(address, AddressRanges[Result.Index].End);
                }
                
                // Otherwise was adjacent to the lower
                else
                {
                    AddressRanges[Result.Index-1] = new AddressRange(AddressRanges[Result.Index-1].Start, address);
                }
                return true;
            }
            return false;
        }
        public BinarySearchResult Search(ulong address)
        {
            // Binary search adapted to work with ranges. As would be expected, this is in logarithmic time.
            // The domain of the method would be any ulong, as values out of boundary are handled.
            // The range of the result index is x = -2, x = -1 or 0 >= x < $AddressRanges.Count.
            // The former two have different meanings:
            //  -2: The input was above all other ranges.
            //  -1: The input was below all other ranges.
            // $Present would indicate that $address is in no existing range, however if not in the above
            // category, would give the index where $address lies between, [lowerRange].End < $address < [upperRange].Start
            // If $Present is false, the index represents index that $address lies between.

            // Exit early if possible.
            if (AddressRanges.Count == 0)
            {
                return new BinarySearchResult(0,false);
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
                    return new BinarySearchResult(index, true);
                }

                // If the end was less than the input address, check middle of the upper range.
                else if (AddressRanges[index].End < address)
                {
                    index += index / 2 + index % 2;
                }

                // If the index isn't 0, the start address of the current index is gt $address and the end address of the previous is lte $address, it must lie
                // between the two, [$index-1].End < $address < [$index].Start
                else if (index != 0 && AddressRanges[index].Start > address && AddressRanges[index - 1].End <= address)
                {
                    return new BinarySearchResult(index, false, (address + 1 == AddressRanges[index].Start || address == AddressRanges[index - 1].End));
                }

                // Otherwise it must have been less than the start(because it wasn't in or above the range)
                // Equivalent to 
                //  else if (AddressRanges[index].Start > address)
                else
                {
                    index /= 2;
                }

                // If $index goes out of the range, the address is above the highest.
                if(index >= AddressRanges.Count - 1)
                {
                    break;
                }
            }

            // If it isn't 0, its $AddressRange.Count-1;
            return new BinarySearchResult(prev_index == 0 ? -1 : -2, false);
        }
        public void Clear() => AddressRanges.Clear();
    }
}
