// The SIB struct is an implementation of SIB bytes in assembly.  It *could* be merged into the ModRM class, but in the past it turned out really messy. I find there are only
// advantages to having it separate, for example if you wanted to optimise further, it would be really simple as everything you see is to do with SIBs. It would be impossible
// (or atleast challenging) to break another part of the program by modifying here. Conversely, the ModRM class is already finicky enough as-is, so by reducing SIB handling to
// just two lines there makes it a lot easier to debug the ModRM class.
// A SIB is different from an IMyDecoded because of a few reasons.
//  1. It is a struct an cannot inherit
//  2. It requires a ModRM before it, there would be no purpose for it otherwise. 
//  3. SIBs only store information for one operand, be that the destination or the source.
using System;
using debugger.Util;
using debugger.Emulator.DecodedTypes;
using static debugger.Util.Disassembly;
namespace debugger.Emulator
{
    public struct SIB
    {
        public RegisterCapacity PointerSize;
        public readonly DeconstructedPointer Disassemble;
        public ulong Destination;
        public long OffsetValue; 
        private readonly ModRM.Mod Mod;
        private readonly ControlUnit.RegisterHandle BaseHandle;
        private readonly ControlUnit.RegisterHandle IndexHandle;
        private struct DeconstructedSIB
        {
            // This struct provides easily readable properties for masking the correct bits in a SIB byte to get the desired field.
            // This makes the code a lot more readable and reduces margin of error.
            // A SIB is constructed like,
            // ------------------------------
            // |   7  6 | 5  4  3 | 2  1  0 |
            // |  SCALE |  INDEX  |  BASE   |
            // ------------------------------
            //
            // So the byte 0xE2 translates to 
            //  Scale = 3
            //  Index = 4
            //  Base = 2
            // The Mod of the preceeding ModRM byte is also used to determine whether EBP is also added to the resulting pointer.

            // This mask will only return bit 7 and 6 [11000000]
            public byte Scale { get => (byte)((Internal_SIB & 0xC0) >> 6); }

            // This mask will only return bits 5, 4, and 3. The value is increased by 8 if ExtendReg.
            public byte Index { get => (byte)(((Internal_SIB & 0x38) >> 3) | (ExtendBase ? 8 : 0)); }

            // Finally, this mask will return bits 2, 1, and 0. The value is increased by 8 if ExtendMem.
            public byte Base { get => (byte)((Internal_SIB & 0x7) | (ExtendIndex ? 8 : 0)); }

            private readonly byte Internal_SIB;
            public bool ExtendBase;
            public bool ExtendIndex;
            public DeconstructedSIB(byte inputSIB)
            {
                Internal_SIB = inputSIB;
                ExtendBase = false;
                ExtendIndex = false;
            }
        }
        public SIB(byte inputSIB, ModRM.Mod mod)
        {
            DeconstructedSIB Fields = new DeconstructedSIB(inputSIB); 
            // To save complication, the SIB is disassembled as it is decoded. Otherwise, more variables would have to be stored.
            Disassemble = new DeconstructedPointer();
            OffsetValue = 0;
            ulong BaseValue = 0;
            ulong IndexValue = 0;

            // If there was a REX.X or a REX.B, adjust the fields accordingly(add 8). 
            Fields.ExtendIndex = (ControlUnit.RexByte | REX.X) == ControlUnit.RexByte;
            Fields.ExtendBase = (ControlUnit.RexByte | REX.B) == ControlUnit.RexByte;

            Mod = mod;

            // The RegisterCapacity of each register used inside the SIB is either a DWORD or a QWORD, the former only if there is an ADDROVR prefix present.
            PointerSize = (ControlUnit.LPrefixBuffer.Contains(PrefixByte.ADDROVR) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD);

            // If the index isn't 4, there is an index register that needs to be added to the SIB. Otherwise it is just a base or an immediate pointer.
            if (Fields.Index != 4)
            {
                // Create a new RegisterHandle pointing to the index regster(Always from the GP table)
                IndexHandle = new ControlUnit.RegisterHandle((XRegCode)Fields.Index, RegisterTable.GP, PointerSize);

                // To get the actual value of the index, multiply it by the scale, which is equal to 2^^(scale bits value)
                // For example, 
                //  Scale bits == 3
                //  2^^3 = 8
                //  Index *= 8
                IndexValue = (byte)Math.Pow(2,Fields.Scale) * BitConverter.ToUInt64(Bitwise.SignExtend(IndexHandle.FetchOnce(), 8), 0);
                
                // The scale coefficient still needs to be shown in the disassembly.
                Disassemble.IndexScale = Fields.Scale;
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
            if (Fields.Base != 5) 
            {
                BaseHandle = new ControlUnit.RegisterHandle((XRegCode)Fields.Base, RegisterTable.GP, RegisterCapacity.QWORD);
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
