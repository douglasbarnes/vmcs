using System;
using debugger.Util;
using debugger.Emulator.DecodedTypes;
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

        private readonly ModRM.Mod Mod;
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
        public SIB(byte sibByte, ModRM.Mod mod)
        {
            string SIBbits = Bitwise.GetBits(sibByte);
            this = new SIB(Convert.ToByte(SIBbits.Substring(0, 2), 2), Convert.ToByte(SIBbits.Substring(2, 3), 2), Convert.ToByte(SIBbits.Substring(5, 3), 2), mod);
        }
        public SIB(byte scale, byte index, byte _base, ModRM.Mod mod)
        {
            (BaseValue, OffsetValue, IndexValue) = (0, 0, 0);
            if((ControlUnit.RexByte | REX.X) == ControlUnit.RexByte)
            {
                index |= 8;
            }
            if ((ControlUnit.RexByte | REX.B) == ControlUnit.RexByte)
            {
                _base |= 8;
            }
            Bits = new DeconstructedSIB(scale, index ,_base);
            Mod = mod;
            PointerSize = (ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD);
            if (index != 4)//4 == none
            {
                IndexValue = (byte)Math.Pow(2,scale) * BitConverter.ToUInt64(Bitwise.SignExtend(ControlUnit.FetchRegister((XRegCode)index, PointerSize), 8), 0);
            }

            if (_base != 5) // 5 = ptr or rbp+ptr
            {
                BaseValue = BitConverter.ToUInt64(ControlUnit.FetchRegister((XRegCode)_base, RegisterCapacity.QWORD), 0);
            }
            else
            {
                if (Mod != ModRM.Mod.Pointer) // mod1 mod2 = ebp+imm
                {
                    BaseValue = BitConverter.ToUInt64(Bitwise.SignExtend(ControlUnit.FetchRegister(XRegCode.BP, PointerSize), 8), 0);
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
                AdditionalReg = DisassembleRegister((XRegCode)Bits.Base, PointerSize, REX.NONE);
            }
            else if (Mod > 0)
            {
                AdditionalReg = DisassembleRegister(XRegCode.BP, PointerSize, REX.NONE);
            }
            string BaseReg = null;
            if (Bits.Index != 4)
            {
                BaseReg = DisassembleRegister((XRegCode)Bits.Index, PointerSize, REX.NONE);
            }
            return (Bits.Scale, BaseReg, AdditionalReg);
        }
    }
}
