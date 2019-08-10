using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Opcodes;
using static debugger.Util.OpcodeUtil;
using static debugger.Util;
using static debugger.ControlUnit;
using static debugger.Primitives;
using static debugger.ControlUnit.FlagSet;
namespace debugger
{
    public static class OpcodeLookup
    {
        public static Dictionary<PrefixByte, string> PrefixTable = new Dictionary<PrefixByte, string>();
        public static Dictionary<byte, Dictionary<byte, Func<Opcode>>> OpcodeTable = new Dictionary<byte, Dictionary<byte, Func<Opcode>>>();

        public static ModRM SubopcodeOutput;
        //byte = opcode, Dict<byte = displacement(increment doesnt matter), action = thing to do>
        // opcodes with one operand e.g inc eax (increment eax) don't use the REG bits in modRM, so the regbits can be used to make extra instructions instead
        // so we can have one "opcode byte" that has meanings depending on the REG bits
        // reg bit starts at the 8 dec val position in a string of bits e.g "01[100]111" [REG] so each offset is -8 each time
        private static Dictionary<byte, Dictionary<int, Func<Opcode>>> SubopcodeDict = new Dictionary<byte, Dictionary<int, Func<Opcode>>>();
        public static Opcode Decode(byte bOpcode)
        {
            SubopcodeOutput = ModRMDecode();
            ulong Offset = SubopcodeOutput.Reg; //swap because
            SubopcodeOutput.Reg = SubopcodeOutput.DestPtr;
            return SubopcodeDict[bOpcode][(byte)Offset].Invoke();
        }
        private static void AddPrefixes()
        {
            PrefixTable.Add(PrefixByte.REXW, "REXW");
            PrefixTable.Add(PrefixByte.ADDR32, "ADDR32");
        }

