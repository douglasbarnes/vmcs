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
                { 0, () => new AddImm() { Is8Bit=true }.Execute() },
                { 1, () => new OrImm() { Is8Bit=true }.Execute() },
                { 5, () => new SubImm() { Is8Bit=true }.Execute() },
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
                { 0, () => new AddImm().Execute() },
                { 1, () => new OrImm().Execute() },
                { 5, () => new SubImm().Execute() },
            };
            MasterDecodeDict.Add(0x81, _81);

            //0x82 no longer standard x86_64

            //0x83
            Dictionary<int, Action> _83 = new Dictionary<int, Action>
            {
                { 0, () => new AddImm(SignExtend:true).Execute() },
                { 5, () => new SubImm(SignExtend:true).Execute() },
            };
            MasterDecodeDict.Add(0x83, _83);

            //0xf6
            Dictionary<int, Action> _F6 = new Dictionary<int, Action>
            {
                { 4, () => new Mul() { Is8Bit=true } },
                { 6, () => new Div() { Is8Bit=true } }
            };
            MasterDecodeDict.Add(0xF6, _F6);

            //0xf7
            Dictionary<int, Action> _F7 = new Dictionary<int, Action>
            {
                { 4, () => new Mul() },
                { 6, () => new Div() }
            };

            MasterDecodeDict.Add(0xF7, _F7);
            //0xff
            Dictionary<int, Action> _FF = new Dictionary<int, Action>
            {
                { 6, () => new Push() }
            };
            MasterDecodeDict.Add(0xFF, _FF);
        }

        public static void Refresh()
        {
            _addPrefixes();
            _addMultiDefinitionDecoders();
            OpcodeTable.Add(1, new Dictionary<byte, Action>()
            {
                { 0x00, () => new Add() { IsSigned=true }.Execute() },
                { 0x01, () => new Add().Execute() },
                { 0x02, () => new Add() { IsSwap=true }.Execute() },
                { 0x03, () => new Add() { IsSwap=true }.Execute() },
                { 0x04, () => new AddImm(ByteCode.A).Execute() },
                { 0x05, () => new AddImm(ByteCode.A).Execute() },
                { 0x08, () => new Or().Execute() },
                { 0x09, () => new Or().Execute() },
                { 0x0A, () => new Or() { IsSwap=true }.Execute() },
                { 0x0B, () => new Or() { IsSwap=true }.Execute() },
                { 0x0C, () => new OrImm(ByteCode.A).Execute() },
                { 0x0D, () => new OrImm(ByteCode.A).Execute() },

                { 0x28, () => new Sub().Execute() },
                { 0x29, () => new Sub().Execute() },
                { 0x2A, () => new Sub() { IsSwap=true }.Execute() },
                { 0x2B, () => new Sub() { IsSwap=true }.Execute() },
                { 0x2C, () => new SubImm(ByteCode.A).Execute() },
                { 0x2D, () => new SubImm(ByteCode.A).Execute() },

                { 0x50, () => new Push(ByteCode.A).Execute() },
                { 0x51, () => new Push(ByteCode.C).Execute() },
                { 0x52, () => new Push(ByteCode.D ).Execute() },
                { 0x53, () => new Push(ByteCode.B ).Execute() },
                { 0x54, () => new Push(ByteCode.AH).Execute() },
                { 0x55, () => new Push(ByteCode.CH).Execute() },
                { 0x56, () => new Push(ByteCode.DH).Execute() },
                { 0x57, () => new Push(ByteCode.BH).Execute() },
                { 0x58, () => new Pop(ByteCode.A ).Execute() },
                { 0x59, () => new Pop(ByteCode.C ).Execute() },
                { 0x5A, () => new Pop(ByteCode.D ).Execute() },
                { 0x5B, () => new Pop(ByteCode.B ).Execute() },
                { 0x5C, () => new Pop(ByteCode.AH ).Execute() },
                { 0x5D, () => new Pop(ByteCode.CH ).Execute() },
                { 0x5E, () => new Pop(ByteCode.DH ).Execute() },
                { 0x5F, () => new Pop(ByteCode.BH ).Execute() },
                { 0x68, () => new PushImm(32) }, // its always 32, weird
                { 0x6A, () => new PushImm(8) },
                { 0x80, () => Decode(0x80) },
                { 0x81, () => Decode(0x81) },
                { 0x83, () => Decode(0x83) },
                { 0x88, () => new Mov().Execute() },
                { 0x89, () => new Mov().Execute() },
                { 0x8A, () => new Mov() { IsSwap=true }.Execute() },
                { 0x8B, () => new Mov() { IsSwap=true }.Execute() },
                { 0x8F, () => new Pop().Execute() },
                { 0xB0, () => new MovImm(ByteCode.A, 8).Execute() },
                { 0xB1, () => new MovImm(ByteCode.C, 8).Execute() },
                { 0xB2, () => new MovImm(ByteCode.D, 8).Execute() },
                { 0xB3, () => new MovImm(ByteCode.B, 8).Execute() },
                { 0xB4, () => new MovImm(ByteCode.AH, 8).Execute() },
                { 0xB5, () => new MovImm(ByteCode.CH, 8).Execute() },
                { 0xB6, () => new MovImm(ByteCode.DH, 8).Execute() },
                { 0xB7, () => new MovImm(ByteCode.BH, 8).Execute() },
                { 0xB8, () => new MovImm(ByteCode.A,32).Execute() },
                { 0xB9, () => new MovImm(ByteCode.C,32).Execute() },
                { 0xBA, () => new MovImm(ByteCode.D,32).Execute() },
                { 0xBB, () => new MovImm(ByteCode.B,32).Execute() },
                { 0xBC, () => new MovImm(ByteCode.AH,32).Execute() },
                { 0xBD, () => new MovImm(ByteCode.CH,32).Execute() },
                { 0xBE, () => new MovImm(ByteCode.DH,32).Execute() },
                { 0xBF, () => new MovImm(ByteCode.BH,32).Execute() },
                { 0xC6, () => new MovImm((ByteCode)ControlUnit.FetchNext(),8).Execute() },
                { 0xC7, () => new MovImm((ByteCode)ControlUnit.FetchNext(),32).Execute() },
                { 0xFF, () => Decode(0xFF) }    
            });
            OpcodeTable.Add(2, new Dictionary<byte, Action>()
            {

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
            MasterDecodeDict[bOpcode][(byte)MultiDefOutput.lReg].Invoke();
        }
    }

    public class Opcodes
    {

        

        public abstract class Opcode
        {
            protected string _string;

            internal bool Is8Bit = false;
            internal bool IsSwap = false;
            internal bool IsSigned = false;       
           
            public override string ToString()
            {
                return _string;
            }

            protected PrefixBytes[] _prefixes = ControlUnit.Prefixes.ToArray();
            public abstract void Execute();
            //public abstract string[] Disassemble(byte? opcode=null);

        }


        public class Mov : Opcode
        {
            bool _swap;
            bool _is8bit;
            public Mov(bool Swap = false, bool Is8Bit = false) { _is8bit = Is8Bit; _string = "MOV"; _swap = Swap; } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;

                ControlUnit.CurrentCapacity = (_is8bit) ? RegisterCapacity.B : GetRegCap();
                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, ControlUnit.Fetch(lDestAddr));
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, ControlUnit.FetchRegister(bcSrcReg));
                    }
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, ControlUnit.FetchRegister(bcDestReg));
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, ControlUnit.FetchRegister(bcSrcReg));
                    }

                }
            }
        }
        public class MovImm : Opcode // b8>= <bb
        {
            //Data [Reg]
            //Prefixes RexW, SIZEOVR
            ByteCode bcDestReg;
            byte _bitcount; // it makes sense, dont change it
            public MovImm(ByteCode _dest, byte BitCount) { _bitcount = BitCount; bcDestReg = _dest; _string = "MOV"; }
            public override void Execute()
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : (RegisterCapacity)_bitcount;
                byte[] baImmediate = ControlUnit.FetchNext((byte)(_bitcount/8));
                ControlUnit.SetRegister(bcDestReg, baImmediate);
            }
        }
        public class AddImm : Opcode // 05=EAX, 81=any  04=al, 80=8bit
        {
            ByteCode bcDestReg;
            bool _signextend;
            public AddImm(ByteCode? _dest = null, bool SignExtend=false)
            {
                bcDestReg = (_dest.HasValue) ? _dest.Value : (ByteCode)MultiDefOutput.lMod;
                _string = "ADD";
                _signextend = SignExtend;
            }
            public override void Execute()
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();

                byte[] baDestBytes = ControlUnit.FetchRegister(bcDestReg);
                byte[] baImmediate;
                if (_signextend)
                {
                    baImmediate = Bitwise.SignExtend(ControlUnit.FetchNext(2), (byte)ControlUnit.CurrentCapacity);
                }
                else
                {
                    baImmediate = ControlUnit.FetchNext(ControlUnit.CurrentCapacity);
                }

                ControlUnit.SetRegister(bcDestReg, Bitwise.Add(baDestBytes, baImmediate, ControlUnit.CurrentCapacity));
            }
        }
        public class Add : Opcode // 01 32bit, 00 8bit,
        {
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR

            public Add() { _string = "ADD"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());

                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;

                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();

                byte[] baResult;
                byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                byte[] baDestData;
                
                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    baDestData = ControlUnit.Fetch(lDestAddr, ControlUnit.CurrentCapacity);
                    baResult = Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, baResult);
                    }
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    baDestData = ControlUnit.FetchRegister(bcDestReg);
                    baResult = Bitwise.Add(baSrcData, baDestData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, baResult);
                    }
                }
            }
        }
        public class Or : Opcode
        {
            public Or() { _string = "OR"; }

            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ControlUnit.CurrentCapacity = GetRegCap();
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;

                string sResult;
                string sDestBits;
                string sSrcBits;

                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    sDestBits = Bitwise.GetBits(ControlUnit.Fetch(lDestAddr, ControlUnit.CurrentCapacity));
                    sSrcBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcSrcReg));
                    sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, Bitwise.GetBytes(sResult));
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, Bitwise.GetBytes(sResult));
                    }
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    sDestBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcDestReg));
                    sSrcBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcSrcReg));
                    sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, Bitwise.GetBytes(sResult));
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, Bitwise.GetBytes(sResult));
                    }
                }
            }
        }
        public class OrImm : Opcode
        {
            ByteCode? bcDestReg;
            bool SignExtend;
            public OrImm(ByteCode? _dest = null, bool _sign = false) { SignExtend = _sign; bcDestReg = _dest; _string = "OR"; }
            public override void Execute()
            {
                RegisterCapacity _regcap = GetRegCap();
                if (!bcDestReg.HasValue)
                {
                    bcDestReg = (ByteCode)MultiDefOutput.lMod; // why not initialise as this? because it might be uninitialised itself
                }

                // If no ModRM was decoded in the MultiDefDecoder, it means that we came from the 0x0D instruction, which always has a dest of EAX 
                string sDestBits;
                string sSrcBits;
                string sResult;
                byte iCount = (byte)((SignExtend) ? 2 : 4);
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.FetchRegister(bcDestReg.Value, _regcap), 0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchNext(iCount), 0));
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDestReg.Value, Convert.ToUInt64(sResult, 2));
                        break;
                    case RegisterCapacity.E:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchRegister(bcDestReg.Value, _regcap), 0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchNext(iCount), 0));
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDestReg.Value, Convert.ToUInt32(sResult, 2));
                        break;
                    case RegisterCapacity.X:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchRegister(bcDestReg.Value, _regcap), 0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchNext(2), 0));
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDestReg.Value, Convert.ToUInt16(sResult, 2));
                        break;
                    case RegisterCapacity.B:
                        sDestBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcDestReg.Value, _regcap)[0]); // base 2
                        sSrcBits = Convert.ToString(ControlUnit.FetchNext(), 2);
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDestReg.Value, Convert.ToByte(sResult, 2));
                        break;
                }
            }
        } //
        public class Push : Opcode
        {
            ByteCode? bcDestReg;
            public Push(ByteCode? _dest = null) { bcDestReg = _dest; _string = "PUSH"; }

            public override void Execute()
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R; // no 32 bit mode for reg push           
                RSP -= (uint)ControlUnit.CurrentCapacity / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
                byte[] baResult;
                if (bcDestReg.HasValue)
                {
                    baResult = ControlUnit.FetchRegister(bcDestReg.Value); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
                }
                else
                {
                    // push [ptr] opcode is from multi def, if this isnt right, we deserve an error
                    baResult = ControlUnit.Fetch(MultiDefOutput.lMod, (int)ControlUnit.CurrentCapacity / 8);
                }
                ControlUnit.SetMemory(RSP, baResult); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
            }
        }
        public class PushImm : Opcode
        {
            byte ImmediateCount;
            public PushImm(byte bImmCount) { ImmediateCount = bImmCount; _string = "PUSH"; }
            public override void Execute()
            {
                ControlUnit.SetMemory(RSP, ControlUnit.FetchNext(ImmediateCount)); // pointer rsp
                RSP -= ImmediateCount; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
            }
        }
        public class Pop : Opcode
        {
            ByteCode? bcDestReg;
            public Pop(ByteCode? _dest = null) { bcDestReg = _dest; _string = "POP"; }
            public override void Execute()
            {
                ControlUnit.CurrentCapacity = (_prefixes.Contains(PrefixBytes.SIZEOVR)) ? RegisterCapacity.X : RegisterCapacity.R; // no 32 bit mode for reg pop, default it to 64
                if (bcDestReg.HasValue)
                {
                    ControlUnit.SetRegister(bcDestReg.Value, ControlUnit.Fetch(RSP)); // pointer rsp
                }
                else
                {
                    // pop [ptr]0x8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction
                    // so i just point it to the generic pop function cause it isnt special enough
                    ControlUnit.SetMemory(MultiDefOutput.lMod, ControlUnit.Fetch(RSP, (byte)((int)ControlUnit.CurrentCapacity / 8)));
                }
                RSP += (uint)ControlUnit.CurrentCapacity / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases, ADD AFTER ^ IS IMPORTANT
                // this is happens too rarely for it to deserve its own function, we already know the dest is RSP for sure so controlunit.setregister() is a waste of time
            }
        }
        public class Mul : Opcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            public Mul() { _string = "MUL"; }
            public override void Execute()
            {
                ModRM DestSrc = MultiDefOutput;
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();             
                byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                byte[] baDestData = ControlUnit.FetchRegister(ByteCode.A); // always a
                byte[] baResult = Bitwise.Multiply(baSrcData, baDestData, ControlUnit.CurrentCapacity, Signed:IsSigned);

                int iHalfCapLen = (int)ControlUnit.CurrentCapacity / 8 /  2;
                if (baResult.Take(iHalfCapLen) == Bitwise.Zero.Take(iHalfCapLen)){ Eflags.Carry = false; Eflags.Overflow = false; } // if highest x == 00000 set flags(little endian)
                                                                            else { Eflags.Carry = true; Eflags.Overflow = true; }
                ControlUnit.SetRegister(ByteCode.D, baResult.Take(iHalfCapLen).ToArray()); // higher bytes to d
                ControlUnit.SetRegister(ByteCode.A, baResult.Skip(iHalfCapLen).ToArray()); // lower to a
            }
        }

        public class Sub : Opcode // 29 32bit, 28 8bit,
        {
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR

            public Sub() { _string = "SUB"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());

                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;

                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();

                byte[] baResult;
                byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                byte[] baDestData;

                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    baDestData = ControlUnit.Fetch(lDestAddr, ControlUnit.CurrentCapacity);
                    baResult = Bitwise.Subtract(baDestData, baSrcData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, baResult);
                    }
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    baDestData = ControlUnit.FetchRegister(bcDestReg);
                    baResult = Bitwise.Add(baDestData, baSrcData, ControlUnit.CurrentCapacity);
                    if (IsSwap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult);
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, baResult);
                    }
                }
            }
        }

        public class SubImm : Opcode // 2c,2d=A ; 80 8bit 81 32bit modrm ; 28, 29 sign extend; 2a 2b swap
        {
            ByteCode bcDestReg;
            bool _signextend;
            public SubImm(ByteCode? _dest = null, bool SignExtend = false)
            {
                bcDestReg = (_dest.HasValue) ? _dest.Value : (ByteCode)MultiDefOutput.lMod;
                _string = "SUB";
                _signextend = SignExtend;
            }
            public override void Execute()
            {
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();

                byte[] baDestBytes = ControlUnit.FetchRegister(bcDestReg);
                byte[] baImmediate;
                if (_signextend)
                {
                    baImmediate = Bitwise.SignExtend(ControlUnit.FetchNext(2), (byte)ControlUnit.CurrentCapacity);
                }
                else
                {
                    baImmediate = ControlUnit.FetchNext(ControlUnit.CurrentCapacity);
                }

                ControlUnit.SetRegister(bcDestReg, Bitwise.Subtract(baDestBytes, baImmediate, ControlUnit.CurrentCapacity));
            }
        }
        public class Div : Opcode // f6,f7
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            public Div() { _string = "DIV"; }
            public override void Execute()
            {
                ModRM DestSrc = MultiDefOutput;
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                ControlUnit.CurrentCapacity = (Is8Bit) ? RegisterCapacity.B : GetRegCap();
                byte[] baSrcData = ControlUnit.FetchRegister(bcSrcReg);
                byte[] baDestData = ControlUnit.FetchRegister(ByteCode.A); // always a

                byte[] baDivided = Bitwise.Divide(baDestData, baSrcData, ControlUnit.CurrentCapacity, Signed: IsSigned);
                byte[] baRemainder = Bitwise.Modulo(baDestData, baSrcData, ControlUnit.CurrentCapacity, Signed: IsSigned);

                ControlUnit.SetRegister(ByteCode.A, baDivided); // lower to a              
                ControlUnit.SetRegister(ByteCode.D, baRemainder); // higher bytes to d
                
            }
        }
    }
}