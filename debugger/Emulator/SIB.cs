using System;
using debugger.Util;
using debugger.Emulator.DecodedTypes;
using static debugger.Util.Disassembly;
namespace debugger.Emulator
{
    public struct SIB
    {
        // A SIB is different from an IMyDecoded because of a few reasons.
        //  1. It is a struct an cannot inherit
        //  2. It requires a ModRM before it, there would be no purpose for it otherwise. 
        //  3. SIBs only store information for one operand, be that the destination or the source.

        public RegisterCapacity PointerSize;
        public readonly DisassembledPointer Disassemble;
        public ulong Destination;
        public long OffsetValue; 
        private readonly ModRM.Mod Mod;
        private readonly ControlUnit.RegisterHandle BaseHandle;
        private readonly ControlUnit.RegisterHandle IndexHandle;        
        public SIB(byte sibByte, ModRM.Mod mod)
        {
            // Separate the SIB byte into the scale, index and base.
            // Take 0xE2 for example,
            //    [11100010]
            //  [11][100][010]
            //   ^    ^    ^
            //   |  Index  |
            //  Scale     Base
            // So the byte 0xE2 translates to 
            //  Scale = 3
            //  Index = 4
            //  Base = 2
            // The Mod of the preceeding ModRM byte is also used to determine whether EBP is also added to the resulting pointer.
            string SIBbits = Bitwise.GetBits(sibByte);            
            this = new SIB(Convert.ToByte(SIBbits.Substring(0, 2), 2), Convert.ToByte(SIBbits.Substring(2, 3), 2), Convert.ToByte(SIBbits.Substring(5, 3), 2), mod);
        }
        public SIB(byte scale, byte index, byte _base, ModRM.Mod mod)
        {
            // To save complication, the SIB is disassembled as it is decoded. Otherwise, more variables would have to be stored.
            Disassemble = new DisassembledPointer();
            OffsetValue = 0;
            ulong BaseValue = 0;
            ulong IndexValue = 0;

            // If there was a REX.X or a REX.B, adjust the fields accordingly(add 8). 
            index |= ((ControlUnit.RexByte & REX.X) == ControlUnit.RexByte ? 8 : 0);
            _base |= ((ControlUnit.RexByte & REX.B) == ControlUnit.RexByte ? 8 : 0);

            Mod = mod;

            // The RegisterCapacity of each register used inside the SIB is either a DWORD or a QWORD, the former only if there is an ADDROVR prefix present.
            PointerSize = (ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD);

            // If the index isn't 4, there is an index register that needs to be added to the SIB. Otherwise it is just a base or an immediate pointer.
            if (index != 4)
            {
                // Create a new RegisterHandle pointing to the index regster(Always from the GP table)
                IndexHandle = new ControlUnit.RegisterHandle((XRegCode)index, RegisterTable.GP, PointerSize);

                // To get the actual value of the index, multiply it by the scale, which is equal to 2^^(scale bits value)
                // For example, 
                //  Scale bits == 3
                //  2^^3 = 8
                //  Index *= 8
                IndexValue = (byte)Math.Pow(2,scale) * BitConverter.ToUInt64(Bitwise.SignExtend(IndexHandle.FetchOnce(), 8), 0);
                
                // The scale coefficient still needs to be shown in the disassembly.
                Disassemble.IndexScale = scale;
                Disassemble.IndexReg = IndexHandle.DisassembleOnce();
            }
            else
            {
                // If the index bits were equal to 4, there is no Index register, but this is a struct and therefore still needs to be assigned.
                IndexHandle = null;
            }
            
            // Note that any additional immediate displacement are dealt with in the ModRM class
            // The base bits being 5 denotes that there is an immediate pointer following the SIB byte, this is the only way of hard coding a pointer to a specific location as of now. An immediate displacement in the ModRM and a SIB absolute address pointer are not mutually exclusive.

            // If the base isn't 5, there is a base register encoded in the SIB. The base being 5 denotes that there is only a pointer(which could be 0)
            if (_base != 5) 
            {
                BaseHandle = new ControlUnit.RegisterHandle((XRegCode)_base, RegisterTable.GP, RegisterCapacity.QWORD);
                BaseValue = BitConverter.ToUInt64(BaseHandle.FetchOnce(), 0);
                Disassemble.AdditionalReg = BaseHandle.DisassembleOnce();
            }

            // If the Mod bits of the preceeding ModRM byte do not equal 0 and the base bits equal 5, $EBP is used as the base
            else if (Mod != ModRM.Mod.Pointer) 
            {
                BaseHandle = new ControlUnit.RegisterHandle(XRegCode.BP, RegisterTable.GP, PointerSize);
                BaseValue = BitConverter.ToUInt64(Bitwise.SignExtend(BaseHandle.FetchOnce(), 8), 0);
                Disassemble.AdditionalReg = BaseHandle.DisassembleOnce();
            }

            // The other case, $_base != 5 and Mod == 0, means that there is an absolute pointer stored in the immediate 4 bytes.
            else
            {
                BaseHandle = null;
                OffsetValue = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);
            }
            
            // The ModRM byte that called this constructor only cares about the destination(or effective address if you want) of the SIB, but the rest is stored beyond that purely for disassembly.
            Destination = BaseValue + IndexValue;
        }
    }
}
