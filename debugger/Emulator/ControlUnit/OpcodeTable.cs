using System.Collections.Generic;
using debugger.Emulator.Opcodes;
using debugger.Emulator.DecodedTypes;

using static debugger.Emulator.Opcodes.OpcodeSettings;
namespace debugger.Emulator
{
    public static partial class ControlUnit
    {        
        private delegate Opcode OpcodeCaller();
        // format <byte size of opcode, <determining byte of opcode, method to return opcode>>
        private static readonly Dictionary<byte, Dictionary<byte, OpcodeCaller>> OpcodeTable = new Dictionary<byte, Dictionary<byte, OpcodeCaller>>()
        {
            {1, new Dictionary<byte, OpcodeCaller>()
                {
                  { 0x00, () => new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:false)},
                  { 0x01, () => new Add(new ModRM(FetchNext()), UseCarry:false) },
                  { 0x02, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:false)},
                  { 0x03, () => new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:false)},
                  { 0x04, () => new Add(new ImplicitRegister(ByteCode.A), BYTEMODE | IMMEDIATE, UseCarry:false) },
                  { 0x05, () => new Add(new ImplicitRegister(ByteCode.A), IMMEDIATE, UseCarry:false)},

                  { 0x08, () => new  Or(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x09, () => new  Or(new ModRM(FetchNext())) },
                  { 0x0A, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x0B, () => new  Or(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x0C, () => new  Or(new ImplicitRegister(ByteCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x0D, () => new  Or(new ImplicitRegister(ByteCode.A), IMMEDIATE)},
                  { 0x10, () =>  new Add(new ModRM(FetchNext()), BYTEMODE,UseCarry:true)},
                  { 0x11, () =>  new Add(new ModRM(FetchNext()), UseCarry:true) },
                  { 0x12, () =>  new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseCarry:true)},
                  { 0x13, () =>  new Add(new ModRM(FetchNext(), ModRMSettings.SWAP), UseCarry:true)},
                  { 0x14, () =>  new Add(new ImplicitRegister(ByteCode.A), BYTEMODE | IMMEDIATE, UseCarry:true) },
                  { 0x15, () =>  new Add(new ImplicitRegister(ByteCode.A), IMMEDIATE , UseCarry:true)},

                  { 0x18, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:true)},
                  { 0x19, () => new Sub(new ModRM(FetchNext()), UseBorrow:true) },
                  { 0x1A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:true)},
                  { 0x1B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:true)},
                  { 0x1C, () => new Sub(new ImplicitRegister(ByteCode.A), BYTEMODE | IMMEDIATE, UseBorrow:true) },
                  { 0x1D, () => new Sub(new ImplicitRegister(ByteCode.A), IMMEDIATE, UseBorrow:true)},