        private static void AddSubOpcodes()
        {
            //0x80
            Dictionary<int, Func<Opcode>> _80 = new Dictionary<int, Func<Opcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true}, UseBorrow:true)},
             //   { 4, () => new AndImm(MultiDefOutput) { Is8Bit=true } },
                { 5,() => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true}, UseBorrow:false)},
             //   { 6, () => new XorImm(MultiDefOutput) { Is8Bit=true } },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=SubopcodeOutput, Is8Bit=true, IsImmediate=true})},
            };       
            SubopcodeDict.Add(0x80, _80);
            //or
            //adc
            //sbb
            //and
            //sub
            //xor
            //cmp
            //0x81
            Dictionary<int, Func<Opcode>> _81 = new Dictionary<int, Func<Opcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true}, UseBorrow:true)},
            //   { 4,  () => new AndImm(MultiDefOutput) },
                { 5,  () => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true}, UseBorrow:false)},
            //   { 6,  () => new XorImm(MultiDefOutput) },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true})},
            };
            SubopcodeDict.Add(0x81, _81);

            //0x82 no longer x86_64

            //0x83
            Dictionary<int, Func<Opcode>> _83 = new Dictionary<int, Func<Opcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true}, UseBorrow:true)},
            //   { 4, () => new AndImm(MultiDefOutput) { IsSwap=true } },
                { 5, () => new Sub(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true}, UseBorrow:false)},
            //   { 6, () => new XorImm(MultiDefOutput, SignExtend:true) },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=SubopcodeOutput, IsImmediate=true, IsSignExtendedByte=true })},
            };
            SubopcodeDict.Add(0x83, _83);

            //0xf6
            Dictionary<int, Func<Opcode>> _F6 = new Dictionary<int, Func<Opcode>>
            {
                { 4, () => new Mul(new OpcodeInput{DecodedModRM = SubopcodeOutput.ChangeSource((ulong)ByteCode.A), Is8Bit=true},oprandCount:1)},
                { 5, () => new Mul(new OpcodeInput{DecodedModRM = SubopcodeOutput.ChangeSource((ulong)ByteCode.A), Is8Bit=true, IsSigned=true}, oprandCount:1)},
                { 6, () => new Div(new OpcodeInput{DecodedModRM = SubopcodeOutput, Is8Bit=true})},
                { 7, () => new Div(new OpcodeInput{DecodedModRM = SubopcodeOutput, Is8Bit=true, IsSigned=true})}
            };
            SubopcodeDict.Add(0xF6, _F6);

            //0xf7
            Dictionary<int, Func<Opcode>> _F7 = new Dictionary<int, Func<Opcode>>
            {
                { 4, () => new Mul(new OpcodeInput{DecodedModRM = SubopcodeOutput.ChangeSource((ulong)ByteCode.A)}, oprandCount:1) },
                { 5, () => new Mul(new OpcodeInput{DecodedModRM = SubopcodeOutput.ChangeSource((ulong)ByteCode.A), IsSigned=true}, oprandCount:1)},
                { 6, () => new Div(new OpcodeInput{DecodedModRM = SubopcodeOutput}) },
                { 7, () => new Div(new OpcodeInput{DecodedModRM = SubopcodeOutput, IsSigned=true})}
            };

            SubopcodeDict.Add(0xF7, _F7);

            //0xfe
            Dictionary<int, Func<Opcode>> _FE = new Dictionary<int, Func<Opcode>>
            {
              //  { 0, () => new Inc(MultiDefOutput) { Is8Bit=true } },
            };
            SubopcodeDict.Add(0xFE, _FE);
            //0xff
            Dictionary<int, Func<Opcode>> _FF = new Dictionary<int, Func<Opcode>>
            {
               // { 0, () => new Inc(MultiDefOutput) },
                { 4, () => new Jmp(Relative:false, Bytes:8) },
                { 6, () => new Push(new OpcodeInput{DecodedModRM = SubopcodeOutput}) }
            };
            SubopcodeDict.Add(0xFF, _FF);
        }
        static OpcodeLookup()
        {
            AddPrefixes();
            AddSubOpcodes();
            OpcodeTable.Add(1, new Dictionary<byte, Func<Opcode>>()
            {
                { 0x00, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true}, UseCarry:false)},
                { 0x01, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode() }, UseCarry:false) },
                { 0x02, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true, Is8Bit=true}, UseCarry:false)},
                { 0x03, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true}, UseCarry:false)},
                { 0x04, () => new Add(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true, Is8Bit=true}, UseCarry:false) },
                { 0x05, () => new Add(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }, UseCarry:false)},

                { 0x08, () => new  Or(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true })},
                { 0x09, () => new  Or(new OpcodeInput{ DecodedModRM = ModRMDecode() }) },
                { 0x0A, () => new  Or(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true, IsSwap=true })},
                { 0x0B, () => new  Or(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true })},
                { 0x0C, () => new  Or(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), Is8Bit=true, IsImmediate=true })},
                { 0x0D, () => new  Or(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }) },
                { 0x10, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true}, UseCarry:true)},
                { 0x11, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode() }, UseCarry:true) },
                { 0x12, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true, Is8Bit=true}, UseCarry:true)},
                { 0x13, () => new Add(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true}, UseCarry:true)},
                { 0x14, () => new Add(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true, Is8Bit=true}, UseCarry:true) },
                { 0x15, () => new Add(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }, UseCarry:true)},

                { 0x18, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true}, UseBorrow:true)},
                { 0x19, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode() }, UseBorrow:true) },
                { 0x1A, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true, Is8Bit=true}, UseBorrow:true)},
                { 0x1B, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true}, UseBorrow:true)},
                { 0x1C, () => new Sub(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true, Is8Bit=true}, UseBorrow:true) },
                { 0x1D, () => new Sub(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }, UseBorrow:true)},

             //   { 0x20, () => new And() { Is8Bit=true } },
             //   { 0x21, () => new And() },
             //   { 0x22, () => new And() { Is8Bit=true, IsSwap=true} },
             //   { 0x23, () => new And() { IsSwap=true } },
             //   { 0x24, () => new AndImm(Input:FromDest(ByteCode.A)) { Is8Bit=true } },
             //   { 0x25, () => new AndImm(Input:FromDest(ByteCode.A)) },

                { 0x28, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true}, UseBorrow:false)},
                { 0x29, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode() }, UseBorrow:false) },
                { 0x2A, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true, Is8Bit=true}, UseBorrow:false)},
                { 0x2B, () => new Sub(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true}, UseBorrow:false)},
                { 0x2C, () => new Sub(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true, Is8Bit=true}, UseBorrow:false) },
                { 0x2D, () => new Sub(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }, UseBorrow:false)},

              //  { 0x30, () => new Xor() { Is8Bit=true } },
              //  { 0x31, () => new Xor() },
              //  { 0x32, () => new Xor() { Is8Bit=true, IsSwap=true} },
              //  { 0x33, () => new Xor() { IsSwap=true } },
              //  { 0x34, () => new XorImm(Input:FromDest(ByteCode.A)) { Is8Bit=true } },
              //  { 0x35, () => new XorImm(Input:FromDest(ByteCode.A)) },

                { 0x38, () => new Cmp(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true })},
                { 0x39, () => new Cmp(new OpcodeInput{ DecodedModRM = ModRMDecode() }) },
                { 0x3A, () => new Cmp(new OpcodeInput{ DecodedModRM = ModRMDecode(), Is8Bit=true, IsSwap=true })},
                { 0x3B, () => new Cmp(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsSwap=true })},
                { 0x3C, () => new Cmp(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), Is8Bit=true, IsImmediate=true })},
                { 0x3D, () => new Cmp(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A), IsImmediate=true }) },

                { 0x50, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A) }) },
                { 0x51, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.C) }) },
                { 0x52, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.D) }) },
                { 0x53, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.B) }) },
                { 0x54, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.AH)}) },
                { 0x55, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.CH)}) },
                { 0x56, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.DH)}) },
                { 0x57, () => new Push(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.BH)}) },
                { 0x58, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A) }) },
                { 0x59, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.C) }) },
                { 0x5A, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.D) }) },
                { 0x5B, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.B) }) },
                { 0x5C, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.AH)}) },
                { 0x5D, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.CH)}) },
                { 0x5E, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.DH)}) },
                { 0x5F, () => new Pop(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.BH)}) },

                { 0x63, () => new Movx(new OpcodeInput{DecodedModRM = ModRMDecode()}, "MOVSXD", signExtend:true) },
                //67 prefix
                { 0x68, () => new Push(new OpcodeInput{ IsImmediate=true}, PushSize:32) }, // its always 32, weird
                { 0x69, () => new  Mul(new OpcodeInput { DecodedModRM = ModRMDecode(), IsImmediate=true, IsSigned=true}, oprandCount:3) },
                { 0x6A, () => new Push(new OpcodeInput{ IsImmediate=true}, PushSize:8) },
                { 0x6B, () => new  Mul(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsImmediate=true, IsSigned=true, IsSignExtendedByte=true}, oprandCount:3)},

                { 0x70, () => new Jmp(Relative:true, Bytes:1, Opcode:"JO") },
                { 0x71, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNO") },
                { 0x72, () => new Jmp(Relative:true, Bytes:1, Opcode:"JC") },
                { 0x73, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNC") },
                { 0x74, () => new Jmp(Relative:true, Bytes:1, Opcode:"JZ") },
                { 0x75, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNZ") },
                { 0x76, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNA") },
                { 0x77, () => new Jmp(Relative:true, Bytes:1, Opcode:"JA") },
                { 0x78, () => new Jmp(Relative:true, Bytes:1, Opcode:"JS") },
                { 0x79, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNS") },
                { 0x7A, () => new Jmp(Relative:true, Bytes:1, Opcode:"JP") },
                { 0x7B, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNP") },
                { 0x7C, () => new Jmp(Relative:true, Bytes:1, Opcode:"JL") },
                { 0x7D, () => new Jmp(Relative:true, Bytes:1, Opcode:"JGE") },
                { 0x7E, () => new Jmp(Relative:true, Bytes:1, Opcode:"JLE") },
                { 0x7F, () => new Jmp(Relative:true, Bytes:1, Opcode:"JG") },

                { 0x80, () => Decode(0x80) },
                { 0x81, () => Decode(0x81) },
                { 0x83, () => Decode(0x83) },
                { 0x88, () => new Mov(new OpcodeInput{DecodedModRM=ModRMDecode(), Is8Bit=true }) },
                { 0x89, () => new Mov(new OpcodeInput{DecodedModRM=ModRMDecode()}) },
                { 0x8A, () => new Mov(new OpcodeInput{DecodedModRM=ModRMDecode(), Is8Bit=true, IsSwap=true }) },
                { 0x8B, () => new Mov(new OpcodeInput{DecodedModRM=ModRMDecode(), IsSwap=true })},
                { 0x8F, () => new Pop(new OpcodeInput{DecodedModRM=ModRMDecode()}) },
                { 0x90, () => new Nop() },

                { 0xB0, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A) , IsImmediate=true, Is8Bit=true})},
                { 0xB1, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.C) , IsImmediate=true, Is8Bit=true})},
                { 0xB2, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.D) , IsImmediate=true, Is8Bit=true})},
                { 0xB3, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.B) , IsImmediate=true, Is8Bit=true})},
                { 0xB4, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.AH), IsImmediate=true, Is8Bit=true})},
                { 0xB5, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.CH), IsImmediate=true, Is8Bit=true})},
                { 0xB6, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.DH), IsImmediate=true, Is8Bit=true})},
                { 0xB7, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.BH), IsImmediate=true, Is8Bit=true})},
                { 0xB8, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.A) , IsImmediate=true})},
                { 0xB9, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.C) , IsImmediate=true})},
                { 0xBA, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.D) , IsImmediate=true})},
                { 0xBB, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.B) , IsImmediate=true})},
                { 0xBC, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.SP), IsImmediate=true})},
                { 0xBD, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.BP), IsImmediate=true})},
                { 0xBE, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.SI), IsImmediate=true})},
                { 0xBF, () => new Mov(new OpcodeInput{ DecodedModRM = FromDest(ByteCode.DI), IsImmediate=true})},

                { 0xC6, () => new Mov(new OpcodeInput{ DecodedModRM=ModRMDecode(), IsImmediate=true, Is8Bit=true})},
                { 0xC7, () => new Movx(new OpcodeInput{ DecodedModRM=ModRMDecode(), IsImmediate=true}, "MOV", signExtend:true)},

                { 0xE3, () => new Jmp(Relative:true, Bytes:1, Opcode:"JRCXZ") },

                { 0xEB, () => new Jmp(Relative:true, Bytes:1) },
                { 0xE9, () => new Jmp(Relative:true, Bytes:4) },

                { 0xF6, () => Decode(0xF6) },
                { 0xF7, () => Decode(0xF7) },
                { 0xFF, () => Decode(0xFF) }    
            });
            OpcodeTable.Add(2, new Dictionary<byte, Func<Opcode>>()
            {
                { 0x80, () => new Jmp(Relative:true, Bytes:4, Opcode:"JO") },
                { 0x81, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNO") },
                { 0x82, () => new Jmp(Relative:true, Bytes:4, Opcode:"JC") },
                { 0x83, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNC") },
                { 0x84, () => new Jmp(Relative:true, Bytes:4, Opcode:"JZ") },
                { 0x85, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNZ") },
                { 0x86, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNA") },
                { 0x87, () => new Jmp(Relative:true, Bytes:4, Opcode:"JA") },
                { 0x88, () => new Jmp(Relative:true, Bytes:4, Opcode:"JS") },
                { 0x89, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNS") },
                { 0x8A, () => new Jmp(Relative:true, Bytes:4, Opcode:"JP") },
                { 0x8B, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNP") },
                { 0x8C, () => new Jmp(Relative:true, Bytes:4, Opcode:"JL") },
                { 0x8D, () => new Jmp(Relative:true, Bytes:4, Opcode:"JGE") },
                { 0x8E, () => new Jmp(Relative:true, Bytes:4, Opcode:"JLE") },
                { 0x8F, () => new Jmp(Relative:true, Bytes:4, Opcode:"JG") },

                { 0xAF, () => new Mul(new OpcodeInput { DecodedModRM=ModRMDecode(), IsSigned=true}, oprandCount:2)},

                { 0xB6, () => new Movx(new OpcodeInput { DecodedModRM=ModRMDecode(), Is8Bit=true},"MOVZX", signExtend:false)},
                { 0xB7, () => new Movx(new OpcodeInput { DecodedModRM=ModRMDecode(), Is8Bit=true},"MOVZX", signExtend:false)},

                { 0xBE, () => new Movx(new OpcodeInput { DecodedModRM=ModRMDecode(), Is8Bit=true},"MOVSX", signExtend:true)},
                { 0xBF, () => new Movx(new OpcodeInput { DecodedModRM=ModRMDecode()},"MOVSX", signExtend:true)},
            });
        }
    }
    public class Opcodes
    {
        public abstract class Opcode
        {
            protected internal byte[] ImmediateBuffer;

            internal readonly string Mnemonic;
            internal OpcodeInput StoredInput;
            //shortcuts, easier to read          
            public Opcode(string OpcodeMnemonic, RegisterCapacity OpcodeCapacity)
            {
                Mnemonic = OpcodeMnemonic;
                CurrentCapacity = OpcodeCapacity;
            }
            public abstract void Execute();
            public virtual string[] Disassemble()
            {
                (string Dest, string Source) = Disassembly.DisassembleModRM(StoredInput, CurrentCapacity);
                Array.Resize(ref ImmediateBuffer, 8);
                return new string[] 
                {
                  Mnemonic,
                  Dest,
                  (StoredInput.IsImmediate) ? $"0x{BitConverter.ToUInt64(ImmediateBuffer,0).ToString("X")}" : Source
                };
            }
        }
        public class Nop : Opcode {
            public Nop() : base("NOP", RegisterCapacity.BYTE) { }
            public override void Execute() { }
            public override string[] Disassemble() => new string[] { Mnemonic };            
        }
        public class Mov : Opcode
        {
            readonly byte[] SourceBytes;
            public Mov(OpcodeInput Input) : base("MOV", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {                
                StoredInput = Input;
                (_, SourceBytes) = FetchDynamic(StoredInput, allowImm64:CurrentCapacity==RegisterCapacity.QWORD);
                ImmediateBuffer = SourceBytes; //set this anyway, use it if we want
            } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {
                SetDynamic(StoredInput, SourceBytes); // immediate should never swap 
            }      
        }
        public class Movx : Opcode
        {
            readonly byte[] SourceBytes;
            readonly byte[] Result;
            public Movx(OpcodeInput input, string mnemonic, bool signExtend) : base(mnemonic, GetRegCap())
            {
                input.IsSwap = true;
                StoredInput = input;//c7 mov fits in nicely here 
                SourceBytes = (input.IsImmediate) ? FetchNext(4) : FetchDynamic(input, (RegisterCapacity)(((int)CurrentCapacity)/2)).Item2;
                ImmediateBuffer = SourceBytes;
                Result = (signExtend) ? Bitwise.SignExtend(SourceBytes, (byte)CurrentCapacity) : Bitwise.ZeroExtend(SourceBytes, (byte)CurrentCapacity);
            }
            public override void Execute()
            {//movx only does rm to reg, not memory. also is always swap
                SetRegister((ByteCode)StoredInput.DecodedModRM.Reg, Result);
            }
            public override string[] Disassemble()
            {
                string[] Base = base.Disassemble();
                (_, Base[2]) = Disassembly.DisassembleModRM(StoredInput, StoredInput.Is8Bit ? RegisterCapacity.BYTE : RegisterCapacity.WORD);
                return Base;
            }
        }
        public class Add : Opcode // 01 32bit, 00 8bit,
        { // imm // 05=EAX, 81=any  04=al, 80=8bit
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            readonly byte[] SourceBytes;
            readonly byte[] DestBytes;
            readonly byte[] Result;
            readonly FlagSet ResultFlags;
            public Add(OpcodeInput Input, bool UseCarry) : base( (UseCarry) ? "ADC" : "ADD", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {
                StoredInput = Input;
                (DestBytes, SourceBytes) = FetchDynamic(StoredInput);
                ImmediateBuffer = SourceBytes;

                ResultFlags = Bitwise.Add(DestBytes, SourceBytes, (int)CurrentCapacity, out Result, (Flags.Carry == FlagState.On && UseCarry));
            }
            public override void Execute()
            {
                SetFlags(ResultFlags);
                SetDynamic(StoredInput, Result);
            }
        }
        public class Or : Opcode
        {
            readonly byte[] SourceBytes;
            readonly byte[] DestBytes;            
            readonly byte[] Result;
            public Or(OpcodeInput Input) : base("OR", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {
                Input.DecodedModRM = ModRMDecode(FetchNext());
                StoredInput = Input;
                (DestBytes, SourceBytes) = FetchDynamic(StoredInput);
                ImmediateBuffer = SourceBytes;
                //
                Result = Bitwise.Or(DestBytes, SourceBytes);
            }

            public override void Execute()
            {
                SetDynamic(StoredInput, Result);
                SetFlags(Result, FlagMode.Logic, DestBytes, SourceBytes);
            }
        }
        public class Push : Opcode
        {
            byte[] Result; 
                          //push imm8 is valid but not push r8                // no 32 bit mode for reg push  
            public Push(OpcodeInput Input, byte PushSize=0) : base("PUSH", (PrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.WORD : RegisterCapacity.QWORD)
            {
                StoredInput = Input;
                (_, Result) = FetchDynamic(Input);        
            }
            public override void Execute()
            {                       //dont set size because rsp already gives default size 8
              //FIX  SetRegister(ByteCode.SP, Bitwise.Subtract(FetchRegister(ByteCode.SP, RegisterCapacity.Qword), new byte[] { (byte)CurrentCapacity }, false, (int)CurrentCapacity));                            
                SetMemory(Convert.ToUInt64(FetchRegister(ByteCode.SP, RegisterCapacity.QWORD)), Result); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
            }
        }
        public class Pop : Opcode
        {
            byte[] StackBytes;                                // no 32 bit mode for reg pop, default it to 64
            public Pop(OpcodeInput Input) : base("POP", (PrefixBuffer.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.WORD : RegisterCapacity.QWORD)
            {
                StoredInput = Input;
                StackBytes = Fetch(BitConverter.ToUInt64(FetchRegister(ByteCode.SP, RegisterCapacity.QWORD),0), (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetDynamic(StoredInput, StackBytes); // pointer rsp
                // pop [ptr]0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction
                // so i just point it to the generic pop function cause it isnt special enough                          V  pop size always this because pop is always known size
                //AAAAAAAAASetRegister(ByteCode.SP, Bitwise.Add(FetchRegister(ByteCode.SP, RegisterCapacity.Qword), new byte[] { (byte)CurrentCapacity }, false, (int)CurrentCapacity));
                
            }
        }
        public class Mul : Opcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            readonly byte OprandCount;
            readonly byte[] SourceBytes;
            readonly byte[] Result;
            readonly byte[] DestBytes;
            readonly FlagSet ResultFlags;
            public Mul(OpcodeInput input, byte oprandCount) : base((input.IsSigned) ? "IMUL" : "MUL", (input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {
                OprandCount = oprandCount;
                StoredInput = input;
                (DestBytes, SourceBytes) = FetchDynamic(input);
                ImmediateBuffer = SourceBytes;
                //for immediate versions, sourcebytes and destbytes are the wrong way round. doesnt massively matter(because in the multidef form the dest is always eax, but the dest = 1st byte read)
                ResultFlags = Bitwise.Multiply(DestBytes, SourceBytes, StoredInput.IsSigned, (int)CurrentCapacity, out Result); // fills 2 regs
            }
            public override void Execute()
            {    
                //careful refactoring this it gets messy, dont think there is a way without losing performance when it isn't needed
                //e.g having to use take,skip with both
                if (OprandCount == 1) 
                {                               
                    //little endian smallest first
                    SetRegister(ByteCode.A, Bitwise.Cut(Result, (int)CurrentCapacity)); // higher bytes to d
                    SetRegister(ByteCode.D, Bitwise.Subarray(Result, (int)CurrentCapacity)); // lower to a=
                } else 
                {
                    SetRegister((ByteCode)StoredInput.DecodedModRM.Reg, Bitwise.Cut(Result, (int)CurrentCapacity)); // higher bytes to d, never a ptr dont use dynamic
                } //copy into source, source isnt source here. choppy workaround in x86_64, because ptr is never dest of imul 2+oprand op, so source and dest are swapped here
                SetFlags(ResultFlags);
            }
        }
        public class Sub : Opcode // 29 32bit, 28 8bit,
        {// 2c,2d=A ; 80 8bit 81 32bit modrm ; 28, 29 sign extend; 2a 2b swap
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            byte[] SourceBytes;
            byte[] DestBytes;
            byte[] Result;
            FlagSet ResultFlags;
            public Sub(OpcodeInput Input, bool UseBorrow=false) : base((UseBorrow) ? "SBB" : "SUB", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {
                StoredInput = Input;
                (DestBytes, SourceBytes) = FetchDynamic(StoredInput);
                ImmediateBuffer = SourceBytes;

                ResultFlags = Bitwise.Subtract(DestBytes, SourceBytes, (int)CurrentCapacity, out Result, (Flags.Carry == FlagState.On && UseBorrow));
            }
            public override void Execute()
            {
                SetFlags(ResultFlags);
                SetDynamic(StoredInput, Result);
            }
        }
 
        public class Div : Opcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            readonly byte[] Quotient;
            readonly byte[] Modulo;
            readonly byte[] SourceBytes;
            readonly byte[] DestBytes;
            public Div(OpcodeInput Input) : base((Input.IsSigned) ? "IDIV" : "DIV", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())
            {
                StoredInput = Input;
                (DestBytes, SourceBytes) = FetchDynamic(StoredInput);
                // always a reg, atleast for this opcode
                Bitwise.Divide(DestBytes, SourceBytes, StoredInput.IsSigned, (int)CurrentCapacity, out Quotient, out Modulo);
            }
            public override void Execute()
            {
                SetRegister(ByteCode.A, Quotient); // lower to a              
                SetRegister(ByteCode.D, Modulo); // higher bytes to d               
            }
        }
        public class Cmp : Opcode
        {
            readonly byte[] SrcData;
            readonly byte[] DestData;
            public Cmp(OpcodeInput Input) : base("CMP", (Input.Is8Bit) ? RegisterCapacity.BYTE : GetRegCap())// feels better and more general than to use IsEAX ?
            {
                StoredInput = Input;
                (DestData, SrcData) = FetchDynamic(StoredInput);
            }

            public override void Execute()
            {
                //basically all cmp does flags-wise is subtract, but doesn't care about the result               
                SetFlags(Bitwise.Subtract(SrcData, DestData, (int)CurrentCapacity, out _));
            }
        }
        public class Jmp : Opcode
        {
            // rel8, rel32, rel16, absolute r/m64 (absolute r/m32,16 zero extended),  

            public Jmp(bool Relative, byte Bytes, string Opcode = "JMP") : base(Opcode, (RegisterCapacity)(Bytes))
            {
                StoredInput = new OpcodeInput
                {
                    DecodedModRM = (!Relative) ? ModRMDecode()
                    : new ModRM()
                    {
                        DestPtr = BitConverter.ToUInt64(Bitwise.SignExtend(FetchNext(Bytes), 8), 0),
                        Mod = 4,
                    }
                };
                StoredInput.DecodedModRM.Offset = (long)StoredInput.DecodedModRM.DestPtr;
            }
            public override void Execute()
            {
                switch (Mnemonic)
                { //speed codesize tradeoff, we could have it test for each one, make a list of opcodes that would take the jump given the eflags and jump if the given opcode is in that list
                    //jmps are used alot so its probably best we dont
                    // wrapped ifs in !(--) rather than simplifying for readability, return on negative case so we dont repeat rip = dest
                    case "JA": //77 jump above ja, jnbe UNFOR SIGNED
                        if(!(Flags.Carry == FlagState.Off && Flags.Zero == FlagState.Off)) { return; }
                        break;
                    case "JNC": //73 jump no carry jnc, jae, jnb FOR UNSIGNED
                        if(!(Flags.Carry == FlagState.Off)) { return; }
                        break;
                    case "JC": // 72 jump carry:  jb, jc, jnae FOR UNSIGNED
                        if (!(Flags.Carry == FlagState.On)) { return; }
                        break;
                    case "JNA": //76 JNA jna UNFOR SIGNED
                        if(!(Flags.Carry == FlagState.On || Flags.Zero == FlagState.On)){ return; }
                        break;
                    case "JRCXZ": //E3 jump rcx zero, with an addr32 prefix this becomes jecxz (ecx zero) , jcxz is 32bit only because in 32bit mode jcxz uses the addr32 prefix not jecxz
                        if(!(PrefixBuffer.Contains(PrefixByte.ADDR32) && FetchRegister(ByteCode.A, RegisterCapacity.DWORD).IsZero()) || !(FetchRegister(ByteCode.A, RegisterCapacity.QWORD).IsZero())){ return; } //where jecxz then has none
                        break;
                    case "JZ": //74 jump zero
                        if(!(Flags.Zero == FlagState.On)) { return; }
                        break;
                    case "JNZ": //75 jump not zero, jne
                        if (!(Flags.Carry == FlagState.Off)) { return; }
                        break;
                    case "JG": // 7F jump > ,JNLE FOR UNSIGNED
                        if(!(Flags.Zero == FlagState.Off && Flags.Sign == Flags.Overflow)) { return; }
                        break;
                    case "JGE": // 7D j >= , jnl FOR UNSIGNED
                        if(!(Flags.Sign == Flags.Overflow)) { return; }
                        break;
                    case "JL": //7C j < ,jnge FOR UNSIGNED
                        if(!(Flags.Sign != Flags.Overflow)) { return; }
                        break;
                    case "JLE": //7E j <= , jng FOR UNSIGNED
                        if(!(Flags.Zero == FlagState.On || Flags.Sign != Flags.Overflow)) { return; }
                        break;
                    case "JO": //70 jump overflow
                        if (!(Flags.Overflow == FlagState.On)) { return; }
                        break;
                    case "JNO": // 71 jump no overflow
                        if(!(Flags.Overflow == FlagState.Off)) { return; }
                        break;
                    case "JS": // 78 jump sign jump negative
                        if(!(Flags.Sign == FlagState.On)) { return; }
                        break;
                    case "JNS": // 79 jump not sign/ jump positive, jump >0
                        if(!(Flags.Sign == FlagState.Off)) { return; }
                        break;
                    case "JP": // 7a jump parity, jump parity even
                        if(!(Flags.Parity == FlagState.On)) { return; }
                        break;
                    case "JNP": // 7b jump no parity/odd parity
                        if (!(Flags.Parity == FlagState.Off)) { return; }
                        break;
                }
                InstructionPointer = StoredInput.DecodedModRM.DestPtr;
            }
        }
 
      /*  public class Inc : MyOpcode 
        {
            byte[] DestBytes;
            byte[] Result;
            public Inc(ModRM Input)
            {
                CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                Mnemonic = "INC";
                base.Input = Input;
                DestBytes = Fetch(base.Input, true);
                Result = Bitwise.Increment(DestBytes, (int)CurrentCapacity);
            }
            public override void Execute()
            {               
                Set(Input, Result);
                SetFlags(Result, FlagMode.Inc, DestBytes, null);
            }
        }

        public class Dec : MyOpcode
        {
            byte[] DestBytes;
            byte[] Result;
            public Dec(ModRM Input)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                Mnemonic = "DEC";
                base.Input = Input;
                DestBytes = Fetch(base.Input, true);
                Result = Bitwise.Decrement(DestBytes, (int)CurrentCapacity);
            }
            public override void Execute()
            {
                Set(Input, Result);
                SetFlags(Result, FlagMode.Dec, DestBytes, null);
            }
        }*/
    }
}