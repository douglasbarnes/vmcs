// The MemorySpace class provides the core way to input data into the program. Currently, the only one way to load the MemorySpace with data,
// which it to initialise a code segment. In the future, users will be able to initialise other segments, such as the stack segment
// with their own data. MemorySpace stores data extremely efficiently, and operates similar to the processor itself. Firstly, memory is only 
// allocated as it is used. This creates the illusion that a 4GB address space is available, however the data used will scale from the
// initial size of a dictionary proportionally with the amount of addresses with data stored.  Compared to the processor itself, which requires
// entire pages to be allocated at one time, usually with a minimum of 4KB size(4096 addresses, can differ, very hardware specific), yet at the
// same time, the processor does not have to store the address of a byte along with it. Being as efficient as a low level technology is what
// this program encourages, not implements. Given what is at my disposal, it makes very good use of its resources. See the index accessor
// for specific implementation.
using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator
{
    public class MemorySpace
    {
        private Dictionary<ulong, byte> AddressMap = new Dictionary<ulong, byte>();
        public Dictionary<string, Segment> SegmentMap = new Dictionary<string, Segment>();
        public ulong EntryPoint;
        public ulong End;
        public class Segment
        {
            // Addresses in the MemorySpace also support a somewhat basic implementation of segmentation
            // A segment with predefined data can be loaded in to memory when the MemorySpace is created.
            // Segments can also be read from the MemorySpace, for example, the stack pointer is first initialised
            // to SegmentMap[".stack"].
            // For ease of use, segmentation is not strict, meaning that memory can be set outside of any segment.            
            public ulong StartAddr;
            public ulong End;
            public byte[] Data = null;
        }
        public static implicit operator Dictionary<ulong, byte>(MemorySpace m) => m.AddressMap;
        public MemorySpace(byte[] memory)
        {
            // For simplicity a MemorySpace starts at 0 because there is no kernel implementation.
            EntryPoint = 0;
            // Define $End to tell when there are no more addresses to be read, anything after $End would return 0x00.
            // It is used to set a breakpoint after all instructions to avoid this.
            End = (ulong)memory.LongLength;
            // ".main" is where the ControlUnit will read the instructions from initially. It contains the data passed in as $memory.
            SegmentMap.Add(".main", new Segment() { StartAddr = EntryPoint, End = EntryPoint + (ulong)memory.LongLength, Data = memory });
            // ".stack" holds the start address of the stack. There is no defined $End of said stack. A manually crafted stack could be added
            // by setting $Segment.Data 
            SegmentMap.Add(".stack", new Segment() { StartAddr = 0x800000, End = 0x0 });
            
            // Load all segments into the internal address table.
            foreach (Segment seg in SegmentMap.Values)
            {
                // If the segment has no defined data, just add a 0x0.
                if (seg.Data == null)
                {
                    AddressMap.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    // If it has data, add all that data until,
                    //  1. There is no more data to add
                    //  2. The defined end of the segment is reached
                    for (ulong i = 0; i < seg.Data.Length && i < seg.End; i++)
                    {
                        AddressMap.Add(i + seg.StartAddr, seg.Data[i]);                        
                    }
                }

            }
        }
        private MemorySpace(MemorySpace toClone)
        {
            // A deep cloning constructor, so that editing addresses in $this does not change the addresses in $toClone.
            // Classes are object orientated, so C# will try to use a reference where ever possible, but this can get in the way.
            AddressMap = toClone.AddressMap.DeepCopy();
            SegmentMap = toClone.SegmentMap.DeepCopy();
            // Value types do not need to be deep copied, by default they are not passed by reference.
            EntryPoint = toClone.EntryPoint;
            End = toClone.End;
        }
        public MemorySpace DeepCopy()
        {
            return new MemorySpace(this);
        }
        public byte this[ulong address]
        {
            // A memory space can use an index accessor which will return AddressMap[$address] if it exists, otherwise a 0. This is where a seg fault would occur if segmentation was strict.
            get
            {
                return AddressMap.ContainsKey(address) ? AddressMap[address] : (byte)0x00;
            }
            set
            {
                // The address map doesn't need to be filled with 0s at initialisation. That would be massive waste of space. So instead, addresses are added to the dictionary as they are given values,
                // if the address is already in $AddressMap, its value is changed.
                // By default a 0 byte is returned if the address is not used. This means that an address can be removed if a 0 byte is assigned to it, or never added to the address table at all.
                if (AddressMap.ContainsKey(address))
                {
                    if(value == 0x00)
                    {
                        AddresMap.Remove(address);
                    }
                    else 
                    {
                        AddressMap[address] = value;
                    }
                }
                else if(value != 0x00)
                {
                    AddressMap.Add(address, value);
                }
            }
        }
    }
}
