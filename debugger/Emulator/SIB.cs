using System;
using debugger.Util;
using debugger.Emulator.DecodedTypes;
using static debugger.Util.Disassembly;
namespace debugger.Emulator
{
    public struct SIB
    {
        public RegisterCapacity PointerSize;
        public readonly DisassembledPointer Disassemble;
        public ulong Destination;
        public long OffsetValue; // a way to avoid this? when we have a sib where its [reg + ebp + offset], we cant disassemble that visibly without this because ebp could change giving the wrong offset displayed
        private readonly ModRM.Mod Mod;
        private readonly ControlUnit.RegisterHandle BaseHandle;
        private readonly ControlUnit.RegisterHandle IndexHandle;        
        public SIB(byte sibByte, ModRM.Mod mod)
        {
            string SIBbits = Bitwise.GetBits(sibByte);
            this = new SIB(Convert.ToByte(SIBbits.Substring(0, 2), 2), Convert.ToByte(SIBbits.Substring(2, 3), 2), Convert.ToByte(SIBbits.Substring(5, 3), 2), mod);
        }
        public SIB(byte scale, byte index, byte _base, ModRM.Mod mod)
        {
            Disassemble = new DisassembledPointer();
            ulong BaseValue = 0;
            ulong IndexValue = 0;
            OffsetValue = 0;
            index |= (byte)(ControlUnit.RexByte & REX.X);
            _base |= (byte)(ControlUnit.RexByte & REX.B);
            Mod = mod;
            PointerSize = (ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD);
            if (index != 4)//4 == none
            {
                IndexHandle = new ControlUnit.RegisterHandle((XRegCode)index, RegisterTable.GP, PointerSize);
                IndexValue = (byte)Math.Pow(2,scale) * BitConverter.ToUInt64(Bitwise.SignExtend(IndexHandle.FetchOnce(), 8), 0);
                Disassemble.IndexReg = IndexHandle.DisassembleOnce();
                Disassemble.IndexScale = scale;
            }
            else
            {
                IndexHandle = null;
            }
            if (_base != 5) // 5 = ptr or rbp+ptr
            {
                BaseHandle = new ControlUnit.RegisterHandle((XRegCode)_base, RegisterTable.GP, RegisterCapacity.QWORD);
                BaseValue = BitConverter.ToUInt64(BaseHandle.FetchOnce(), 0);
                Disassemble.AdditionalReg = BaseHandle.DisassembleOnce();
            }
            else if (Mod != ModRM.Mod.Pointer) // mod1 mod2 = ebp+imm
            {
                BaseHandle = new ControlUnit.RegisterHandle(XRegCode.BP, RegisterTable.GP, PointerSize);
                BaseValue = BitConverter.ToUInt64(Bitwise.SignExtend(BaseHandle.FetchOnce(), 8), 0);
                Disassemble.AdditionalReg = BaseHandle.DisassembleOnce();
            }
            else
            {
                BaseHandle = null;
                OffsetValue = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);
            }
            Destination = BaseValue + IndexValue;
        }
    }
}