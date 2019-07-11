using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Registers;
using static debugger.Opcodes;
using static debugger.MultiDefinitionDecoder;
using static debugger.Util.Opcode;
using static debugger.Util;
namespace debugger
{
    public static class OpcodeLookup
    {
        public static Dictionary<PrefixBytes, string> PrefixTable = new Dictionary<PrefixBytes, string>();
        public static Dictionary<byte, Dictionary<byte, Action>> OpcodeTable = new Dictionary<byte, Dictionary<byte, Action>>();

        private static void _addPrefixes()
        {
            PrefixTable.Add(PrefixBytes.REXW, "REXW");
            PrefixTable.Add(PrefixBytes.ADDR32, "ADDR32");
        }

        private static void _addMultiDefinitionDecoders()
        {
            //0x80
            Dictionary<int, Action> _80 = new Dictionary<int, Action>
            {
                { 0, () => new AddImm(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 1, () => new OrImm(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 2, () => new AddImm(MultiDefOutput, UseCarry:true) { Is8Bit=true }.Execute() },
                { 3, () => new SubImm(MultiDefOutput, UseBorrow:true) { Is8Bit=true }.Execute() },
                { 4, () => new AndImm(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 5, () => new SubImm(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 6, () => new XorImm(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 7, () => new Cmp(IsImmediate:true) { Is8Bit=true }.Execute() },
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
            Dictionary<int, Action> _81 = new Dictionary<int, Action>
            {
                { 0, () => new AddImm(MultiDefOutput).Execute() },
                { 1, () => new OrImm(MultiDefOutput).Execute() },
                { 2, () => new AddImm(MultiDefOutput, UseCarry:true).Execute() },
                { 3, () => new SubImm(MultiDefOutput, UseBorrow:true).Execute() },
                { 4, () => new AndImm(MultiDefOutput).Execute() },
                { 5, () => new SubImm(MultiDefOutput).Execute() },
                { 6, () => new XorImm(MultiDefOutput).Execute() },
                { 7, () => new Cmp(IsImmediate:true).Execute() },
            };
            MasterDecodeDict.Add(0x81, _81);

            //0x82 no longer x86_64

            //0x83
            Dictionary<int, Action> _83 = new Dictionary<int, Action>
            {
                { 0, () => new AddImm(MultiDefOutput, SignExtend:true).Execute() },
                { 1, () => new OrImm(MultiDefOutput, SignExtend:true).Execute() },
                { 2, () => new AddImm(MultiDefOutput, SignExtend:true, UseCarry:true).Execute() },
                { 3, () => new SubImm(MultiDefOutput, SignExtend:true, UseBorrow:true).Execute() },
                { 4, () => new AndImm(MultiDefOutput) { IsSwap=true }.Execute() },
                { 5, () => new SubImm(MultiDefOutput, SignExtend:true).Execute() },
                { 6, () => new XorImm(MultiDefOutput, SignExtend:true).Execute() },
                { 7, () => new Cmp(IsImmediate:true, SignExtendedByte:true).Execute() },
            };
            MasterDecodeDict.Add(0x83, _83);

            //0xf6
            Dictionary<int, Action> _F6 = new Dictionary<int, Action>
            {
                { 4, () => new Mul(Oprands:1) { Is8Bit=true }.Execute() },
                { 5, () => new Mul(Oprands:1) { IsSigned=true }.Execute() },
                { 6, () => new Div(MultiDefOutput) { Is8Bit=true }.Execute() },
                { 7, () => new Div(MultiDefOutput) { Is8Bit=true, IsSigned=true }.Execute() }
            };
            MasterDecodeDict.Add(0xF6, _F6);

            //0xf7
            Dictionary<int, Action> _F7 = new Dictionary<int, Action>
            {
                { 4, () => new Mul(Oprands:1).Execute() },
                { 5, () => new Mul(Oprands:1) { IsSigned=true }.Execute() },
                { 6, () => new Div(MultiDefOutput).Execute() },
                { 7, () => new Div(MultiDefOutput) { IsSigned=true }.Execute() }
            };

            MasterDecodeDict.Add(0xF7, _F7);

            //0xfe
            Dictionary<int, Action> _FE = new Dictionary<int, Action>
            {
                { 0, () => new Inc(MultiDefOutput) { Is8Bit=true }.Execute() },
            };
            MasterDecodeDict.Add(0xFE, _FE);
            //0xff
            Dictionary<int, Action> _FF = new Dictionary<int, Action>
            {
                { 0, () => new Inc(MultiDefOutput).Execute() },
                { 4, () => new Jmp(Relative:false, Bytes:8).Execute() },
                { 6, () => new Push().Execute() }
            };
            MasterDecodeDict.Add(0xFF, _FF);
        }
        public static void Refresh()
        {
            _addPrefixes();
            _addMultiDefinitionDecoders();
            OpcodeTable.Add(1, new Dictionary<byte, Action>()
            {
                { 0x00, () => new Add() { Is8Bit=true}.Execute() },
                { 0x01, () => new Add().Execute() },
                { 0x02, () => new Add() { IsSwap=true, Is8Bit=true }.Execute() },
                { 0x03, () => new Add() { IsSwap=true }.Execute() },
                { 0x04, () => new AddImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x05, () => new AddImm(Input:FromDest(ByteCode.A)).Execute() },
                { 0x08, () => new Or().Execute() },
                { 0x09, () => new Or().Execute() },
                { 0x0A, () => new Or() { IsSwap=true, Is8Bit=true }.Execute() },
                { 0x0B, () => new Or() { IsSwap=true }.Execute() },
                { 0x0C, () => new OrImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x0D, () => new OrImm(Input:FromDest(ByteCode.A)).Execute() },
                { 0x10, () => new Add(UseCarry:true) {Is8Bit=true }.Execute() },
                { 0x11, () => new Add(UseCarry:true).Execute() },
                { 0x12, () => new Add(UseCarry:true) { Is8Bit=true, IsSwap=true}.Execute() },
                { 0x13, () => new Add(UseCarry:true) { IsSwap=true }.Execute() },
                { 0x14, () => new AddImm(Input:FromDest(ByteCode.A), UseCarry:true) { Is8Bit=true }.Execute() },
                { 0x15, () => new AddImm(Input:FromDest(ByteCode.A), UseCarry:true).Execute() },

                { 0x18, () => new Sub(UseBorrow:true) { Is8Bit=true }.Execute() },
                { 0x19, () => new Sub(UseBorrow:true).Execute() },
                { 0x1A, () => new Sub(UseBorrow:true) { Is8Bit=true, IsSwap=true }.Execute() },
                { 0x1B, () => new Sub(UseBorrow:true) { IsSwap=true }.Execute() },
                { 0x1C, () => new Sub(UseBorrow:true) { Is8Bit=true }.Execute() },
                { 0x1D, () => new Sub(UseBorrow:true).Execute() },

                { 0x20, () => new And() { Is8Bit=true }.Execute() },
                { 0x21, () => new And().Execute() },
                { 0x22, () => new And() { Is8Bit=true, IsSwap=true}.Execute() },
                { 0x23, () => new And() { IsSwap=true }.Execute() },
                { 0x24, () => new AndImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x25, () => new AndImm(Input:FromDest(ByteCode.A)).Execute() },

                { 0x28, () => new Sub().Execute() },
                { 0x29, () => new Sub().Execute() },
                { 0x2A, () => new Sub() { IsSwap=true, Is8Bit=true }.Execute() },
                { 0x2B, () => new Sub() { IsSwap=true }.Execute() },
                { 0x2C, () => new SubImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x2D, () => new SubImm(Input:FromDest(ByteCode.A)).Execute() },

                { 0x30, () => new Xor() { Is8Bit=true }.Execute() },
                { 0x31, () => new Xor().Execute() },
                { 0x32, () => new Xor() { Is8Bit=true, IsSwap=true}.Execute() },
                { 0x33, () => new Xor() { IsSwap=true }.Execute() },
                { 0x34, () => new XorImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x35, () => new XorImm(Input:FromDest(ByteCode.A)).Execute() },

                { 0x38, () => new Cmp() { Is8Bit=true }.Execute() },
                { 0x39, () => new Cmp().Execute() },
                { 0x3A, () => new Cmp() { Is8Bit=true, IsSwap=true }.Execute() },
                { 0x3B, () => new Cmp() { IsSwap=true }.Execute() },
                { 0x3C, () => new Cmp(IsImmediate:true, InputOverride:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0x3D, () => new Cmp(IsImmediate:true, InputOverride:FromDest(ByteCode.A)).Execute() },

                { 0x50, () => new Push(ByteCode.A).Execute() },
                { 0x51, () => new Push(ByteCode.C).Execute() },
                { 0x52, () => new Push(ByteCode.D).Execute() },
                { 0x53, () => new Push(ByteCode.B).Execute() },
                { 0x54, () => new Push(ByteCode.AH).Execute() },
                { 0x55, () => new Push(ByteCode.CH).Execute() },
                { 0x56, () => new Push(ByteCode.DH).Execute() },
                { 0x57, () => new Push(ByteCode.BH).Execute() },
                { 0x58, () => new Pop(Input:FromDest(ByteCode.A)).Execute() },
                { 0x59, () => new Pop(Input:FromDest(ByteCode.C)).Execute() },
                { 0x5A, () => new Pop(Input:FromDest(ByteCode.D)).Execute() },
                { 0x5B, () => new Pop(Input:FromDest(ByteCode.B)).Execute() },
                { 0x5C, () => new Pop(Input:FromDest(ByteCode.AH)).Execute() },
                { 0x5D, () => new Pop(Input:FromDest(ByteCode.CH)).Execute() },
                { 0x5E, () => new Pop(Input:FromDest(ByteCode.DH)).Execute() },
                { 0x5F, () => new Pop(Input:FromDest(ByteCode.BH)).Execute() },

                //67 prefix
                { 0x68, () => new PushImm(32).Execute() }, // its always 32, weird
                { 0x69, () => new Mul(Oprands:3) { IsSigned=true}.Execute() },
                { 0x6A, () => new PushImm(8).Execute() },
                { 0x6B, () => new Mul(Oprands:3, SignExtendedByte:true) { IsSigned=true }.Execute() },

                { 0x70, () => new Jmp(Relative:true, Bytes:1, Opcode:"JO").Execute() },
                { 0x71, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNO").Execute() },
                { 0x72, () => new Jmp(Relative:true, Bytes:1, Opcode:"JC").Execute() },
                { 0x73, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNC").Execute() },
                { 0x74, () => new Jmp(Relative:true, Bytes:1, Opcode:"JZ").Execute() },
                { 0x75, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNZ").Execute() },
                { 0x76, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNA").Execute() },
                { 0x77, () => new Jmp(Relative:true, Bytes:1, Opcode:"JA").Execute() },
                { 0x78, () => new Jmp(Relative:true, Bytes:1, Opcode:"JS").Execute() },
                { 0x79, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNS").Execute() },
                { 0x7A, () => new Jmp(Relative:true, Bytes:1, Opcode:"JP").Execute() },
                { 0x7B, () => new Jmp(Relative:true, Bytes:1, Opcode:"JNP").Execute() },
                { 0x7C, () => new Jmp(Relative:true, Bytes:1, Opcode:"JL").Execute() },
                { 0x7D, () => new Jmp(Relative:true, Bytes:1, Opcode:"JGE").Execute() },
                { 0x7E, () => new Jmp(Relative:true, Bytes:1, Opcode:"JLE").Execute() },
                { 0x7F, () => new Jmp(Relative:true, Bytes:1, Opcode:"JG").Execute() },

                { 0x80, () => Decode(0x80) },
                { 0x81, () => Decode(0x81) },
                { 0x83, () => Decode(0x83) },
                { 0x88, () => new Mov().Execute() },
                { 0x89, () => new Mov().Execute() },
                { 0x8A, () => new Mov() { IsSwap=true, Is8Bit=true }.Execute() },
                { 0x8B, () => new Mov() { IsSwap=true }.Execute() },
                { 0x8F, () => new Pop(Input:ControlUnit.ModRMDecode()).Execute() },

                { 0xB0, () => new MovImm(Input:FromDest(ByteCode.A)) { Is8Bit=true }.Execute() },
                { 0xB1, () => new MovImm(Input:FromDest(ByteCode.C)) { Is8Bit=true }.Execute() },
                { 0xB2, () => new MovImm(Input:FromDest(ByteCode.D)) { Is8Bit=true }.Execute() },
                { 0xB3, () => new MovImm(Input:FromDest(ByteCode.B)) { Is8Bit=true }.Execute() },
                { 0xB7, () => new MovImm(Input:FromDest(ByteCode.AH)) { Is8Bit=true }.Execute() },
                { 0xB4, () => new MovImm(Input:FromDest(ByteCode.CH)) { Is8Bit=true }.Execute() },
                { 0xB5, () => new MovImm(Input:FromDest(ByteCode.DH)) { Is8Bit=true }.Execute() },
                { 0xB6, () => new MovImm(Input:FromDest(ByteCode.BH)) { Is8Bit=true }.Execute() },
                { 0xB8, () => new MovImm(Input:FromDest(ByteCode.A)).Execute() },
                { 0xB9, () => new MovImm(Input:FromDest(ByteCode.C)).Execute() },
                { 0xBA, () => new MovImm(Input:FromDest(ByteCode.D)).Execute() },
                { 0xBB, () => new MovImm(Input:FromDest(ByteCode.B)).Execute() },
                { 0xBC, () => new MovImm(Input:FromDest(ByteCode.AH)).Execute() },
                { 0xBD, () => new MovImm(Input:FromDest(ByteCode.CH)).Execute() },
                { 0xBE, () => new MovImm(Input:FromDest(ByteCode.DH)).Execute() },
                { 0xBF, () => new MovImm(Input:FromDest(ByteCode.BH)).Execute() },

                { 0xC6, () => new MovImm(Input:ControlUnit.ModRMDecode()) { Is8Bit = true}.Execute() },
                { 0xC7, () => new MovImm(Input:ControlUnit.ModRMDecode()).Execute() },

                { 0xE3, () => new Jmp(Relative:true, Bytes:1, Opcode:"JRCXZ").Execute() },

                { 0xEB, () => new Jmp(Relative:true, Bytes:1).Execute() },
                { 0xE9, () => new Jmp(Relative:true, Bytes:4).Execute() },

                { 0xF6, () => Decode(0xF6) },
                { 0xF7, () => Decode(0xF7) },
                { 0xFF, () => Decode(0xFF) }    
            });
            OpcodeTable.Add(2, new Dictionary<byte, Action>()
            {
                { 0x80, () => new Jmp(Relative:true, Bytes:4, Opcode:"JO").Execute() },
                { 0x81, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNO").Execute() },
                { 0x82, () => new Jmp(Relative:true, Bytes:4, Opcode:"JC").Execute() },
                { 0x83, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNC").Execute() },
                { 0x84, () => new Jmp(Relative:true, Bytes:4, Opcode:"JZ").Execute() },
                { 0x85, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNZ").Execute() },
                { 0x86, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNA").Execute() },
                { 0x87, () => new Jmp(Relative:true, Bytes:4, Opcode:"JA").Execute() },
                { 0x88, () => new Jmp(Relative:true, Bytes:4, Opcode:"JS").Execute() },
                { 0x89, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNS").Execute() },
                { 0x8A, () => new Jmp(Relative:true, Bytes:4, Opcode:"JP").Execute() },
                { 0x8B, () => new Jmp(Relative:true, Bytes:4, Opcode:"JNP").Execute() },
                { 0x8C, () => new Jmp(Relative:true, Bytes:4, Opcode:"JL").Execute() },
                { 0x8D, () => new Jmp(Relative:true, Bytes:4, Opcode:"JGE").Execute() },
                { 0x8E, () => new Jmp(Relative:true, Bytes:4, Opcode:"JLE").Execute() },
                { 0x8F, () => new Jmp(Relative:true, Bytes:4, Opcode:"JG").Execute() },

                { 0xAF, () => new Mul(Oprands:2) { IsSigned=true }.Execute() },
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
        public static Dictionary<byte, Dictionary<int, Action>> MasterDecodeDict = new Dictionary<byte, Dictionary<int, Action>>();
        public static void Decode(byte bOpcode)
        {
            MultiDefOutput = ControlUnit.ModRMDecode(ControlUnit.FetchNext(), true);
            MasterDecodeDict[bOpcode][(byte)MultiDefOutput.lSource].Invoke();
        }
    }

    public class Opcodes
    {
        public abstract class MyOpcode
        {
            protected string _string;

            internal bool Is8Bit = false;
            internal bool IsSwap = false;
            internal bool IsSigned = false;
            internal ModRM _input;
            public override string ToString()
            {
                return _string;
            }

            protected PrefixBytes[] _prefixes = ControlUnit.Prefixes.ToArray();
            public abstract void Execute();
            //public abstract string[] Disassemble(byte? opcode=null);

        }
        public class Mov : MyOpcode
        {
            byte[] baDestData;
            public Mov() {
                _string = "MOV";
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _input = ControlUnit.ModRMDecode(ControlUnit.FetchNext());                
                baDestData = FetchDynamic(_input, IsSwap);
            } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {               
                SetDynamic(_input, baDestData , IsSwap);
            }
        }
        public class MovImm : MyOpcode // b8>= <bb
        {
            //Data [Reg]
            //Prefixes RexW, SIZEOVR
            byte[] baImmediate;
            public MovImm(ModRM Input) {
                _input = Input;
                _string = "MOV";
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                byte _bytesToMove = (byte)((ControlUnit.CurrentCapacity == RegisterCapacity.R) ? 4 : (byte)ControlUnit.CurrentCapacity / 8);
                baImmediate = ControlUnit.FetchNext(_bytesToMove); // NOT immediatefetch32(), we dont sign extend for mov ever
            }
            public override void Execute()
            {    
                SetDynamic(_input, baImmediate);
            }
        }
        public class AddImm : MyOpcode // 05=EAX, 81=any  04=al, 80=8bit
        {
            byte[] baImmediate;
            byte[] baDestBytes;
            byte[] baResult;
            public AddImm(ModRM Input, bool SignExtend=false, bool UseCarry=false)
            {
                _string = (UseCarry) ? "ADC" : "ADD";
                _input = Input;
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                baImmediate = ImmediateFetch(SignExtend);
                baDestBytes = FetchDynamic(_input, true);
                baResult = Bitwise.Add(baDestBytes, baImmediate, ControlUnit.CurrentCapacity, UseCarry);
            }
            public override void Execute()
            {            
                SetDynamic(_input, baResult);
            }
        }
        public class Add : MyOpcode // 01 32bit, 00 8bit,
        {
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            byte[] baSrcData;
            byte[] baDestData;
            byte[] baResult;
            public Add(bool UseCarry=false)
            {
                _string = (UseCarry) ? "ADC" : "ADD";
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _input = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                baSrcData = ControlUnit.FetchRegister((ByteCode)_input.lSource);
                baDestData = FetchDynamic(_input, IsSwap);
                baResult = Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity, UseCarry);
            }
            public override void Execute()
            {                              
                SetDynamic(_input, baResult, IsSwap);
            }
        }
        public class Or : MyOpcode
        {
            byte[] baDestBytes;
            byte[] baSrcBytes;
            byte[] baResult;
            public Or()
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _input = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                _string = "OR";
                baDestBytes = FetchDynamic(_input, IsSwap);
                ByteCode bcSrcReg = (ByteCode)_input.lSource;
                baSrcBytes = ControlUnit.FetchRegister(bcSrcReg);
                baResult = Bitwise.Or(baDestBytes, baSrcBytes);
            }

            public override void Execute()
            {                                            
                SetDynamic(_input, baResult, IsSwap);              
            }
        }
        public class OrImm : MyOpcode
        {
            byte[] baImmediate;
            byte[] baDestBytes;
            public OrImm(ModRM Input, bool SignExtend = false)
            {
                // If no ModRM was decoded in the MultiDefDecoder, it means that we came from the 0x0D instruction, which always has a dest of EAX 
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                baImmediate = ImmediateFetch(SignExtend);
                _string = "OR"; 
                baDestBytes = FetchDynamic(Input, true); // weird at first, but since there is no 2nd byte in single oprand opcodes, actual source is shifted to lDest
                _input = Input;
            }
            public override void Execute()
            {               
                SetDynamic(_input, Bitwise.Or(baDestBytes, baImmediate));
            }
        } 
        public class Push : MyOpcode
        {
            byte[] baResult;
            ByteCode? bcDestReg;
            public Push(ByteCode? _dest = null)
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R; // no 32 bit mode for reg push   
                bcDestReg = _dest; _string = "PUSH";
                if (bcDestReg.HasValue)
                {
                    baResult = ControlUnit.FetchRegister(bcDestReg.Value); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
                }
                else
                {
                    // push [ptr] opcode is from multi def, if this isnt right, we deserve an error
                    baResult = ControlUnit.Fetch(MultiDefOutput.lDest, (int)ControlUnit.CurrentCapacity / 8);
                }
            }

            public override void Execute()
            {                       
                RSP -= (uint)ControlUnit.CurrentCapacity / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases                               
                ControlUnit.SetMemory(RSP, baResult); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
            }
        }
        public class PushImm : MyOpcode
        {
            byte[] baImmediate;
            byte _immcount;
            public PushImm(byte bImmCount)
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R; // no 32 bit mode, default it to 64
                _string = "PUSH";
                baImmediate = ControlUnit.FetchNext(bImmCount);
                _immcount = bImmCount;
            }
            public override void Execute()
            {
                ControlUnit.SetMemory(RSP, baImmediate); // pointer rsp
                RSP -= _immcount; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
            }
        }
        public class Pop : MyOpcode
        {
            byte[] StackData;
            public Pop(ModRM Input)
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R; // no 32 bit mode for reg pop, default it to 64
                _input = Input;
                _string = "POP";
                StackData = ControlUnit.Fetch(RSP, (byte)((int)ControlUnit.CurrentCapacity / 8));
            }
            public override void Execute()
            {
                SetDynamic(_input, StackData); // pointer rsp
                // pop [ptr]0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction
                // so i just point it to the generic pop function cause it isnt special enough
                RSP += (uint)ControlUnit.CurrentCapacity / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases, ADD AFTER ^ IS IMPORTANT
                // this is happens too rarely for it to deserve its own function, we already know the dest is RSP for sure so controlunit.setregister() is a waste of time
            }
        }
        public class Mul : MyOpcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            readonly byte _oprands;
            readonly byte[] baDestData;
            readonly byte[] baResult;
            readonly byte[] baSrcData;
            public Mul(byte Oprands, bool SignExtendedByte=false)
            {
                _string = (IsSigned) ? "IMUL" : "MUL";
                _oprands = Oprands;
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                switch (Oprands)
                {
                    case 1:
                        _input = MultiDefOutput; //lsource is the modrm offset here!
                        baDestData = ControlUnit.FetchRegister(ByteCode.A);
                        break;
                    case 2:
                        _input = ControlUnit.ModRMDecode();
                        baDestData = FetchDynamic(_input);
                        break;
                    case 3:
                        _input = ControlUnit.ModRMDecode();
                        baDestData = ImmediateFetch(SignExtendedByte);                  
                        break;
                    default:
                        throw new Exception();
                }
                baSrcData = FetchDynamic(_input, true); //sign extend here for case 3 anyway, its only one compare, we dont know current cap yet
                baResult = Bitwise.Multiply(baSrcData, Bitwise.SignExtend(baDestData, ControlUnit.CurrentCapacity), ControlUnit.CurrentCapacity, Signed: IsSigned);
            }
            public override void Execute()
            {    
                //careful refactoring this it gets messy, dont think there is a way without losing performance when it isn't needed
                //e.g having to use take,skip with both
                if (_oprands == 1) 
                {                               
                    //little endian smallest first
                    ControlUnit.SetRegister(ByteCode.A, baResult.Take((int)ControlUnit.CurrentCapacity / 8).ToArray()); // higher bytes to d
                    ControlUnit.SetRegister(ByteCode.D, baResult.Skip((int)ControlUnit.CurrentCapacity / 8).ToArray()); // lower to a=
                } else 
                {
                    ControlUnit.SetRegister((ByteCode)_input.lSource, baResult); // higher bytes to d, never a ptr dont use dynamic
                } //copy into source, source isnt source here. choppy workaround in x86_64, because ptr is never dest of imul 2+oprand op, so source and dest are swapped here
            }
        }
        public class Sub : MyOpcode // 29 32bit, 28 8bit,
        {
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            ByteCode bcSrcReg;
            byte[] baSrcData;
            byte[] baDestData;
            byte[] baResult;
            public Sub(bool UseBorrow=false)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _string = (UseBorrow) ? "SBB" : "SUB";
                _input = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                bcSrcReg = (ByteCode)_input.lSource;
                baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                baDestData = FetchDynamic(_input, IsSwap);
                baResult = Bitwise.Subtract(baSrcData, baDestData, ControlUnit.CurrentCapacity, UseBorrow);
            }
            public override void Execute()
            {
                SetDynamic(_input, baResult, IsSwap);
            }
        }
        public class SubImm : MyOpcode // 2c,2d=A ; 80 8bit 81 32bit modrm ; 28, 29 sign extend; 2a 2b swap
        {
            byte[] baImmediate;
            byte[] baDestBytes;
            byte[] baResult;
            public SubImm(ModRM Input, bool SignExtend = false, bool UseBorrow=false)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _string = (UseBorrow) ? "SBB" : "SUB";
                _input = Input;
                baImmediate = ImmediateFetch(SignExtend);
                baDestBytes = FetchDynamic(_input, true);
                baResult = Bitwise.Subtract(baDestBytes, baImmediate, ControlUnit.CurrentCapacity, UseBorrow);
            }
            public override void Execute()
            {          
                SetDynamic(_input, baResult);
            }
        }
        public class Div : MyOpcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            public Div(ModRM Input)
            {
              ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
              _input = Input;
              _string = (IsSigned) ? "IDIV" : "DIV";
            }
            public override void Execute()
            {
                
                byte[] baSrcData = FetchDynamic(_input, true);
                byte[] baDestData = ControlUnit.FetchRegister(ByteCode.A); // always a, atleast for this opcode

                byte[] baDivided = Bitwise.Divide(baDestData, baSrcData, ControlUnit.CurrentCapacity, Signed: IsSigned);
                byte[] baRemainder = Bitwise.Modulo(baDestData, baSrcData, ControlUnit.CurrentCapacity, Signed: IsSigned);

                ControlUnit.SetRegister(ByteCode.A, baDivided); // lower to a              
                ControlUnit.SetRegister(ByteCode.D, baRemainder); // higher bytes to d
                
            }
        }
        public class Cmp : MyOpcode
        {
            readonly byte[] baInput1;
            readonly byte[] baInput2;
            public Cmp(bool IsImmediate=false, bool SignExtendedByte=false, ModRM? InputOverride=null)// feels better and more general than to use IsEAX ?
            {
                _string = "CMP";
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                if(IsImmediate)
                {
                    baInput1 = FetchDynamic((InputOverride ?? MultiDefOutput)); //either the opcode is for eax or its from multidef
                    baInput2 = ImmediateFetch(SignExtendedByte);
                } else
                {
                    ModRM Input = ControlUnit.ModRMDecode();
                    baInput1 = FetchDynamic(Input, !IsSwap); //dont really know why this exists, i guess because the flags other than ZF?
                    baInput2 = FetchDynamic(Input, IsSwap); 
                }
            }

            public override void Execute()
            {
                //basically all cmp does flags-wise is subtract, but doesn't care about the result
                Bitwise.Subtract(baInput1, baInput2, ControlUnit.CurrentCapacity, bCarry:false);
            }
        }
        public class Jmp : MyOpcode
        {
            // rel8, rel32, rel16, absolute r/m64 (absolute r/m32,16 zero extended),  
            readonly ulong DestAddr;
            public Jmp(bool Relative, byte Bytes, string Opcode = "JMP")
            {
                ControlUnit.CurrentCapacity = (RegisterCapacity)(Bytes*8);
                _string = Opcode;
                
                DestAddr = (Relative) ? BitConverter.ToUInt64(Bitwise.SignExtend(ControlUnit.FetchNext(Bytes),8),0) + ControlUnit.BytePointer : ControlUnit.ModRMDecode().lDest;

            }
            public override void Execute()
            {
                switch(_string)
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
                        if(!(_prefixes.Contains(PrefixBytes.ADDR32) && ECX == 0) || !(RCX==0)){ return; } //where jecxz then has none
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
                ControlUnit.BytePointer = DestAddr;
            }
        }
        public class And : MyOpcode
        {
            ModRM DestSrc;
            byte[] baDestBytes;
            byte[] baSrcBytes;
            public And()
            {
                ControlUnit.CurrentCapacity = GetRegCap();
                DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                _string = "AND";
                baDestBytes = FetchDynamic(DestSrc, IsSwap);
                ByteCode bcSrcReg = (ByteCode)DestSrc.lSource;
                baSrcBytes = ControlUnit.FetchRegister(bcSrcReg);
            }

            public override void Execute()
            {
                SetDynamic(DestSrc, Bitwise.And(baDestBytes, baSrcBytes), IsSwap);
            }
        }
        public class AndImm : MyOpcode
        {
            byte[] baImmediate;
            byte[] baDestBytes;
            public AndImm(ModRM Input, bool SignExtend = false)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                baImmediate = ImmediateFetch(SignExtend);
                _string = "AND";
                baDestBytes = FetchDynamic(Input, true);
                _input = Input;
            }
            public override void Execute()
            {
                SetDynamic(_input, Bitwise.And(baDestBytes, baImmediate));
            }
        }
        public class Xor : MyOpcode
        {
            ModRM DestSrc;
            byte[] baDestBytes;
            byte[] baSrcBytes;
            public Xor()
            {
                ControlUnit.CurrentCapacity = GetRegCap();
                DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                _string = "XOR";
                baDestBytes = FetchDynamic(DestSrc, IsSwap);
                ByteCode bcSrcReg = (ByteCode)DestSrc.lSource;
                baSrcBytes = ControlUnit.FetchRegister(bcSrcReg);
            }

            public override void Execute()
            {
                SetDynamic(DestSrc, Bitwise.Xor(baDestBytes, baSrcBytes), IsSwap);
            }
        }
        public class XorImm : MyOpcode
        {
            byte[] baImmediate;
            byte[] baDestBytes;
            public XorImm(ModRM Input, bool SignExtend = false)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                baImmediate = ImmediateFetch(SignExtend);
                _string = "XOR";
                baDestBytes = FetchDynamic(Input, true); 
                _input = Input;
            }
            public override void Execute()
            {
                SetDynamic(_input, Bitwise.Xor(baDestBytes, baImmediate));
            }
        }

        public class Inc : MyOpcode 
        {
            byte[] baDestBytes;
            public Inc(ModRM Input)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _string = "INC";
                _input = Input;
                baDestBytes = FetchDynamic(_input, true);
            }
            public override void Execute()
            {               
                SetDynamic(_input, Bitwise.Increment(baDestBytes, ControlUnit.CurrentCapacity));
            }
        }

        public class Dec : MyOpcode
        {
            byte[] baDestBytes;
            public Dec(ModRM Input)
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                _string = "DEC";
                _input = Input;
                baDestBytes = FetchDynamic(_input, true);
            }
            public override void Execute()
            {
                SetDynamic(_input, Bitwise.Decrement(baDestBytes, ControlUnit.CurrentCapacity));
            }
        }
    }
}