                  { 0x20, () => new And(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x21, () => new And(new ModRM(FetchNext())) },
                  { 0x22, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x23, () => new And(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x24, () => new And(new ImplicitRegister(ByteCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x25, () => new And(new ImplicitRegister(ByteCode.A), IMMEDIATE)},

                  { 0x28, () => new Sub(new ModRM(FetchNext()), BYTEMODE,UseBorrow:false)},
                  { 0x29, () => new Sub(new ModRM(FetchNext()), UseBorrow:false) },
                  { 0x2A, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE, UseBorrow:false)},
                  { 0x2B, () => new Sub(new ModRM(FetchNext(), ModRMSettings.SWAP), UseBorrow:false)},
                  { 0x2C, () => new Sub(new ImplicitRegister(ByteCode.A), BYTEMODE | IMMEDIATE, UseBorrow:false) },
                  { 0x2D, () => new Sub(new ImplicitRegister(ByteCode.A), IMMEDIATE, UseBorrow:false)},

                  { 0x30, () => new Xor(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x31, () => new Xor(new ModRM(FetchNext())) },
                  { 0x32, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x33, () => new Xor(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x34, () => new Xor(new ImplicitRegister(ByteCode.A), IMMEDIATE | BYTEMODE) },
                  { 0x35, () => new Xor(new ImplicitRegister(ByteCode.A), IMMEDIATE)},

                  { 0x38, () => new Cmp(new ModRM(FetchNext()), BYTEMODE)},
                  { 0x39, () => new Cmp(new ModRM(FetchNext())) },
                  { 0x3A, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE)},
                  { 0x3B, () => new Cmp(new ModRM(FetchNext(), ModRMSettings.SWAP)) },
                  { 0x3C, () => new Cmp(new ImplicitRegister(ByteCode.A), BYTEMODE | IMMEDIATE) },
                  { 0x3D, () => new Cmp(new ImplicitRegister(ByteCode.A), IMMEDIATE)},

                  { 0x50, () => new Push(new ImplicitRegister(ByteCode.A ), IMMEDIATE) },
                  { 0x51, () => new Push(new ImplicitRegister(ByteCode.C ), IMMEDIATE) },
                  { 0x52, () => new Push(new ImplicitRegister(ByteCode.D ), IMMEDIATE) },
                  { 0x53, () => new Push(new ImplicitRegister(ByteCode.B ), IMMEDIATE) },
                  { 0x54, () => new Push(new ImplicitRegister(ByteCode.AH), IMMEDIATE) },
                  { 0x55, () => new Push(new ImplicitRegister(ByteCode.CH), IMMEDIATE) },
                  { 0x56, () => new Push(new ImplicitRegister(ByteCode.DH), IMMEDIATE) },
                  { 0x57, () => new Push(new ImplicitRegister(ByteCode.BH), IMMEDIATE) },
                  { 0x58, () => new Pop (new ImplicitRegister(ByteCode.A ), IMMEDIATE) },
                  { 0x59, () => new Pop (new ImplicitRegister(ByteCode.C ), IMMEDIATE) },
                  { 0x5A, () => new Pop (new ImplicitRegister(ByteCode.D ), IMMEDIATE) },
                  { 0x5B, () => new Pop (new ImplicitRegister(ByteCode.B ), IMMEDIATE) },
                  { 0x5C, () => new Pop (new ImplicitRegister(ByteCode.AH), IMMEDIATE) },
                  { 0x5D, () => new Pop (new ImplicitRegister(ByteCode.CH), IMMEDIATE) },
                  { 0x5E, () => new Pop (new ImplicitRegister(ByteCode.DH), IMMEDIATE) },
                  { 0x5F, () => new Pop (new ImplicitRegister(ByteCode.BH), IMMEDIATE) },

                  { 0x63, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP), "MOVSXD", signExtend:true, RegisterCapacity.DWORD) },
                  //67 prefix
                  { 0x68, () => new Push(new Immediate(RegisterCapacity.DWORD)) }, // its always 32, weird
                  { 0x69, () => new  Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED | IMMEDIATE) },
                  { 0x6A, () => new Push(new Immediate(RegisterCapacity.QWORD)) },
                  { 0x6B, () => new  Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED | IMMEDIATE | SXBYTE) },

