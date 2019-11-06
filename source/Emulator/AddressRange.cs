// AddressRange along with AddressMap provides a very efficent way of storing metadata about a collection. The merits are best shown in collections that
// have large gaps in addresses. For example, by keeping the address map coherent with a collection, instead of having to search the whole collection
// for a value, an address range can narrow that down immensely if used correctly. A collection can be coherent by keeping its ranges synchronised
// with AddressRange using the AddRange() method. A simple example is with a dictionary. Assume a dictionary with AddressRange as the key type and ints
// as the value. If I set the range 0x100<=x<0x200 to a series of 0xAAs, instead of having to store 0xFF bytes of 0xAAs, it could just be the range 0x100<x<0x200
// and the value 0xAA. When using the binary search to access 0x120, the Index field of the output would say which index of the internal list to access to get 
// its range(outside of the class it would be $AddressMap[$AddressRange]). Then the dictionary could be accessed with this range to get the corresponding byte.
// As this struct was imported from another program of my own, it has a been adapted to fit a slightly different use case which is explained in Disassembler
// and in MemorySpace
using System;
using System.Collections.Generic;

namespace debugger.Emulator
{
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
        public struct BinarySearchResult
        {
            // See Search()
            public enum ResultInfo
            {
                NONE = 0,
                ADJ_ABOVE = 1,
                ADJ_BENEATH = 2,
                OUT_OF_BOUNDS = 4,
                PRESENT = 8,
            }

            // The index of the result in the internal list
            public readonly int Index;

