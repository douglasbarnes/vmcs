// OpcodeTable provides calling tables for resolving fetched bytes into their correct opcodes. It is essential that the instruction pointer is aligned at all times, othewise 
// unwanted opcodes will execute and from then on the entire program will be interpreted differently. This is the cause for the complete automation of the instruction pointer
// through the opcode basee class and methods in ControlUnit.
// A plethora of information can be inferred from the given byte. This information is absolutely critical for factoring out the generic procedures that an opcode will run,
// making the opcodes classes themselves have hardly any code at all. By abstracting these procedures, such as decoding operands, the individual opcode class code becomes
// very demonstrative of the function the opcode performs. This information can be inferred because of the good processor design choices. IA-64/AMD64 architecture is super
// conservative of bytes used(for the most part). If you look at other architectures, they are an absolute compatibility nightmare, for example ARM. ARM uses 4 bytes per
// instruction every time, even a nop. This results in massive amount of memory wasted. Not to mention the instruction pointer points to 4 bytes ahead of the current instruction, what?? 
// Despite IA-64 being stuck with many legacy compatibility features, it certainly does well for the amount it has to uphold(basically still works with 8-bit programs).
// The layout of the OpcodeTable is super simple, but separated from the ControlUnit class file because of its size. 
// A tree-like hierarchy is used for tables. Here is an example with a few entries,
//  Main table
//  ├───0x01 - Add()
//  ├───0x08 - Or()
//  ├───0x0F - Two byte table
//  │   ├───0x80 - Jo()
//  │   └───0x81 - Jno()
//  └───0x80 - Extended Table
//      ├───0x80.0 - Add()
//      ├───0x80.1 - Or()
//      └───0x80.2 - Adc()
// As you can see, the extended tables and two byte table branch off from the main table if a specific byte was fetched. The difference between the two byte table and the extended table is that
// the extended table contains the extra opcode information in the following ModRM byte, but the two byte table requires a 0x0F byte to be read before the opcode to tell the processor that the
// other table should be used. Because of this, less common opcodes are pushed to the two byte table. Extended opcodes forfeit the source(reg bits) of their ModRM byte to allow one byte to
// represent up to 8 different opcodes(although all 8 are rarely used)
using System.Collections.Generic;
using debugger.Emulator.Opcodes;
using debugger.Emulator.DecodedTypes;

using static debugger.Emulator.Opcodes.OpcodeSettings;
namespace debugger.Emulator
{
    public static partial class ControlUnit
    {        
        private enum AlternateTable
        {
            EXTENDED=1,
        }
        private delegate IMyOpcode OpcodeCaller();
        private delegate IMyOpcode AlternateTableCaller(byte input);
        private static readonly Dictionary<AlternateTable, AlternateTableCaller> AlternateTableMap = new Dictionary<AlternateTable, AlternateTableCaller>()
        {
            { AlternateTable.EXTENDED,  (input) => DecodeExtension(input)}
        };
        private static readonly Dictionary<byte, Dictionary<byte, OpcodeCaller>> OpcodeTable = new Dictionary<byte, Dictionary<byte, OpcodeCaller>>()
        {
            {1, new Dictionary<byte, OpcodeCaller>()
                {
                  { 0x00, () => new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:false)},
                  { 0x01, () => new Add(new ModRM(FetchNext()), UseCarry:false) },
                  { 0x02, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:false)},
                  { 0x03, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:false)},
                  { 0x04, () => new Add(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE, UseCarry:false) },
                  { 0x05, () => new Add(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), UseCarry:false)},

