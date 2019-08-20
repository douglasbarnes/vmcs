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
            public ulong StartAddr;
            public ulong End;
            public byte[] baData = null;
        }
        public static implicit operator Dictionary<ulong, byte>(MemorySpace m) => m.AddressMap;
        //public MemorySpace(Dictionary<ulong, byte> memory, Dictionary<string, Segment> segmap)
        //{
        //    AddressMap = memory;
        //    SegmentMap = segmap;
        //    EntryPoint = segmap[".main"].StartAddr;
        //    End = segmap[".main"].End;
        //}
        public MemorySpace(byte[] memory)
        {
            EntryPoint = 0;
            End = (ulong)memory.LongLength;
            SegmentMap.Add(".main", new Segment() { StartAddr = EntryPoint, End = EntryPoint + (ulong)memory.LongLength, baData = memory });
            SegmentMap.Add(".heap", new Segment() { StartAddr = 0x400001, End = 0x0 });
            SegmentMap.Add(".stack", new Segment() { StartAddr = 0x800000, End = 0x0 });

            foreach (Segment seg in SegmentMap.Values)
            {
                if (seg.baData == null)
                {
                    AddressMap.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    for (ulong i = 0; i < (seg.End - seg.StartAddr); i++)
                    {
                        AddressMap.Add(i + seg.StartAddr, seg.baData[i]);
                    }
                }

            }
        }
        private MemorySpace(MemorySpace toClone)
        {
            AddressMap = toClone.AddressMap.DeepCopy();
            SegmentMap = toClone.SegmentMap.DeepCopy();
            EntryPoint = toClone.EntryPoint;
            End = toClone.End;
        }
        public MemorySpace DeepCopy()
        {
            return new MemorySpace(this);
        }
        public byte this[ulong address]
        {
            get
            {
                return AddressMap.ContainsKey(address) ? AddressMap[address] : (byte)0x00;
            }
            set
            {
                if (AddressMap.ContainsKey(address))
                {
                    AddressMap[address] = value;
                }
                else
                {
                    AddressMap.Add(address, value);
                }
            }
        }
    }
}
