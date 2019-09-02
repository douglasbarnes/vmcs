using System.Collections.Generic;
using debugger.Emulator.Opcodes;
using debugger.Emulator.DecodedTypes;

using static debugger.Emulator.Opcodes.OpcodeSettings;
namespace debugger.Emulator
{
    public static partial class ControlUnit
    {        
        private delegate IMyOpcode OpcodeCaller();
        // format <byte size of opcode, <determining byte of opcode, method to return opcode>>
        private static readonly Dictionary<byte, Dictionary<byte, OpcodeCaller>> OpcodeTable = new Dictionary<byte, Dictionary<byte, OpcodeCaller>>()
        {
            {1, new Dictionary<byte, OpcodeCaller>()
                {
                  { 0x00, () => new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:false)},
                  { 0x01, () => new Add(new ModRM(FetchNext()), UseCarry:false) },
                  { 0x02, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:false)},
                  { 0x03, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:false)},
                  { 0x04, () => new Add(new ImplicitRegister(XRegCode.A), BYTEMODE | IMMEDIATE, UseCarry:false) },
                  { 0x05, () => new Add(new ImplicitRegister(XRegCode.A), IMMEDIATE, UseCarry:false)},

                  { 0x08, () => new  Or(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x09, () => new  Or(new ModRM(FetchNext())) },
                  { 0x0A, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x0B, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x0C, () => new  Or(new ImplicitRegister(XRegCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x0D, () => new  Or(new ImplicitRegister(XRegCode.A), IMMEDIATE)},
                  { 0x10, () =>  new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:true)},
                  { 0x11, () =>  new Add(new ModRM(FetchNext()), UseCarry:true) },
                  { 0x12, () =>  new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:true)},
                  { 0x13, () =>  new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:true)},
                  { 0x14, () =>  new Add(new ImplicitRegister(XRegCode.A), BYTEMODE | IMMEDIATE, UseCarry:true) },
                  { 0x15, () =>  new Add(new ImplicitRegister(XRegCode.A), IMMEDIATE , UseCarry:true)},