                  { 0x70, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.O) },
                  { 0x71, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NO) },
                  { 0x72, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.C) },
                  { 0x73, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NC) },
                  { 0x74, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.Z) },
                  { 0x75, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NZ) },
                  { 0x76, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NA) },
                  { 0x77, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.A) },
                  { 0x78, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.S) },
                  { 0x79, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NS) },
                  { 0x7A, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.P) },
                  { 0x7B, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.NP) },
                  { 0x7C, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.L) },
                  { 0x7D, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.GE) },
                  { 0x7E, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.LE) },
                  { 0x7F, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true), Condition.G) },

                  { 0x80, () => DecodeExtension(0x80, 1) },
                  { 0x81, () => DecodeExtension(0x81, 1) },
                  { 0x83, () => DecodeExtension(0x83, 1) },
                  { 0x88, () => new Mov(new ModRM(FetchNext()), BYTEMODE) },
                  { 0x89, () => new Mov(new ModRM(FetchNext())) },
                  { 0x8A, () => new Mov(new ModRM(FetchNext(), ModRMSettings.SWAP), BYTEMODE) },
                  { 0x8B, () => new Mov(new ModRM(FetchNext(), ModRMSettings.SWAP))},
                  { 0x8F, () => new Pop(new ModRM(FetchNext())) },
                  { 0x90, () => new Nop() },

                  { 0xB0, () => new Mov(new ImplicitRegister(ByteCode.A ), BYTEMODE | IMMEDIATE) },
                  { 0xB1, () => new Mov(new ImplicitRegister(ByteCode.C ), BYTEMODE | IMMEDIATE) },
                  { 0xB2, () => new Mov(new ImplicitRegister(ByteCode.D ), BYTEMODE | IMMEDIATE) },
                  { 0xB3, () => new Mov(new ImplicitRegister(ByteCode.B ), BYTEMODE | IMMEDIATE) },
                  { 0xB4, () => new Mov(new ImplicitRegister(ByteCode.AH), BYTEMODE | IMMEDIATE) },
                  { 0xB5, () => new Mov(new ImplicitRegister(ByteCode.CH), BYTEMODE | IMMEDIATE) },
                  { 0xB6, () => new Mov(new ImplicitRegister(ByteCode.DH), BYTEMODE | IMMEDIATE) },
                  { 0xB7, () => new Mov(new ImplicitRegister(ByteCode.BH), BYTEMODE | IMMEDIATE) },
                  { 0xB8, () => new Mov(new ImplicitRegister(ByteCode.A ), IMMEDIATE | ALLOWIMM64) },
                  { 0xB9, () => new Mov(new ImplicitRegister(ByteCode.C ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBA, () => new Mov(new ImplicitRegister(ByteCode.D ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBB, () => new Mov(new ImplicitRegister(ByteCode.B ), IMMEDIATE | ALLOWIMM64) },
                  { 0xBC, () => new Mov(new ImplicitRegister(ByteCode.AH), IMMEDIATE | ALLOWIMM64) },
                  { 0xBD, () => new Mov(new ImplicitRegister(ByteCode.CH), IMMEDIATE | ALLOWIMM64) },
                  { 0xBE, () => new Mov(new ImplicitRegister(ByteCode.DH), IMMEDIATE | ALLOWIMM64) },
                  { 0xBF, () => new Mov(new ImplicitRegister(ByteCode.BH), IMMEDIATE | ALLOWIMM64) },

                  { 0xC6, () => new Mov(new ModRM(FetchNext(), ModRMSettings.EXTENDED), IMMEDIATE | BYTEMODE)},
                  { 0xC7, () => new Movx(new ModRM(FetchNext(), ModRMSettings.EXTENDED), "MOV", signExtend:true, RegisterCapacity.DWORD, IMMEDIATE)},

                  { 0xE3, () => new Jmp(new Immediate(RegisterCapacity.BYTE), Condition.RCXZ) },

                  { 0xEB, () => new Jmp(new Immediate(RegisterCapacity.BYTE, ripRel:true)) },
                  { 0xE9, () => new Jmp(new Immediate(RegisterCapacity.DWORD, ripRel:true)) },

                  { 0xF6, () => DecodeExtension(0xF6, 1) },
                  { 0xF7, () => DecodeExtension(0xF7, 1) },
                  { 0xFF, () => DecodeExtension(0xFF, 1) }
                }
            },
            {2, new Dictionary<byte, OpcodeCaller>()
                {
                    { 0x80, () => new Jmp(new Immediate(ripRel:true), Condition.O) },
                    { 0x81, () => new Jmp(new Immediate(ripRel:true), Condition.NO) },
                    { 0x82, () => new Jmp(new Immediate(ripRel:true), Condition.C) },
                    { 0x83, () => new Jmp(new Immediate(ripRel:true), Condition.NC) },
                    { 0x84, () => new Jmp(new Immediate(ripRel:true), Condition.Z) },
                    { 0x85, () => new Jmp(new Immediate(ripRel:true), Condition.NZ) },
                    { 0x86, () => new Jmp(new Immediate(ripRel:true), Condition.NA) },
                    { 0x87, () => new Jmp(new Immediate(ripRel:true), Condition.A) },
                    { 0x88, () => new Jmp(new Immediate(ripRel:true), Condition.S) },
                    { 0x89, () => new Jmp(new Immediate(ripRel:true), Condition.NS) },
                    { 0x8A, () => new Jmp(new Immediate(ripRel:true), Condition.P) },
                    { 0x8B, () => new Jmp(new Immediate(ripRel:true), Condition.NP) },
                    { 0x8C, () => new Jmp(new Immediate(ripRel:true), Condition.L) },
                    { 0x8D, () => new Jmp(new Immediate(ripRel:true), Condition.GE) },
                    { 0x8E, () => new Jmp(new Immediate(ripRel:true), Condition.LE) },
                    { 0x8F, () => new Jmp(new Immediate(ripRel:true), Condition.G) },

                    { 0xAF, () => new Mul(new ModRM(FetchNext(), ModRMSettings.SWAP), SIGNED)},

                    { 0xB6, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, sourceSize:RegisterCapacity.BYTE)},
                    { 0xB7, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVZX", signExtend:false, sourceSize:RegisterCapacity.WORD)},

                    { 0xBE, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, sourceSize:RegisterCapacity.BYTE)},
                    { 0xBF, () => new Movx(new ModRM(FetchNext(), ModRMSettings.SWAP),"MOVSX", signExtend:true, sourceSize:RegisterCapacity.WORD)},
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
                    { 0, (InputModRM) => new Add(InputModRM,IMMEDIATE | SXBYTE, UseCarry:false) },
                    { 1, (InputModRM) => new Or (InputModRM,IMMEDIATE | SXBYTE) },
                    { 2, (InputModRM) => new Add(InputModRM,IMMEDIATE | SXBYTE, UseCarry:true) },
                    { 3, (InputModRM) => new Sub(InputModRM,IMMEDIATE | SXBYTE, UseBorrow: true) },
                    { 4, (InputModRM) => new And(InputModRM,IMMEDIATE | SXBYTE)},
                    { 5, (InputModRM) => new Sub(InputModRM,IMMEDIATE | SXBYTE, UseBorrow: false) } ,
                    { 6, (InputModRM) => new Xor(InputModRM,IMMEDIATE | SXBYTE) },
                    { 7, (InputModRM) => new Cmp(InputModRM,IMMEDIATE | SXBYTE) },
                }
                },
                { 0xF6, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 4, (InputModRM) => new Mul(InputModRM, BYTEMODE) },
                    { 5, (InputModRM) => new Mul(InputModRM, BYTEMODE | SIGNED) },
                    { 6, (InputModRM) => new Div(InputModRM, BYTEMODE) },
                    { 7, (InputModRM) => new Div(InputModRM, BYTEMODE | SIGNED) }
                }
                },
                { 0xF7, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    { 4, (InputModRM) => new Mul(InputModRM) },
                    { 5, (InputModRM) => new Mul(InputModRM, SIGNED) },
                    { 6, (InputModRM) => new Div(InputModRM) },
                    { 7, (InputModRM) => new Div(InputModRM, SIGNED) }
                }
                },
                { 0xFE, new Dictionary<int, ExtendedOpcodeCaller>
                {
                     { 0, (InputModRM) => new Inc(InputModRM) },
                }
                },
                { 0xFF, new Dictionary<int, ExtendedOpcodeCaller>
                {
                    // { 0, () => new Inc(MultiDefOutput) },
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
