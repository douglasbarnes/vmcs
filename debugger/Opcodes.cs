using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Opcodes;
using static debugger.MultiDefinitionDecoder;
using static debugger.Util.Opcode;
using static debugger.Util;
using static debugger.ControlUnit;
using static debugger.Primitives;

namespace debugger
{
    public static class OpcodeLookup
    {
        public static Dictionary<PrefixByte, string> PrefixTable = new Dictionary<PrefixByte, string>();
        public static Dictionary<byte, Dictionary<byte, Func<MyOpcode>>> OpcodeTable = new Dictionary<byte, Dictionary<byte, Func<MyOpcode>>>();

        private static void _addPrefixes()
        {
            PrefixTable.Add(PrefixByte.REXW, "REXW");
            PrefixTable.Add(PrefixByte.ADDR32, "ADDR32");
        }

        private static void _addMultiDefinitionDecoders()
        {
            //0x80
            Dictionary<int, Func<MyOpcode>> _80 = new Dictionary<int, Func<MyOpcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true}, UseBorrow:true)},
             //   { 4, () => new AndImm(MultiDefOutput) { Is8Bit=true } },
                { 5,() => new Sub(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true}, UseBorrow:false)},
             //   { 6, () => new XorImm(MultiDefOutput) { Is8Bit=true } },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=MultiDefOutput, Is8Bit=true, IsImmediate=true})},
            };       
            MasterDecodeDict.Add(0x80, _80);
            //or
            //adc
            //sbb
            //and
            //sub
            //xor
            //cmp
            //0x81
            Dictionary<int, Func<MyOpcode>> _81 = new Dictionary<int, Func<MyOpcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true}, UseBorrow:true)},
            //   { 4,  () => new AndImm(MultiDefOutput) },
                { 5,  () => new Sub(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true}, UseBorrow:false)},
            //   { 6,  () => new XorImm(MultiDefOutput) },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true})},
            };
            MasterDecodeDict.Add(0x81, _81);

            //0x82 no longer x86_64

            //0x83
            Dictionary<int, Func<MyOpcode>> _83 = new Dictionary<int, Func<MyOpcode>>
            {
                { 0, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true, IsSignExtendedByte=true}, UseCarry:false)},
                { 1, () => new  Or(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true, IsSignExtendedByte=true})},
                { 2, () => new Add(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true, IsSignExtendedByte=true}, UseCarry:true)},
                { 3, () => new Sub(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true, IsSignExtendedByte=true}, UseBorrow:true)},
            //   { 4, () => new AndImm(MultiDefOutput) { IsSwap=true } },
            //   { 5, () => new SubImm(MultiDefOutput, SignExtend:true) },
            //   { 6, () => new XorImm(MultiDefOutput, SignExtend:true) },
                { 7, () => new Cmp(new OpcodeInput{ DecodedModRM=MultiDefOutput, IsImmediate=true, IsSignExtendedByte=true })},
            };
            MasterDecodeDict.Add(0x83, _83);

            //0xf6
            Dictionary<int, Func<MyOpcode>> _F6 = new Dictionary<int, Func<MyOpcode>>
            {
                { 4, () => new Mul(new OpcodeInput{DecodedModRM = MultiDefOutput, Is8Bit=true},Oprands:1)},
                { 5, () => new Mul(new OpcodeInput{DecodedModRM = MultiDefOutput, Is8Bit=true, IsSigned=true}, Oprands:1)},
                { 6, () => new Div(new OpcodeInput{DecodedModRM = MultiDefOutput, Is8Bit=true})},
                { 7, () => new Div(new OpcodeInput{DecodedModRM = MultiDefOutput, Is8Bit=true, IsSigned=true})}
            };
            MasterDecodeDict.Add(0xF6, _F6);

            //0xf7
            Dictionary<int, Func<MyOpcode>> _F7 = new Dictionary<int, Func<MyOpcode>>
            {
                { 4, () => new Mul(new OpcodeInput{DecodedModRM = MultiDefOutput}, Oprands:1) },
                { 5, () => new Mul(new OpcodeInput{DecodedModRM = MultiDefOutput, IsSigned=true}, Oprands:1)},
                { 6, () => new Div(new OpcodeInput{DecodedModRM = MultiDefOutput}) },
                { 7, () => new Div(new OpcodeInput{DecodedModRM = MultiDefOutput, IsSigned=true})}
            };

            MasterDecodeDict.Add(0xF7, _F7);

            //0xfe
            Dictionary<int, Func<MyOpcode>> _FE = new Dictionary<int, Func<MyOpcode>>
            {
              //  { 0, () => new Inc(MultiDefOutput) { Is8Bit=true } },
            };
            MasterDecodeDict.Add(0xFE, _FE);
            //0xff
            Dictionary<int, Func<MyOpcode>> _FF = new Dictionary<int, Func<MyOpcode>>
            {
               // { 0, () => new Inc(MultiDefOutput) },
                { 4, () => new Jmp(Relative:false, Bytes:8) },
                { 6, () => new Push(new OpcodeInput{DecodedModRM = MultiDefOutput}) }
            };
            MasterDecodeDict.Add(0xFF, _FF);
        }

        public static void Refresh()
        {
            _addPrefixes();
            _addMultiDefinitionDecoders();
            OpcodeTable.Add(1, new Dictionary<byte, Func<MyOpcode>>()
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

                //67 prefix
                { 0x68, () => new Push(new OpcodeInput{ IsImmediate=true}, PushSize:32) }, // its always 32, weird
                { 0x69, () => new  Mul(new OpcodeInput { DecodedModRM = ModRMDecode(), IsImmediate=true, IsSigned=true}, Oprands:3) },
                { 0x6A, () => new Push(new OpcodeInput{ IsImmediate=true}, PushSize:8) },
                { 0x6B, () => new  Mul(new OpcodeInput{ DecodedModRM = ModRMDecode(), IsImmediate=true, IsSigned=true, IsSignExtendedByte=true}, Oprands:3)},

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
                { 0xC7, () => new Mov(new OpcodeInput{ DecodedModRM=ModRMDecode(), IsImmediate=true})},

                { 0xE3, () => new Jmp(Relative:true, Bytes:1, Opcode:"JRCXZ") },

                { 0xEB, () => new Jmp(Relative:true, Bytes:1) },
                { 0xE9, () => new Jmp(Relative:true, Bytes:4) },

                { 0xF6, () => Decode(0xF6) },
                { 0xF7, () => Decode(0xF7) },
                { 0xFF, () => Decode(0xFF) }    
            });
            OpcodeTable.Add(2, new Dictionary<byte, Func<MyOpcode>>()
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

                { 0xAF, () => new Mul(new OpcodeInput { DecodedModRM=ModRMDecode(), IsSigned=true}, Oprands:2)},
            });

            //OpcodeTable.Add(opcode, ExecuteAction);

        }
    }

    public static class MultiDefinitionDecoder
    {
        public static ModRM MultiDefOutput;
        //byte = opcode, Dict<byte = displacement(increment doesnt matter), action = thing to do>
        // opcodes with one operand e.g inc eax (increment eax) don't use the REG bits in modRM, so the regbits can be used to make extra instructions instead
        // so we can have one "opcode byte" that has meanings depending on the REG bits
        // reg bit starts at the 8 dec val position in a string of bits e.g "01[100]111" [REG] so each offset is -8 each time
        public static Dictionary<byte, Dictionary<int, Func<MyOpcode>>> MasterDecodeDict = new Dictionary<byte, Dictionary<int, Func<MyOpcode>>>();
        public static MyOpcode Decode(byte bOpcode)
        {
            MultiDefOutput = ModRMDecode();
            ulong _tmpsrc = MultiDefOutput.SourceReg; //swap because
              MultiDefOutput.SourceReg = MultiDefOutput.DestPtr;
     //         MultiDefOutput.Dest = _tmpsrc;
            return MasterDecodeDict[bOpcode][(byte)_tmpsrc].Invoke();
        }
    }

    public class Opcodes
    {
        
        public abstract class MyOpcode
        {
            protected internal byte[] ImmediateBuffer;

            internal readonly string Mnemonic;                       
            internal OpcodeInput StoredInput;
            internal readonly ulong StartPointer;
            public override string ToString()
            {
                return Mnemonic;
            }          
            public MyOpcode(string OpcodeMnemonic, RegisterCapacity OpcodeCapacity)
            {
                Mnemonic = OpcodeMnemonic;
                CurrentCapacity = OpcodeCapacity;
                StartPointer = BytePointer;
            }
            public abstract void Execute();
            public virtual ulong Disassemble(out string Assembly)
            {
                string[] DisassembledModRM = Disassembly.DisassembleModRM(StoredInput, CurrentCapacity);
                Assembly = $"{Mnemonic} ";
                Array.Resize(ref ImmediateBuffer, 8);
                if (StoredInput.IsImmediate)
                {
                    Assembly += $"{DisassembledModRM[0]}, 0x{BitConverter.ToUInt64(ImmediateBuffer,0).ToString("X")}";
                } else
                {
                    Assembly += $"{DisassembledModRM[0]}, {DisassembledModRM[1]}";
                }              
                return BytePointer - StartPointer;
            }
            
        }
        public class Nop : MyOpcode {
            public Nop() : base("NOP", RegisterCapacity.B) { }
            public override void Execute() { }  
            public override ulong Disassemble(out string Assembly)
            {
                Assembly = Mnemonic;
                return 0;
            }              
        }
        public class Mov : MyOpcode
        {
            readonly byte[] SourceBytes;
            public Mov(OpcodeInput Input) : base("MOV", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {                
                //initialisation
                //if (!Input.IsImmediate) { Input.DecodedModRM = ModRMDecode(); }
                StoredInput = Input;
                //
                SourceBytes = FetchDynamic(StoredInput).SourceBytes;
                ImmediateBuffer = SourceBytes; //set this anyway, use it if we want
            } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {
                SetDynamic(StoredInput, SourceBytes); // immediate should never swap 
            }
        }
        public class Add : MyOpcode // 01 32bit, 00 8bit,
        { // imm // 05=EAX, 81=any  04=al, 80=8bit
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            readonly byte[] SourceBytes;
            readonly byte[] DestBytes;
            readonly byte[] Result;
            public Add(OpcodeInput Input, bool UseCarry) : base( (UseCarry) ? "ADC" : "ADD", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(StoredInput);
                ImmediateBuffer = FetchedData.SourceBytes;
                SourceBytes = FetchedData.SourceBytes;
                DestBytes = FetchedData.DestBytes;

                Result = Bitwise.Add(DestBytes, SourceBytes, (Eflags.Carry && UseCarry), (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetFlags(Result, FlagMode.Add, DestBytes, SourceBytes);
                SetDynamic(StoredInput, Result);
            }
        }
        public class Or : MyOpcode
        {
            readonly byte[] SourceBytes;
            readonly byte[] DestBytes;            
            readonly byte[] Result;
            public Or(OpcodeInput Input) : base("OR", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {
                Input.DecodedModRM = ModRMDecode(FetchNext());
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(StoredInput);
                SourceBytes = FetchedData.SourceBytes;
                ImmediateBuffer = SourceBytes;
                DestBytes = FetchedData.DestBytes;
                //
                Result = Bitwise.Or(DestBytes, SourceBytes);
            }

            public override void Execute()
            {
                SetDynamic(StoredInput, Result);
                SetFlags(Result, FlagMode.Logic, DestBytes, SourceBytes);
            }
        }
        public class Push : MyOpcode
        {
            byte[] Result; 
                          //push imm8 is valid but not push r8                // no 32 bit mode for reg push  
            public Push(OpcodeInput Input, byte PushSize=0) : base("PUSH", (Prefixes.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R)
            {
                StoredInput = Input;
                Result = FetchDynamic(Input).SourceBytes;        
            }
            public override void Execute()
            {                       //dont set size because rsp already gives default size 8
                SetRegister(ByteCode.SP, Bitwise.Subtract(FetchRegister(ByteCode.SP, RegisterCapacity.R), new byte[] { (byte)CurrentCapacity }, false, (int)CurrentCapacity));                            
                SetMemory(FetchRegister(ByteCode.SP, RegisterCapacity.R), Result); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
            }
        }
        public class Pop : MyOpcode
        {
            byte[] StackBytes;                                // no 32 bit mode for reg pop, default it to 64
            public Pop(OpcodeInput Input) : base("POP", (Prefixes.Contains(PrefixByte.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R)
            {
                StoredInput = Input;
                StackBytes = Fetch(FetchRegister(ByteCode.SP, RegisterCapacity.R), (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetDynamic(StoredInput, StackBytes); // pointer rsp
                // pop [ptr]0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction
                // so i just point it to the generic pop function cause it isnt special enough                          V  pop size always this because pop is always known size
                SetRegister(ByteCode.SP, Bitwise.Add(FetchRegister(ByteCode.SP, RegisterCapacity.R), new byte[] { (byte)CurrentCapacity }, false, (int)CurrentCapacity));
            }
        }
        public class Mul : MyOpcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            readonly byte _oprands;
            readonly byte[] SourceBytes;
            readonly byte[] Result;
            readonly byte[] DestBytes;
            public Mul(OpcodeInput Input, byte Oprands) : base((Input.IsSigned) ? "IMUL" : "MUL", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {
                _oprands = Oprands;
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(Input);
                DestBytes = FetchedData.DestBytes;
                SourceBytes = FetchedData.SourceBytes;
                ImmediateBuffer = SourceBytes;
                Result = Bitwise.Multiply(DestBytes, SourceBytes, Signed:StoredInput.IsSigned, (int)CurrentCapacity); // fills 2 regs
            }
            public override void Execute()
            {    
                //careful refactoring this it gets messy, dont think there is a way without losing performance when it isn't needed
                //e.g having to use take,skip with both
                if (_oprands == 1) 
                {                               
                    //little endian smallest first
                    SetRegister(ByteCode.A, Bitwise.Cut(Result, (int)CurrentCapacity)); // higher bytes to d
                    SetRegister(ByteCode.D, Bitwise.Subarray(Result, (int)CurrentCapacity).ToArray()); // lower to a=
                } else 
                {
                    SetRegister((ByteCode)StoredInput.DecodedModRM.SourceReg, Bitwise.Cut(Result, (int)CurrentCapacity)); // higher bytes to d, never a ptr dont use dynamic
                } //copy into source, source isnt source here. choppy workaround in x86_64, because ptr is never dest of imul 2+oprand op, so source and dest are swapped here
                SetFlags(Result, FlagMode.Mul, SourceBytes, DestBytes);
            }
        }
        public class Sub : MyOpcode // 29 32bit, 28 8bit,
        {// 2c,2d=A ; 80 8bit 81 32bit modrm ; 28, 29 sign extend; 2a 2b swap
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            byte[] SourceBytes;
            byte[] DestBytes;
            byte[] Result;
            public Sub(OpcodeInput Input, bool UseBorrow=false) : base((UseBorrow) ? "SBB" : "SUB", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(StoredInput);
                ImmediateBuffer = FetchedData.SourceBytes;
                SourceBytes = FetchedData.SourceBytes;
                DestBytes = FetchedData.DestBytes;

                Result = Bitwise.Subtract(DestBytes, SourceBytes, (Eflags.Carry && UseBorrow), (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetFlags(Result, FlagMode.Sub, DestBytes, SourceBytes);
                SetDynamic(StoredInput, Result);
            }
        }
 
        public class Div : MyOpcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            byte[] baDivided;
            byte[] baRemainder;
            public Div(OpcodeInput Input) : base((Input.IsSigned) ? "IDIV" : "DIV", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())
            {
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(Input);
                // always a reg, atleast for this opcode
                baDivided = Bitwise.Divide(FetchedData.DestBytes, FetchedData.SourceBytes, StoredInput.IsSigned, (int)CurrentCapacity);
                baRemainder = Bitwise.Modulo(FetchedData.DestBytes, FetchedData.SourceBytes, StoredInput.IsSigned, (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetRegister(ByteCode.A, baDivided); // lower to a              
                SetRegister(ByteCode.D, baRemainder); // higher bytes to d               
            }
        }
        public class Cmp : MyOpcode
        {
            readonly byte[] SrcData;
            readonly byte[] DestData;
            public Cmp(OpcodeInput Input) : base("CMP", (Input.Is8Bit) ? RegisterCapacity.B : GetRegCap())// feels better and more general than to use IsEAX ?
            {
                StoredInput = Input;
                DynamicResult FetchedData = FetchDynamic(StoredInput);
                SrcData = FetchedData.SourceBytes;
                DestData = FetchedData.DestBytes;
            }

            public override void Execute()
            {
                //basically all cmp does flags-wise is subtract, but doesn't care about the result               
                SetFlags(Bitwise.Subtract(SrcData, DestData, bCarry: false, (int)CurrentCapacity), FlagMode.Sub, DestData, SrcData);
            }
        }
        public class Jmp : MyOpcode
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
                switch(Mnemonic)
                { //speed codesize tradeoff, we could have it test for each one, make a list of opcodes that would take the jump given the eflags and jump if the given opcode is in that list
                    //jmps are used alot so its probably best we dont
                    // wrapped ifs in !(--) rather than simplifying for readability, return on negative case so we dont repeat rip = dest
                    case "JA": //77 jump above ja, jnbe UNFOR SIGNED
                        if(!(!Eflags.Carry && !Eflags.Zero)) { return; }
                        break;
                    case "JNC": //73 jump no carry jnc, jae, jnb FOR UNSIGNED
                        if(!(!Eflags.Carry)) { return; }
                        break;
                    case "JC": // 72 jump carry:  jb, jc, jnae FOR UNSIGNED
                        if (!(Eflags.Carry)) { return; }
                        break;
                    case "JNA": //76 JNA jna UNFOR SIGNED
                        if(!(Eflags.Carry || Eflags.Zero)){ return; }
                        break;
                    case "JRCXZ": //E3 jump rcx zero, with an addr32 prefix this becomes jecxz (ecx zero) , jcxz is 32bit only because in 32bit mode jcxz uses the addr32 prefix not jecxz
                        if(!(Prefixes.Contains(PrefixByte.ADDR32) && FetchRegister(ByteCode.A, RegisterCapacity.E).IsZero()) || !(FetchRegister(ByteCode.A, RegisterCapacity.R).IsZero())){ return; } //where jecxz then has none
                        break;
                    case "JZ": //74 jump zero
                        if(!(Eflags.Zero)) { return; }
                        break;
                    case "JNZ": //75 jump not zero, jne
                        if (!(!Eflags.Zero)) { return; }
                        break;
                    case "JG": // 7F jump > ,JNLE FOR UNSIGNED
                        if(!(!Eflags.Zero && Eflags.Sign == Eflags.Overflow)) { return; }
                        break;
                    case "JGE": // 7D j >= , jnl FOR UNSIGNED
                        if(!(Eflags.Sign == Eflags.Overflow)) { return; }
                        break;
                    case "JL": //7C j < ,jnge FOR UNSIGNED
                        if(!(Eflags.Sign != Eflags.Overflow)) { return; }
                        break;
                    case "JLE": //7E j <= , jng FOR UNSIGNED
                        if(!(Eflags.Zero || Eflags.Sign != Eflags.Overflow)) { return; }
                        break;
                    case "JO": //70 jump overflow
                        if (!(Eflags.Overflow)) { return; }
                        break;
                    case "JNO": // 71 jump no overflow
                        if(!(!Eflags.Overflow)) { return; }
                        break;
                    case "JS": // 78 jump sign jump negative
                        if(!(Eflags.Sign)) { return; }
                        break;
                    case "JNS": // 79 jump not sign/ jump positive, jump >0
                        if(!(!Eflags.Sign)) { return; }
                        break;
                    case "JP": // 7a jump parity, jump parity even
                        if(!(Eflags.Parity)) { return; }
                        break;
                    case "JNP": // 7b jump no parity/odd parity
                        if (!(!Eflags.Parity)) { return; }
                        break;
                }
                BytePointer = StoredInput.DecodedModRM.DestPtr;
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
                DestBytes = FetchDynamic(base.Input, true);
                Result = Bitwise.Increment(DestBytes, (int)CurrentCapacity);
            }
            public override void Execute()
            {               
                SetDynamic(Input, Result);
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
                DestBytes = FetchDynamic(base.Input, true);
                Result = Bitwise.Decrement(DestBytes, (int)CurrentCapacity);
            }
            public override void Execute()
            {
                SetDynamic(Input, Result);
                SetFlags(Result, FlagMode.Dec, DestBytes, null);
            }
        }*/
    }
}