            // Extra information about the position of the address.
            public readonly ResultInfo Info;
            public BinarySearchResult(int index, ResultInfo info = ResultInfo.NONE)
            {
                Index = index;
                Info = info;
            }
        }
        private readonly List<AddressRange> AddressRanges = new List<AddressRange>();
        public AddressRange this[int index]
        {
            get => AddressRanges[index];
        }
        public int Count => AddressRanges.Count;
        public void AddRange(AddressRange range)
        {
            // If there are no existing ranges it is obvious where $range will go.
            if (AddressRanges.Count == 0)
            {
                AddressRanges.Add(range);
                return;
            }

            // Method to preserve the order of the address ranges, such that every value in AddressRange[x] is greater than AddressRange[x-1].
            // lower >= x < upper
            BinarySearchResult Result = Search(range.Start);

            // If the intersection of AddressRanges[index] and $range has a cardinality greater than 0, create a new range which is the union of AddressRanges[index] and $range.
            // This is because the two overlap, and so should be stored as one range rather than two.
            if ((Result.Info | BinarySearchResult.ResultInfo.PRESENT) == Result.Info && AddressRanges[Result.Index].End < range.End)
            {
                // $range.Start may be in the middle of AddressRanges[index], so the union can only be calculated by the following.
                AddressRanges[Result.Index] = new AddressRange(AddressRanges[Result.Index].Start, range.End);
            }

            // If the index is not present and above range overlaps, ensure that the new range does not overlap with the one currently at $index. This will not work if the
            // range is OUT_OF_BOUNDS as it will not have a start address in the existing range.
            else if ((Result.Info | BinarySearchResult.ResultInfo.PRESENT) != Result.Info
                    && (Result.Info | BinarySearchResult.ResultInfo.OUT_OF_BOUNDS) != Result.Info
                    && range.Start < AddressRanges[Result.Index].Start)
            {
                AddressRanges.Insert(Result.Index, new AddressRange(range.Start, AddressRanges[Result.Index].Start));
            }

            // If it not present and did not meet the above, it must just be a standalone address that can be inserted as normal.            
            else if ((Result.Info | BinarySearchResult.ResultInfo.PRESENT) != Result.Info)
            {
                AddressRanges.Insert(Result.Index, range);
            }

            // If none of the above predicates were true, $range must be a subset an existing range.
        }
        public void TryMerge(ulong address)
        {
            // Extend an existing(and existing) range to cover $address, if it is adjacent. Extending a higher range is prioritised. Combining both adjacent ranges
            // could introduce other side effects and lessen the performance of the address range concept as larger ranges have to be searched. If there was an adjacent
            // range to be merged, return true. 

            BinarySearchResult Result = Search(address);

            // If the address is adjacent to an existing range, expand the existing range to cover $address.
            // Effectively the range is either incremented downwards or upwards.
            // This(if possible) reduces the clutter in the address range list. However there are cases where AddRange() would still be used, so the two concepts
            // of these functions are kept separate.

            // If adjacent to the above and beneath, create a new range of $beneath union $above union $address
            if (Result.Info == (BinarySearchResult.ResultInfo.ADJ_ABOVE | BinarySearchResult.ResultInfo.ADJ_BENEATH))
            {
                ulong Above_End = AddressRanges[Result.Index].End;
                AddressRanges.RemoveAt(Result.Index);
                AddressRanges[Result.Index - 1] = new AddressRange(AddressRanges[Result.Index - 1].Start, Above_End);
            }

            // If it was adjacent to the higher range
            else if ((Result.Info | BinarySearchResult.ResultInfo.ADJ_ABOVE) == Result.Info)
            {
                AddressRanges[Result.Index] = new AddressRange(address, AddressRanges[Result.Index].End);
            }

            // Check if adjacent to the beneath.
            else if ((Result.Info | BinarySearchResult.ResultInfo.ADJ_BENEATH) == Result.Info)
            {
                AddressRanges[Result.Index - 1] = new AddressRange(AddressRanges[Result.Index - 1].Start, address + 1);
            }

            // Otherwise it is an outlier.
            else
            {
                AddRange(new AddressRange(address, address + 1));
            }
        }
        public BinarySearchResult Search(ulong address)
        {
            // Binary search adapted to work with ranges. As would be expected, this is in logarithmic time.
            // The returned is a BinarySearchResult contains important information about the search. The Index
            // field of this is the output you would expect from a binary search, the index in the array of which
            // the target is found. In this case, the index provides much more insight as opposed to the value which
            // is at the index. This is because the range may then be inserted there. See AddRange() for more specifics
            // on this. The ResultInfo is what distinguishes this algorithm from an ordinary binary search. It includes
            // useful information that is very specific to the case of searching ranges.
            //  NONE - An address that is not present and has no distinguishable attributes.
            //  PRESENT - $address lies in an existing range thus need not be added as a new range
            //  OUT_OF_BOUNDS - $address lies at the extreme of the existing ranges, either at the top index or index 0.
            //  ADJ_ABOVE - The end address is the start address - 1 of the next range up
            //  ADJ_BENEATH - The start address is the end address of the range beneath(the end address is not inclusive)
            // For implementation of ADJ_*, see DetermineAdjacency().
            // It is important to remember that multiple of these attributes can be present at once; they are simply ORed
            // onto the Info value. For future maintenance, this will mean that ORs logic should be used to check for the
            // presence of an attribute.
            // E.g
            // This,
            //  (Result.Info | BinarySearchResult.ResultInfo.PRESENT) == Result.Info
            // Not,
            //  Result.Info != BinarySearchResult.ResultInfo.PRESENT
            // Because Info could be PRESENT and so could ADJ_BENEATH, in this case only the first would recognise the result as present.

            // Exit early if possible.
            if (AddressRanges.Count == 0)
            {
                return new BinarySearchResult(0);
            }

            // Start at the middle index(-1 because indexes start at 0)
            int index = (AddressRanges.Count - 1) / 2;

            // It is certain that this will not loop ad infinitum. This is because the condition element of this while was just moved inside the loop
            // Normally I would oppose this kind of design, but it actually makes it really clear what its happening as there are multiple conditions
            // to consider (see ahead).
            while (true)
            {
                // Store the current index to be tested later
                int prev_index = index;

                // Check whether the address lies above the end of the current range. If so, ascend like a binary search.
                if (AddressRanges[index].End <= address)
                {
                    index += index / 2;
                }

                // If the start address of the current range is greater than the address, the address must be someplace beneath.
                else if (AddressRanges[index].Start > address)
                {
                    index /= 2;
                }

                // Now are the conditions that were moved inside the loop. There is no binary searching past here.

                // This is the case where the address lies above any existing range. Unlike an ordinary binary search, this is an
                // absolutely valid and common case here as it is no so much a binary search of an ordered set, rather a binary search of
                // indexes. This could go way over AddressRanges.Count(exactly $prev_index/2 above count), but obviously this is the only possible outcome in this case.
                if (index >= AddressRanges.Count)
                {
                    return new BinarySearchResult(AddressRanges.Count, BinarySearchResult.ResultInfo.OUT_OF_BOUNDS);
                }

                // As with any binary search, once the "right meets the left", the search has converged onto one value. This now has to
                // be evaluated a little further.
                if (index == prev_index)
                {
                    // If this condition is met, the address was between two bounds of a range, therefore in it.
                    if (AddressRanges[index].Start <= address && AddressRanges[index].End > address)
                    {
                        return new BinarySearchResult(index, BinarySearchResult.ResultInfo.PRESENT);
                    }

                    // Despite handling this scenario earlier, there is still one possiblity where the address is above any range. This is when
                    // the search converged on the uppermost index without going out of the any bounds. This condition will be tested again shortly.
                    if (AddressRanges[index].End <= address)
                    {
                        // The index needs to be incremented because it is in the range above the count. This could not be produced by the algorithm
                        // because the index converged on the uppermost index.
                        index++;
                    }

                    // Determine extra information about the result that is useful elsewhere.
                    BinarySearchResult.ResultInfo info = DetermineAdjacency(address, index);

                    // Test whether out of any existing ranges.
                    if (address < AddressRanges[0].Start || index >= AddressRanges.Count)
                    {
                        info |= BinarySearchResult.ResultInfo.OUT_OF_BOUNDS;
                    }

                    return new BinarySearchResult(index, info);
                }
            }
        }
        private BinarySearchResult.ResultInfo DetermineAdjacency(ulong address, int index)
        {
            // By default assume the address is adjacent to no existing range.
            BinarySearchResult.ResultInfo Output = 0;

            // If the address +1 is the start of the next, it is adjacent to the above.
            if (index < AddressRanges.Count && address + 1 == AddressRanges[index].Start)
            {
                Output |= BinarySearchResult.ResultInfo.ADJ_ABOVE;
            }

            // If the address is the end of the range beneath, it is adjacent to it(as .End is exclusive).
            if (index > 0 && address == AddressRanges[index - 1].End)
            {
                Output |= BinarySearchResult.ResultInfo.ADJ_BENEATH;
            }

            return Output;
        }
        public void Clear() => AddressRanges.Clear();

        public AddressMap DeepCopy() => new AddressMap(this);

        private AddressMap(AddressMap toClone)
        {
            AddressRanges = Util.Core.DeepCopy(toClone.AddressRanges);
        }
        public AddressMap()
        {

        }
    }
}
