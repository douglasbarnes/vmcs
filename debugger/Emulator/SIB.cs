using System;
using System.Linq;
using debugger.Util;

using static debugger.Util.Disassembly;
namespace debugger.Emulator
{
    public struct SIB
    {
        public RegisterCapacity PointerSize;
        public ulong ResultNoOffset;
        public ulong IndexValue;
        public ulong BaseValue;
        public long OffsetValue; // a way to avoid this? when we have a sib where its [reg + ebp + offset], we cant disassemble that visibly without this because ebp could change giving the wrong offset displayed
        public readonly DeconstructedSIB Bits;

        private readonly byte Mod;
        public struct DeconstructedSIB
        {
            public readonly byte Scale;
            public readonly byte Index;
            public readonly byte Base;
            public DeconstructedSIB(byte scale, byte index, byte _base)
            {
                (Scale, Index, Base) = (scale, index, _base);
            }
            public DeconstructedSIB(byte sibByte)
            {
                string SIBbits = Bitwise.GetBits(sibByte);
                this = new DeconstructedSIB(Convert.ToByte(SIBbits.Substring(0, 2),2), Convert.ToByte(SIBbits.Substring(2, 3), 2), Convert.ToByte(SIBbits.Substring(5, 3), 2));
            }
        }
        public SIB(byte sibByte, byte mod)
        {
            string SIBbits = Bitwise.GetBits(sibByte);
            this = new SIB(Convert.ToByte(SIBbits.Substring(0, 2), 2), Convert.ToByte(SIBbits.Substring(2, 3), 2), Convert.ToByte(SIBbits.Substring(5, 3), 2), mod);
        }
        public SIB(byte scale, byte index, byte _base, byte mod)
        {
            (BaseValue, OffsetValue, IndexValue) = (0, 0, 0);
            Bits = new DeconstructedSIB(scale, index ,_base);
            Mod = mod;
            PointerSize = (ControlUnit.PrefixBuffer.Contains(PrefixByte.ADDR32) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD);
            if (index != 4)//4 == none
            {
                IndexValue = (byte)Math.Pow(2,scale) * BitConverter.ToUInt64(Bitwise.SignExtend(ControlUnit.FetchRegister((ByteCode)index, PointerSize), 8), 0);
            }

            if (_base != 5) // 5 = ptr or rbp+ptr
            {
                BaseValue = BitConverter.ToUInt64(ControlUnit.FetchRegister((ByteCode)_base, RegisterCapacity.QWORD), 0);
            }
            else
            {
                if (mod > 0) // mod1 mod2 = ebp+imm
                {
                    BaseValue = BitConverter.ToUInt64(Bitwise.SignExtend(ControlUnit.FetchRegister(ByteCode.BP, PointerSize), 8), 0);
                }
                else
                {
                    OffsetValue = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);
                }

            }
            ResultNoOffset = BaseValue + IndexValue;
        }
        public (int, string, string) Disassemble()
        {
            string AdditionalReg = null;
            if (Bits.Base != 5)
            {
                AdditionalReg = RegisterMnemonics[Bits.Base][PointerSize];
            }
            else if (Mod > 0)
            {
                AdditionalReg = RegisterMnemonics[(int)ByteCode.BP][PointerSize];
            }
            string BaseReg = null;
            if (Bits.Index != 4)
            {
                BaseReg = RegisterMnemonics[Bits.Index][PointerSize];
            }
            return (Bits.Scale, BaseReg, AdditionalReg);
        }
    }
}