                  { 0x18, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:true)},
                  { 0x19, () => new Sub(new ModRM(FetchNext()), UseBorrow:true) },
                  { 0x1A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:true)},
                  { 0x1B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:true)},
                  { 0x1C, () => new Sub(new ImplicitRegister(XRegCode.A), BYTEMODE | IMMEDIATE, UseBorrow:true) },
                  { 0x1D, () => new Sub(new ImplicitRegister(XRegCode.A), IMMEDIATE, UseBorrow:true)},

                  { 0x20, () => new And(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x21, () => new And(new ModRM(FetchNext())) },
                  { 0x22, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x23, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x24, () => new And(new ImplicitRegister(XRegCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x25, () => new And(new ImplicitRegister(XRegCode.A), IMMEDIATE)},
                  //0x27 DAA, 32 ONLY
                  { 0x28, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:false)},
                  { 0x29, () => new Sub(new ModRM(FetchNext()), UseBorrow:false) },
                  { 0x2A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:false)},
                  { 0x2B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:false)},
                  { 0x2C, () => new Sub(new ImplicitRegister(XRegCode.A), BYTEMODE | IMMEDIATE, UseBorrow:false) },
                  { 0x2D, () => new Sub(new ImplicitRegister(XRegCode.A), IMMEDIATE, UseBorrow:false)},
                  //0X2F DAS, 32 ONLY
                  { 0x30, () => new Xor(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x31, () => new Xor(new ModRM(FetchNext())) },
                  { 0x32, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x33, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x34, () => new Xor(new ImplicitRegister(XRegCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x35, () => new Xor(new ImplicitRegister(XRegCode.A), IMMEDIATE)},
                  //0x37 AAA 32 ONLY
                  { 0x38, () => new Cmp(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x39, () => new Cmp(new ModRM(FetchNext())) },
                  { 0x3A, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x3B, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x3C, () => new Cmp(new ImplicitRegister(XRegCode.A), BYTEMODE | IMMEDIATE) },
                  { 0x3D, () => new Cmp(new ImplicitRegister(XRegCode.A), IMMEDIATE)},
                  //0X3F AAS 32 ONLY
                  { 0x50, () => new Push(new ImplicitRegister(XRegCode.A )) },
                  { 0x51, () => new Push(new ImplicitRegister(XRegCode.C )) },
                  { 0x52, () => new Push(new ImplicitRegister(XRegCode.D )) },
                  { 0x53, () => new Push(new ImplicitRegister(XRegCode.B )) },
                  { 0x54, () => new Push(new ImplicitRegister(XRegCode.SP)) },
                  { 0x55, () => new Push(new ImplicitRegister(XRegCode.BP)) },
                  { 0x56, () => new Push(new ImplicitRegister(XRegCode.SI)) },
                  { 0x57, () => new Push(new ImplicitRegister(XRegCode.DI)) },
                  { 0x58, () => new Pop (new ImplicitRegister(XRegCode.A )) },
                  { 0x59, () => new Pop (new ImplicitRegister(XRegCode.C )) },
                  { 0x5A, () => new Pop (new ImplicitRegister(XRegCode.D )) },
                  { 0x5B, () => new Pop (new ImplicitRegister(XRegCode.B )) },
                  { 0x5C, () => new Pop (new ImplicitRegister(XRegCode.SP)) },
                  { 0x5D, () => new Pop (new ImplicitRegister(XRegCode.BP)) },
                  { 0x5E, () => new Pop (new ImplicitRegister(XRegCode.SI)) },
                  { 0x5F, () => new Pop (new ImplicitRegister(XRegCode.DI)) },
                  //0x60 PUSHA 32 ONLY
                  //0x61 POPA 32 ONLY
                  { 0x63, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP), "MOVSXD", signExtend:true, RegisterCapacity.GP_DWORD) },
                  //67 prefix
                  { 0x68, () => new Push(new NoOperands(), IMMEDIATE) }, // its always 32, weird
                  { 0x69, () => new  Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED | IMMEDIATE) },
                  { 0x6A, () => new Push(new NoOperands(), IMMEDIATE | SXTBYTE) },
                  { 0x6B, () => new  Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED | IMMEDIATE | SXTBYTE) },

                  { 0x70, () => new Jmp(new NoOperands(), Condition.O , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x71, () => new Jmp(new NoOperands(), Condition.NO, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x72, () => new Jmp(new NoOperands(), Condition.C , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x73, () => new Jmp(new NoOperands(), Condition.NC, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x74, () => new Jmp(new NoOperands(), Condition.Z , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x75, () => new Jmp(new NoOperands(), Condition.NZ, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x76, () => new Jmp(new NoOperands(), Condition.NA, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x77, () => new Jmp(new NoOperands(), Condition.A , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x78, () => new Jmp(new NoOperands(), Condition.S , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x79, () => new Jmp(new NoOperands(), Condition.NS, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7A, () => new Jmp(new NoOperands(), Condition.P , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7B, () => new Jmp(new NoOperands(), Condition.NP, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7C, () => new Jmp(new NoOperands(), Condition.L , IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7D, () => new Jmp(new NoOperands(), Condition.GE, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7E, () => new Jmp(new NoOperands(), Condition.LE, IMMEDIATE | RELATIVE | SXTBYTE) },
                  { 0x7F, () => new Jmp(new NoOperands(), Condition.G , IMMEDIATE | RELATIVE | SXTBYTE) },

                  { 0x80, () => DecodeExtension(0x80, 1) },
                  { 0x81, () => DecodeExtension(0x81, 1) },
                  { 0x83, () => DecodeExtension(0x83, 1) },
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
                  { 0x91, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.C )) },
                  { 0x92, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.D )) },
                  { 0x93, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.B )) },
                  { 0x94, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.SP )) },
                  { 0x95, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.BP )) },
                  { 0x96, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.SI )) },
                  { 0x97, () => new Xchg(new ImplicitRegister(XRegCode.A, XRegCode.DI )) },
                  { 0x98, () => new Cbw(new ImplicitRegister(XRegCode.A)) },

                  { 0xA4, () => new Movs(StringOpSettings.BYTEMODE) },
                  { 0xA5, () => new Movs() },
                  { 0xA6, () => new Cmps(StringOpSettings.BYTEMODE) },
                  { 0xA7, () => new Cmps() },
                  { 0xA8, () => new Test(new ImplicitRegister(XRegCode.A ), BYTEMODE | IMMEDIATE) },
                  { 0xA9, () => new Test(new ImplicitRegister(XRegCode.A ), IMMEDIATE) },
                  { 0xAA, () => new Stos(StringOpSettings.BYTEMODE) },
                  { 0xAB, () => new Stos() },
                  { 0xAC, () => new Lods(StringOpSettings.BYTEMODE) },
                  { 0xAD, () => new Lods() },
                  { 0xAE, () => new Scas(StringOpSettings.BYTEMODE) },
                  { 0xAF, () => new Scas() },
                  { 0xB0, () => new Mov(new ImplicitRegister(XRegCode.A ), BYTEMODE | IMMEDIATE) },
                  { 0xB1, () => new Mov(new ImplicitRegister(XRegCode.C ), BYTEMODE | IMMEDIATE) },
                  { 0xB2, () => new Mov(new ImplicitRegister(XRegCode.D ), BYTEMODE | IMMEDIATE) },
                  { 0xB3, () => new Mov(new ImplicitRegister(XRegCode.B ), BYTEMODE | IMMEDIATE) },
                  { 0xB4, () => new Mov(new ImplicitRegister(XRegCode.SP), BYTEMODE | IMMEDIATE) },
                  { 0xB5, () => new Mov(new ImplicitRegister(XRegCode.BP), BYTEMODE | IMMEDIATE) },
                  { 0xB6, () => new Mov(new ImplicitRegister(XRegCode.SI), BYTEMODE | IMMEDIATE) },
                  { 0xB7, () => new Mov(new ImplicitRegister(XRegCode.DI), BYTEMODE | IMMEDIATE) },
                  { 0xB8, () => new Mov(new ImplicitRegister(XRegCode.A ), IMMEDIATE | ALLOWIMM64) },
                  { 0xB9, () => new Mov(new ImplicitRegister(XRegCode.C ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBA, () => new Mov(new ImplicitRegister(XRegCode.D ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBB, () => new Mov(new ImplicitRegister(XRegCode.B ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBC, () => new Mov(new ImplicitRegister(XRegCode.SP), IMMEDIATE | ALLOWIMM64) },
                  { 0xBD, () => new Mov(new ImplicitRegister(XRegCode.BP), IMMEDIATE | ALLOWIMM64) },
                  { 0xBE, () => new Mov(new ImplicitRegister(XRegCode.SI), IMMEDIATE | ALLOWIMM64) },
                  { 0xBF, () => new Mov(new ImplicitRegister(XRegCode.DI), IMMEDIATE | ALLOWIMM64) },

                  { 0xC0, () => DecodeExtension(0xC0, 1) },
                  { 0xC1, () => DecodeExtension(0xC1, 1) },
                  { 0xC2, () => new Ret(new NoOperands(), IMMEDIATE) },
                  { 0xC3, () => new Ret(new NoOperands()) },
                  
                  { 0xC6, () => new Mov(new ModRM(FetchNext(), ModRMSettings.EXTENDED), IMMEDIATE | BYTEMODE)},
                  { 0xC7, () => new Movx(new ModRM(FetchNext(), ModRMSettings.EXTENDED), "MOV", signExtend:true, RegisterCapacity.GP_QWORD, IMMEDIATE)},

                  

                  { 0xD0, () => DecodeExtension(0xD0, 1) },
                  { 0xD1, () => DecodeExtension(0xD1, 1) },
                  { 0xD2, () => DecodeExtension(0xD2, 1) },
                  { 0xD3, () => DecodeExtension(0xD3, 1) },

                  { 0xE3, () => new Jmp(new NoOperands(), Condition.RCXZ, IMMEDIATE | RELATIVE | SXTBYTE) },
                  
                  { 0xE8, () => new Call(new NoOperands(), IMMEDIATE | RELATIVE) },
                  { 0xE9, () => new Jmp(new NoOperands(), Condition.NONE, IMMEDIATE | RELATIVE, dwordOnly:true) },

                  { 0xEB, () => new Jmp(new NoOperands(), Condition.NONE, BYTEMODE | IMMEDIATE | RELATIVE) },

                  { 0xF6, () => DecodeExtension(0xF6, 1) },
                  { 0xF7, () => DecodeExtension(0xF7, 1) },
                  { 0xF8, () => new Clc(new NoOperands())},
                  { 0xF9, () => new Stc(new NoOperands())},

                  { 0xFC, () => new Cld(new NoOperands())},
                  { 0xFD, () => new Std(new NoOperands())},

                  { 0xFF, () => DecodeExtension(0xFF, 1) }
                }
            },
            {2, new Dictionary<byte, OpcodeCaller>()
                {
                    { 0x80, () => new Jmp(new NoOperands(), Condition.O , IMMEDIATE | RELATIVE) },
                    { 0x81, () => new Jmp(new NoOperands(), Condition.NO, IMMEDIATE | RELATIVE) },
                    { 0x82, () => new Jmp(new NoOperands(), Condition.C , IMMEDIATE | RELATIVE) },
                    { 0x83, () => new Jmp(new NoOperands(), Condition.NC, IMMEDIATE | RELATIVE) },
                    { 0x84, () => new Jmp(new NoOperands(), Condition.Z , IMMEDIATE | RELATIVE) },
                    { 0x85, () => new Jmp(new NoOperands(), Condition.NZ, IMMEDIATE | RELATIVE) },
                    { 0x86, () => new Jmp(new NoOperands(), Condition.NA, IMMEDIATE | RELATIVE) },
                    { 0x87, () => new Jmp(new NoOperands(), Condition.A , IMMEDIATE | RELATIVE) },
                    { 0x88, () => new Jmp(new NoOperands(), Condition.S , IMMEDIATE | RELATIVE) },
                    { 0x89, () => new Jmp(new NoOperands(), Condition.NS, IMMEDIATE | RELATIVE) },
                    { 0x8A, () => new Jmp(new NoOperands(), Condition.P , IMMEDIATE | RELATIVE) },
                    { 0x8B, () => new Jmp(new NoOperands(), Condition.NP, IMMEDIATE | RELATIVE) },
                    { 0x8C, () => new Jmp(new NoOperands(), Condition.L , IMMEDIATE | RELATIVE) },
                    { 0x8D, () => new Jmp(new NoOperands(), Condition.GE, IMMEDIATE | RELATIVE) },
                    { 0x8E, () => new Jmp(new NoOperands(), Condition.LE, IMMEDIATE | RELATIVE) },
                    { 0x8F, () => new Jmp(new NoOperands(), Condition.G , IMMEDIATE | RELATIVE) },
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

                    { 0xB6, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, sourceSize:RegisterCapacity.GP_BYTE)},
                    { 0xB7, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, sourceSize:RegisterCapacity.GP_WORD)},

                    { 0xBE, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, sourceSize:RegisterCapacity.GP_BYTE)},
                    { 0xBF, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, sourceSize:RegisterCapacity.GP_WORD)},
                }
            }
        };
        // format <byte size of opcode, <determining byte of opcode, <offset of extension, method to return opcode>>>
        private delegate Opcode ExtendedOpcodeCaller(ModRM input); // the opcodeinput of an extended opcode can be determined from a modrm(aka pretty much the same every time)
        private static readonly Dictionary<byte, Dictionary<byte, Dictionary<int, ExtendedOpcodeCaller>>> ExtendedOpcodeTable = new Dictionary<byte, Dictionary<byte, Dictionary<int, ExtendedOpcodeCaller>>>()
        {
            { 1, new Dictionary<byte, Dictionary<int, ExtendedOpcodeCaller>>()
            {
                { 0x80, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Add(InputModRM, IMMEDIATE | BYTEMODE, UseCarry:false) },
                    { 1, (InputModRM) => new Or (InputModRM, IMMEDIATE | BYTEMODE) },
                    { 2, (InputModRM) => new Add(InputModRM, IMMEDIATE | BYTEMODE, UseCarry:true) },
                    { 3, (InputModRM) => new Sub(InputModRM, IMMEDIATE | BYTEMODE, UseBorrow: true) },
                    { 4, (InputModRM) => new And(InputModRM, IMMEDIATE | BYTEMODE)},
                    { 5, (InputModRM) => new Sub(InputModRM, IMMEDIATE | BYTEMODE, UseBorrow: false) } ,
                    { 6, (InputModRM) => new Xor(InputModRM, IMMEDIATE | BYTEMODE) }, 
                    { 7, (InputModRM) => new Cmp(InputModRM, IMMEDIATE | BYTEMODE) },
                }
                },
                { 0x81, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Add(InputModRM,IMMEDIATE, UseCarry:false) },
                    { 1, (InputModRM) => new Or (InputModRM,IMMEDIATE) },
                    { 2, (InputModRM) => new Add(InputModRM,IMMEDIATE, UseCarry:true) },
                    { 3, (InputModRM) => new Sub(InputModRM,IMMEDIATE, UseBorrow: true) },
                    { 4, (InputModRM) => new And(InputModRM,IMMEDIATE)},
                    { 5, (InputModRM) => new Sub(InputModRM,IMMEDIATE, UseBorrow: false) } ,
                    { 6, (InputModRM) => new Xor(InputModRM,IMMEDIATE) },
                    { 7, (InputModRM) => new Cmp(InputModRM,IMMEDIATE) },
                }
                },
                { 0x83, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Add(InputModRM,IMMEDIATE | SXTBYTE, UseCarry:false) },
                    { 1, (InputModRM) => new Or (InputModRM,IMMEDIATE | SXTBYTE) },
                    { 2, (InputModRM) => new Add(InputModRM,IMMEDIATE | SXTBYTE, UseCarry:true) },
                    { 3, (InputModRM) => new Sub(InputModRM,IMMEDIATE | SXTBYTE, UseBorrow: true) },
                    { 4, (InputModRM) => new And(InputModRM,IMMEDIATE | SXTBYTE)},
                    { 5, (InputModRM) => new Sub(InputModRM,IMMEDIATE | SXTBYTE, UseBorrow: false) } ,
                    { 6, (InputModRM) => new Xor(InputModRM,IMMEDIATE | SXTBYTE) },
                    { 7, (InputModRM) => new Cmp(InputModRM,IMMEDIATE | SXTBYTE) },
                }
                },
                { 0xC0, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, BYTEMODE | IMMEDIATE | SXTBYTE) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, BYTEMODE | IMMEDIATE | SXTBYTE) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, BYTEMODE | IMMEDIATE | SXTBYTE) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, BYTEMODE | IMMEDIATE | SXTBYTE) },
                    { 4, (InputModRM) => new Shl(InputModRM, BYTEMODE | IMMEDIATE | SXTBYTE) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, BYTEMODE | IMMEDIATE | SXTBYTE) },

                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, BYTEMODE | IMMEDIATE | SXTBYTE) },
                }
                },
                { 0xC1, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, IMMEDIATE | SXTBYTE) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, IMMEDIATE | SXTBYTE) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, IMMEDIATE | SXTBYTE) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, IMMEDIATE | SXTBYTE) },
                    { 4, (InputModRM) => new Shl(InputModRM, IMMEDIATE | SXTBYTE) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, IMMEDIATE | SXTBYTE) },

                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, IMMEDIATE | SXTBYTE) },
                }
                },
                { 0xD0, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, BYTEMODE | EXTRA_1) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, BYTEMODE | EXTRA_1) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, BYTEMODE | EXTRA_1) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, BYTEMODE | EXTRA_1) },
                    { 4, (InputModRM) => new Shl(InputModRM, BYTEMODE | EXTRA_1) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, BYTEMODE | EXTRA_1) },

                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, BYTEMODE | EXTRA_1) },
                }
                },
                { 0xD1, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, EXTRA_1) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, EXTRA_1) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, EXTRA_1) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, EXTRA_1) },
                    { 4, (InputModRM) => new Shl(InputModRM, EXTRA_1) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, EXTRA_1) },

                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, EXTRA_1) },
                }
                },
                { 0xD2, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, BYTEMODE |  EXTRA_CL) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, BYTEMODE | EXTRA_CL) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, BYTEMODE |  EXTRA_CL) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, BYTEMODE | EXTRA_CL) },
                    { 4, (InputModRM) => new Shl(InputModRM, BYTEMODE |  EXTRA_CL) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, BYTEMODE |EXTRA_CL ) },
                                                                                        
                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, BYTEMODE | EXTRA_CL  ) },
                }
                },
                { 0xD3, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Rxl(InputModRM, useCarry:false, EXTRA_CL) },
                    { 1, (InputModRM) => new Rxr(InputModRM, useCarry:false, EXTRA_CL) },
                    { 2, (InputModRM) => new Rxl(InputModRM, useCarry:true, EXTRA_CL) },
                    { 3, (InputModRM) => new Rxr(InputModRM, useCarry:true, EXTRA_CL) },
                    { 4, (InputModRM) => new Shl(InputModRM, EXTRA_CL) },
                    { 5, (InputModRM) => new Sxr(InputModRM, arithmetic:false, EXTRA_CL) },

                    { 7, (InputModRM) => new Sxr(InputModRM, arithmetic:true, EXTRA_CL) },
                }
                },
                { 0xF6, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Test(InputModRM, BYTEMODE | IMMEDIATE) },
                    { 4, (InputModRM) => new Mul(InputModRM, BYTEMODE) },
                    { 5, (InputModRM) => new Mul(InputModRM, BYTEMODE | SIGNED) },
                    { 6, (InputModRM) => new Div(InputModRM, BYTEMODE) },
                    { 7, (InputModRM) => new Div(InputModRM, BYTEMODE | SIGNED) }
                }
                },
                { 0xF7, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 0, (InputModRM) => new Test(InputModRM, IMMEDIATE) },
                    { 4, (InputModRM) => new Mul(InputModRM) },
                    { 5, (InputModRM) => new Mul(InputModRM, SIGNED) },
                    { 6, (InputModRM) => new Div(InputModRM) },
                    { 7, (InputModRM) => new Div(InputModRM, SIGNED) }
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
            }
            }
        };
        //byte = opcode, Dict<byte = displacement(increment doesnt matter), action = thing to do>
        // opcodes with one operand e.g inc eax (increment eax) don't use the REG bits in modRM, so the regbits can be used to make extra instructions instead
        // so we can have one "opcode byte" that has meanings depending on the REG bits
        // reg bit starts at the 8 dec val position in a string of bits e.g "01[100]111" [REG] so each offset is -8 each time
        private static Opcode DecodeExtension(byte opcode, byte bytesInOpcode)
        {
            ModRM InputModRM = new ModRM(FetchNext(), ModRMSettings.EXTENDED);
            return ExtendedOpcodeTable[bytesInOpcode][opcode][(int)InputModRM.Source](InputModRM);
        }
    }
}