                  { 0x08, () => new  Or(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x09, () => new  Or(new ModRM(FetchNext())) },
                  { 0x0A, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x0B, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x0C, () => new  Or(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0x0D, () => new  Or(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()))},
                  { 0x10, () => new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:true)},
                  { 0x11, () => new Add(new ModRM(FetchNext()), UseCarry:true) },
                  { 0x12, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:true)},
                  { 0x13, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:true)},
                  { 0x14, () => new Add(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE, UseCarry:true) },
                  { 0x15, () => new Add(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), UseCarry:true)},

                  { 0x18, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:true)},
                  { 0x19, () => new Sub(new ModRM(FetchNext()), UseBorrow:true) },
                  { 0x1A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:true)},
                  { 0x1B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:true)},
                  { 0x1C, () => new Sub(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE, UseBorrow:true) },
                  { 0x1D, () => new Sub(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), UseBorrow:true)},

                  { 0x20, () => new And(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x21, () => new And(new ModRM(FetchNext())) },
                  { 0x22, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x23, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x24, () => new And(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0x25, () => new And(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()))},
                  { 0x28, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:false)},
                  { 0x29, () => new Sub(new ModRM(FetchNext()), UseBorrow:false) },
                  { 0x2A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:false)},
                  { 0x2B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:false)},
                  { 0x2C, () => new Sub(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE, UseBorrow:false) },
                  { 0x2D, () => new Sub(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), UseBorrow:false)},
                  { 0x30, () => new Xor(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x31, () => new Xor(new ModRM(FetchNext())) },
                  { 0x32, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x33, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x34, () => new Xor(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0x35, () => new Xor(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()))},
                  { 0x38, () => new Cmp(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x39, () => new Cmp(new ModRM(FetchNext())) },
                  { 0x3A, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x3B, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x3C, () => new Cmp(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0x3D, () => new Cmp(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()))},
                  { 0x50, () => new Push(new RegisterHandle(XRegCode.A , RegisterTable.GP)) },
                  { 0x51, () => new Push(new RegisterHandle(XRegCode.C , RegisterTable.GP)) },
                  { 0x52, () => new Push(new RegisterHandle(XRegCode.D , RegisterTable.GP)) },
                  { 0x53, () => new Push(new RegisterHandle(XRegCode.B , RegisterTable.GP)) },
                  { 0x54, () => new Push(new RegisterHandle(XRegCode.SP, RegisterTable.GP)) },
                  { 0x55, () => new Push(new RegisterHandle(XRegCode.BP, RegisterTable.GP)) },
                  { 0x56, () => new Push(new RegisterHandle(XRegCode.SI, RegisterTable.GP)) },
                  { 0x57, () => new Push(new RegisterHandle(XRegCode.DI, RegisterTable.GP)) },
                  { 0x58, () => new Pop (new RegisterHandle(XRegCode.A , RegisterTable.GP)) },
                  { 0x59, () => new Pop (new RegisterHandle(XRegCode.C , RegisterTable.GP)) },
                  { 0x5A, () => new Pop (new RegisterHandle(XRegCode.D , RegisterTable.GP)) },
                  { 0x5B, () => new Pop (new RegisterHandle(XRegCode.B , RegisterTable.GP)) },
                  { 0x5C, () => new Pop (new RegisterHandle(XRegCode.SP, RegisterTable.GP)) },
                  { 0x5D, () => new Pop (new RegisterHandle(XRegCode.BP, RegisterTable.GP)) },
                  { 0x5E, () => new Pop (new RegisterHandle(XRegCode.SI, RegisterTable.GP)) },
                  { 0x5F, () => new Pop (new RegisterHandle(XRegCode.DI, RegisterTable.GP)) },
                  { 0x63, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP), "MOVSXD", signExtend:true, RegisterCapacity.DWORD) },
                  { 0x68, () => new Push(new Immediate()) }, 
                  { 0x69, () => new  Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED) },
                  { 0x6A, () => new Push(new Immediate(ImmediateSettings.SXTBYTE)) },
                  { 0x6B, () => new  Mul(new DecodedCompound(new ModRM(FetchNext(), ModRMSettings.SWAP), new Immediate(ImmediateSettings.SXTBYTE)), SIGNED) },

                  { 0x70, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.O )},
                  { 0x71, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NO)},
                  { 0x72, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.C )},
                  { 0x73, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NC)},
                  { 0x74, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.Z )},
                  { 0x75, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NZ)},
                  { 0x76, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NA)},
                  { 0x77, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.A )},
                  { 0x78, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.S )},
                  { 0x79, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NS)},
                  { 0x7A, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.P )},
                  { 0x7B, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.NP)},
                  { 0x7C, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.L )},
                  { 0x7D, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.GE)},
                  { 0x7E, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.LE)},
                  { 0x7F, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.G )},

                  { 0x80, () => AlternateTableMap[AlternateTable.EXTENDED](0x80) },
                  { 0x81, () => AlternateTableMap[AlternateTable.EXTENDED](0x81) },
                  { 0x83, () => AlternateTableMap[AlternateTable.EXTENDED](0x83) },
                  { 0x84, () => new Test(new ModRM(FetchNext()), BYTEMODE) },
                  { 0x85, () => new Test(new ModRM(FetchNext())) },
                  { 0x86, () => new Xchg(new ModRM(FetchNext()), BYTEMODE) },
                  { 0x87, () => new Xchg(new ModRM(FetchNext())) },

                  { 0x88, () => new Mov(new ModRM(FetchNext()), BYTEMODE) },
                  { 0x89, () => new Mov(new ModRM(FetchNext())) },
                  { 0x8A, () => new Mov(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE) },
                  { 0x8B, () => new Mov(new ModRM(FetchNext(), ModRMSettings.SWAP))},

                  { 0x8D, () => new Lea(new ModRM(FetchNext(), ModRMSettings.SWAP | ModRMSettings.HIDEPTR))},

                  { 0x8F, () => new Pop(new ModRM(FetchNext())) },
                  { 0x90, () => new Nop() },
                  { 0x91, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.C,RegisterTable.GP)) ) },
                  { 0x92, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.D,RegisterTable.GP)) ) },
                  { 0x93, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.B,RegisterTable.GP)) ) },
                  { 0x94, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.SP,RegisterTable.GP) )) },
                  { 0x95, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.BP,RegisterTable.GP) )) },
                  { 0x96, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.SI,RegisterTable.GP) )) },
                  { 0x97, () => new Xchg(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new RegisterHandle(XRegCode.DI,RegisterTable.GP) )) },
                  { 0x98, () => new Cbw(new RegisterHandle(XRegCode.A, RegisterTable.GP)) },

                  { 0xA4, () => new Movs(StringOpSettings.BYTEMODE) },
                  { 0xA5, () => new Movs() },
                  { 0xA6, () => new Cmps(StringOpSettings.BYTEMODE) },
                  { 0xA7, () => new Cmps() },
                  { 0xA8, () => new Test(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xA9, () => new Test(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate())) },
                  { 0xAA, () => new Stos(StringOpSettings.BYTEMODE) },
                  { 0xAB, () => new Stos() },
                  { 0xAC, () => new Lods(StringOpSettings.BYTEMODE) },
                  { 0xAD, () => new Lods() },
                  { 0xAE, () => new Scas(StringOpSettings.BYTEMODE) },
                  { 0xAF, () => new Scas() },
                  { 0xB0, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB1, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.C, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB2, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.D, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB3, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.B, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB4, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.SP, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB5, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.BP, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB6, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.SI, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB7, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.DI, RegisterTable.GP), new Immediate()), BYTEMODE) },
                  { 0xB8, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP),  new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xB9, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.C, RegisterTable.GP),  new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBA, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.D, RegisterTable.GP),  new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBB, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.B, RegisterTable.GP),  new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBC, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.SP, RegisterTable.GP), new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBD, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.BP, RegisterTable.GP), new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBE, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.SI, RegisterTable.GP), new Immediate(ImmediateSettings.ALLOWIMM64))) },
                  { 0xBF, () => new Mov(new DecodedCompound(new RegisterHandle(XRegCode.DI, RegisterTable.GP), new Immediate(ImmediateSettings.ALLOWIMM64))) },

                  { 0xC0, () => AlternateTableMap[AlternateTable.EXTENDED](0xC0) },
                  { 0xC1, () => AlternateTableMap[AlternateTable.EXTENDED](0xC1) },
                  { 0xC2, () => new Ret(new Immediate()) },
                  { 0xC3, () => new Ret(new NoOperands()) },
                  
                  { 0xC6, () => new Mov(new DecodedCompound(new ModRM(FetchNext(), ModRMSettings.EXTENDED), new Immediate()), BYTEMODE)},
                  { 0xC7, () => new Movx(new DecodedCompound(new ModRM(FetchNext(), ModRMSettings.EXTENDED), new Immediate()), "MOV", true, RegisterCapacity.QWORD)},

                  

                  { 0xD0, () => AlternateTableMap[AlternateTable.EXTENDED](0xD0) },
                  { 0xD1, () => AlternateTableMap[AlternateTable.EXTENDED](0xD1) },
                  { 0xD2, () => AlternateTableMap[AlternateTable.EXTENDED](0xD2) },
                  { 0xD3, () => AlternateTableMap[AlternateTable.EXTENDED](0xD3) },

                  { 0xE3, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE | ImmediateSettings.SXTBYTE), Condition.RCXZ) },
                  
                  { 0xE8, () => new Call(new Immediate(ImmediateSettings.RELATIVE)) },
                  { 0xE9, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), dwordOnly:true) },

                  { 0xEB, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NONE, BYTEMODE) },

                  { 0xF6, () => AlternateTableMap[AlternateTable.EXTENDED](0xF6) },
                  { 0xF7, () => AlternateTableMap[AlternateTable.EXTENDED](0xF7) },
                  { 0xF8, () => new Clc(new NoOperands())},
                  { 0xF9, () => new Stc(new NoOperands())},

                  { 0xFC, () => new Cld(new NoOperands())},
                  { 0xFD, () => new Std(new NoOperands())},

                  { 0xFF, () => AlternateTableMap[AlternateTable.EXTENDED](0xFF) }
                }
            },
            {2, new Dictionary<byte, OpcodeCaller>()
                {
                    { 0x80, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.O ) },
                    { 0x81, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NO) },
                    { 0x82, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.C ) },
                    { 0x83, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NC) },
                    { 0x84, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.Z ) },
                    { 0x85, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NZ) },
                    { 0x86, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NA) },
                    { 0x87, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.A ) },
                    { 0x88, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.S ) },
                    { 0x89, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NS) },
                    { 0x8A, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.P ) },
                    { 0x8B, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.NP) },
                    { 0x8C, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.L ) },
                    { 0x8D, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.GE) },
                    { 0x8E, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.LE) },
                    { 0x8F, () => new Jmp(new Immediate(ImmediateSettings.RELATIVE), Condition.G ) },
                    { 0x90, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.O) },
                    { 0x91, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NO) },
                    { 0x92, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.C) },
                    { 0x93, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NC) },
                    { 0x94, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.Z) },
                    { 0x95, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NZ) },
                    { 0x96, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NA) },
                    { 0x97, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.A) },
                    { 0x98, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.S) },
                    { 0x99, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NS) },
                    { 0x9A, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.P) },
                    { 0x9B, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.NP) },
                    { 0x9C, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.L) },
                    { 0x9D, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.GE) },
                    { 0x9E, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.LE) },
                    { 0x9F, () => new Set(new ModRM(FetchNext(), ModRMSettings.EXTENDED), Condition.G) },

                    { 0xAF, () => new Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED)},

                    { 0xB6, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, desiredSourceSize:RegisterCapacity.BYTE)},
                    { 0xB7, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, desiredSourceSize:RegisterCapacity.WORD)},

                    { 0xBE, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, desiredSourceSize:RegisterCapacity.BYTE)},
                    { 0xBF, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, desiredSourceSize:RegisterCapacity.WORD)},
                }
            }
        };
        // format <byte size of opcode, <determining byte of opcode, <offset of extension, method to return opcode>>>
        private delegate Opcode ExtendedOpcodeCaller(ModRM input); // the opcodeinput of an extended opcode can be determined from a modrm(aka pretty much the same every time)
        private static readonly Dictionary<byte, Dictionary<int, ExtendedOpcodeCaller>> ExtendedOpcodeTable = new Dictionary<byte, Dictionary<int, ExtendedOpcodeCaller>>()
        {
            { 0x80, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE, UseCarry:false) },
                { 1, (InputModRM) => new Or (new DecodedCompound(InputModRM, new Immediate()), BYTEMODE) },
                { 2, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE, UseCarry:true) },
                { 3, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE, UseBorrow: true) },
                { 4, (InputModRM) => new And(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE)},
                { 5, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE, UseBorrow: false) } ,
                { 6, (InputModRM) => new Xor(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE) }, 
                { 7, (InputModRM) => new Cmp(new DecodedCompound(InputModRM, new Immediate()), BYTEMODE) },
            }
            },
            { 0x81, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate()), UseCarry:false) },
                { 1, (InputModRM) => new Or (new DecodedCompound(InputModRM, new Immediate())) },
                { 2, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate()), UseCarry:true) },
                { 3, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate()), UseBorrow: true) },
                { 4, (InputModRM) => new And(new DecodedCompound(InputModRM, new Immediate()))},
                { 5, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate()), UseBorrow: false) } ,
                { 6, (InputModRM) => new Xor(new DecodedCompound(InputModRM, new Immediate())) },
                { 7, (InputModRM) => new Cmp(new DecodedCompound(InputModRM, new Immediate())) },
            }
            },
            { 0x83, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), UseCarry:false) },
                { 1, (InputModRM) => new Or (new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE))) },
                { 2, (InputModRM) => new Add(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), UseCarry:true) },
                { 3, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), UseBorrow: true) },
                { 4, (InputModRM) => new And(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)))},
                { 5, (InputModRM) => new Sub(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), UseBorrow: false) } ,
                { 6, (InputModRM) => new Xor(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE))) },
                { 7, (InputModRM) => new Cmp(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE))) },
            }
            },
            { 0xC0, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:false, BYTEMODE) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:false, BYTEMODE) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:true, BYTEMODE) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:true, BYTEMODE) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), BYTEMODE) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), arithmetic:false, BYTEMODE) },

                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), arithmetic:true, BYTEMODE) },
            }
            },
            { 0xC1, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:false) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:false) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:true) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), useCarry:true) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE))) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), arithmetic:false) },
                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Immediate(ImmediateSettings.SXTBYTE)), arithmetic:true) },
            }
            },
            { 0xD0, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Constant(1)), useCarry:false, BYTEMODE) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Constant(1)), useCarry:false, BYTEMODE) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Constant(1)), useCarry:true, BYTEMODE) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Constant(1)), useCarry:true, BYTEMODE) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM, new Constant(1)), BYTEMODE) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Constant(1)), arithmetic:false, BYTEMODE) },

                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Constant(1)), arithmetic:true, BYTEMODE) },
            }
            },
            { 0xD1, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Constant(1)), useCarry:false) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Constant(1)), useCarry:false) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new Constant(1)), useCarry:true) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new Constant(1)), useCarry:true) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM, new Constant(1))) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Constant(1)), arithmetic:false) },

                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new Constant(1)), arithmetic:true) },
            }
            },
            { 0xD2, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:false, BYTEMODE) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:false, BYTEMODE) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:true, BYTEMODE) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:true, BYTEMODE) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), BYTEMODE) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), arithmetic:false, BYTEMODE) },
                                                                            
                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM, new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), arithmetic:true, BYTEMODE) },
            }
            },
            { 0xD3, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Rxl(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:false) },
                { 1, (InputModRM) => new Rxr(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:false) },
                { 2, (InputModRM) => new Rxl(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:true) },
                { 3, (InputModRM) => new Rxr(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), useCarry:true) },
                { 4, (InputModRM) => new Shl(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT))) },
                { 5, (InputModRM) => new Sxr(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), arithmetic:false) },

                { 7, (InputModRM) => new Sxr(new DecodedCompound(InputModRM,  new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT)), arithmetic:true) },
            }
            },
            { 0xF6, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Test(new DecodedCompound( InputModRM, new Immediate()), BYTEMODE) },
                { 3, (InputModRM) => new Neg(InputModRM, BYTEMODE) },
                { 4, (InputModRM) => new Mul(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.WORD,RegisterHandleSettings.NO_INIT), InputModRM), BYTEMODE) },
                { 5, (InputModRM) => new Mul(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.WORD,RegisterHandleSettings.NO_INIT), InputModRM), BYTEMODE | SIGNED) },
                { 6, (InputModRM) => new Div(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.WORD,RegisterHandleSettings.NO_INIT),InputModRM), BYTEMODE) },
                { 7, (InputModRM) => new Div(new DecodedCompound(new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.WORD,RegisterHandleSettings.NO_INIT),InputModRM), BYTEMODE | SIGNED) }
            }
            },
            { 0xF7, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Test(new DecodedCompound(InputModRM, new Immediate())) },
                { 3, (InputModRM) => new Neg(InputModRM) },
                { 4, (InputModRM) => new Mul(new DecodedCompound(new SplitRegisterHandle(XRegCode.A, XRegCode.D), InputModRM)) },
                { 5, (InputModRM) => new Mul(new DecodedCompound(new SplitRegisterHandle(XRegCode.A, XRegCode.D), InputModRM), SIGNED) },
                { 6, (InputModRM) => new Div(new DecodedCompound(new SplitRegisterHandle(XRegCode.A, XRegCode.D, SplitRegisterHandleSettings.FETCH_UPPER), InputModRM))},
                { 7, (InputModRM) => new Div(new DecodedCompound(new SplitRegisterHandle(XRegCode.A, XRegCode.D, SplitRegisterHandleSettings.FETCH_UPPER), InputModRM), SIGNED) }
            }
            },
            { 0xFE, new Dictionary<int, ExtendedOpcodeCaller>
            {
                 { 0, (InputModRM) => new Inc(InputModRM, BYTEMODE) },
                 { 1, (InputModRM) => new Dec(InputModRM, BYTEMODE) },
            }
            },
            { 0xFF, new Dictionary<int, ExtendedOpcodeCaller>
            {
                { 0, (InputModRM) => new Inc(InputModRM) },
                { 1, (InputModRM) => new Dec(InputModRM) },
                { 2, (InputModRM) => new Call(InputModRM) },
                { 4, (InputModRM) => new Jmp(InputModRM) },
                { 6, (InputModRM) => new Push(InputModRM) }
            }
            }            
            
        };
        //byte = opcode, Dict<byte = displacement(increment doesnt matter), action = thing to do>
        // opcodes with one operand e.g inc eax (increment eax) don't use the REG bits in modRM, so the regbits can be used to make extra instructions instead
        // so we can have one "opcode byte" that has meanings depending on the REG bits
        // reg bit starts at the 8 dec val position in a string of bits e.g "01[100]111" [REG] so each offset is -8 each time
        private static Opcode DecodeExtension(byte opcode)
        {
            ModRM InputModRM = new ModRM(FetchNext(), ModRMSettings.EXTENDED);
            ExtendedOpcodeCaller Output;
            if(!ExtendedOpcodeTable[opcode].TryGetValue((int)InputModRM.Source.Code, out Output))
            {
                RaiseException(Logging.LogCode.INVALID_OPCODE);
            }
            return ExtendedOpcodeTable[opcode][(int)InputModRM.Source.Code](InputModRM);
        }
    }
}
