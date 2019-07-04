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
                { 0, () => new AddImm8().Execute() },
                { 1, () => new OrImm8().Execute() }
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
                { 0, () => new AddImm32().Execute() },
                { 1, () => new OrImm32().Execute() }
            };
            MasterDecodeDict.Add(0x81, _81);

            //0x82 no longer standard x86_64

            //0x83
            Dictionary<int, Action> _83 = new Dictionary<int, Action>
            {
                { 0, () => new AddImm8Extended().Execute() }
            };
            MasterDecodeDict.Add(0x83, _83);

            //0xf6
            Dictionary<int, Action> _F6 = new Dictionary<int, Action>
            {
                { 5, () => new Imul8(new byte[] { (byte)ByteCode.A }) }
            };
            MasterDecodeDict.Add(0xF6, _F6);

            //0xf7
            Dictionary<int, Action> _F7 = new Dictionary<int, Action>
            {
                { 5, () => new Imul32() }
            };

            MasterDecodeDict.Add(0xF7, _F7);
            //0xff
            Dictionary<int, Action> _FF = new Dictionary<int, Action>
            {
                { 6, () => new PushRM() }
            };
            MasterDecodeDict.Add(0xFF, _FF);
        }

        public static void Refresh()
        {
            _addPrefixes();
            _addMultiDefinitionDecoders();
            OpcodeTable.Add(1, new Dictionary<byte, Action>()
            {
                { 0x00, () => new Add8().Execute() },
                { 0x01, () => new Add32().Execute() },
                { 0x02, () => new Add8(Swap:true).Execute() },
                { 0x03, () => new Add32(Swap:true).Execute() },
                { 0x04, () => new AddImm8(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0x05, () => new AddImm32(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0x08, () => new Or8().Execute() },
                { 0x09, () => new Or32().Execute() },
                { 0x08, () => new Or8(Swap:true).Execute() },
                { 0x09, () => new Or32(Swap:true).Execute() },
                { 0x0C, () => new OrImm8(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0x0D, () => new OrImm32(new byte[] { (byte)ByteCode.A }).Execute() },

                { 0x50, () => new Push(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0x51, () => new Push(new byte[] { (byte)ByteCode.C }).Execute() },
                { 0x52, () => new Push(new byte[] { (byte)ByteCode.D }).Execute() },
                { 0x53, () => new Push(new byte[] { (byte)ByteCode.B }).Execute() },
                { 0x54, () => new Push(new byte[] { (byte)ByteCode.AH }).Execute() },
                { 0x55, () => new Push(new byte[] { (byte)ByteCode.CH }).Execute() },
                { 0x56, () => new Push(new byte[] { (byte)ByteCode.DH }).Execute() },
                { 0x57, () => new Push(new byte[] { (byte)ByteCode.BH }).Execute() },
                { 0x58, () => new Pop(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0x59, () => new Pop(new byte[] { (byte)ByteCode.C }).Execute() },
                { 0x5A, () => new Pop(new byte[] { (byte)ByteCode.D }).Execute() },
                { 0x5B, () => new Pop(new byte[] { (byte)ByteCode.B }).Execute() },
                { 0x5C, () => new Pop(new byte[] { (byte)ByteCode.AH }).Execute() },
                { 0x5D, () => new Pop(new byte[] { (byte)ByteCode.CH }).Execute() },
                { 0x5E, () => new Pop(new byte[] { (byte)ByteCode.DH }).Execute() },
                { 0x5F, () => new Pop(new byte[] { (byte)ByteCode.BH }).Execute() },
                { 0x68, () => new PushImm32() },
                { 0x6A, () => new PushImm16() },
                { 0x80, () => Decode(0x80) },
                { 0x81, () => Decode(0x81) },
                { 0x83, () => Decode(0x83) },
                { 0x88, () => new Mov8().Execute() },
                { 0x89, () => new Mov32().Execute() },
                { 0x8A, () => new Mov8(Swap:true).Execute() },
                { 0x8B, () => new Mov32(Swap:true).Execute() },
                { 0x8F, () => new PopRM().Execute() },
                { 0xB0, () => new MovImm8(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0xB1, () => new MovImm8(new byte[] { (byte)ByteCode.C }).Execute() },
                { 0xB2, () => new MovImm8(new byte[] { (byte)ByteCode.D }).Execute() },
                { 0xB3, () => new MovImm8(new byte[] { (byte)ByteCode.B }).Execute() },
                { 0xB4, () => new MovImm8(new byte[] { (byte)ByteCode.AH }).Execute() },
                { 0xB5, () => new MovImm8(new byte[] { (byte)ByteCode.CH }).Execute() },
                { 0xB6, () => new MovImm8(new byte[] { (byte)ByteCode.DH }).Execute() },
                { 0xB7, () => new MovImm8(new byte[] { (byte)ByteCode.BH }).Execute() },
                { 0xB8, () => new MovImm32(new byte[] { (byte)ByteCode.A }).Execute() },
                { 0xB9, () => new MovImm32(new byte[] { (byte)ByteCode.C }).Execute() },
                { 0xBA, () => new MovImm32(new byte[] { (byte)ByteCode.D }).Execute() },
                { 0xBB, () => new MovImm32(new byte[] { (byte)ByteCode.B }).Execute() },
                { 0xC6, () => new MovImm32(new byte[] { ControlUnit.FetchNext() }).Execute() },
                { 0xC7, () => new MovImm32(new byte[] { ControlUnit.FetchNext() }).Execute() },
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
            protected RegisterCapacity _regcap;
            public override string ToString()
            {
                return _string;
            }
            protected byte[] _data;
            protected PrefixBytes[] _prefixes = ControlUnit.Prefixes.ToArray();
            public abstract void Execute();
            //public abstract string[] Disassemble(byte? opcode=null);

        }

        public class Mov8 : Opcode
        {
            bool _swap;
            public Mov8(bool Swap = false) { _string = "MOV"; _swap = Swap; } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {
                _regcap = RegisterCapacity.B;
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                if (_swap)
                {
                    if (DestSrc.IsAddress)
                    {
                        ulong lDestAddr = DestSrc.lMod;
                        ControlUnit.SetMemory(lDestAddr, ControlUnit.FetchRegister(bcSrcReg, _regcap));
                    }
                    else
                    {
                        ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                        MoveReg(bcDestReg, bcSrcReg, RegisterCapacity.B);
                    }
                } else
                {
                    if (DestSrc.IsAddress)
                    {
                        ulong lDestAddr = DestSrc.lMod;
                        ControlUnit.SetMemory(lDestAddr, ControlUnit.FetchRegister(bcSrcReg, _regcap));
                    }
                    else
                    {
                        ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                        MoveReg(bcDestReg, bcSrcReg, RegisterCapacity.B);
                    }
                }

                
                

            }
        }

        public class Mov32 : Opcode
        {
            bool _swap;
            public Mov32(bool Swap = false) { _string = "MOV"; _swap = Swap; } // 0x88 ACCEPTS MOV R/M8, R8
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;

                _regcap = GetRegCap();
                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, ControlUnit.Fetch(lDestAddr, _regcap), _regcap);                      
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, ControlUnit.FetchRegister(bcSrcReg, _regcap));
                    }                  
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    if (_swap) 
                    {
                        MoveReg(bcSrcReg, bcDestReg, _regcap);
                    }
                    else
                    {
                        MoveReg(bcDestReg, bcSrcReg, _regcap);
                    }
                    
                }
            }
        }

        public class MovImm32 : Opcode // b8>= <bb
        {
            //Data [Reg]
            //Prefixes RexW, SIZEOVR
            public MovImm32(byte[] baInputData) { _data = baInputData; _string = "MOV"; }
            public override void Execute()
            {
                RegisterCapacity _regcap = GetRegCap();
                ByteCode bcDestReg = (ByteCode)_data[0];
                byte bBytestoFetch = (byte)((_regcap == RegisterCapacity.X) ? 2 : 4);
                byte[] baImmediate = Bitwise.ZeroExtend(ControlUnit.FetchNext(bBytestoFetch),64);
                ControlUnit.SetRegister(bcDestReg, baImmediate, RegisterCapacity.R);
            }
        }

        public class MovImm8 : Opcode //  b0>= <b8
        {
            //Data [Reg]
            public MovImm8(byte[] baInputData) { _data = baInputData; _string = "MOV"; }

            public override void Execute()
            {
                byte bImmediate8 = ControlUnit.FetchNext();
                ByteCode bcDestReg = (ByteCode)_data[0];
                ControlUnit.SetRegister(bcDestReg, bImmediate8);
            }
        }

        public class AddImm8 : Opcode // 04=AL, 80
        {
            public AddImm8(byte[] baInputData = null) { _data = baInputData; _string = "ADD"; }
            public override void Execute()
            {

                _regcap = RegisterCapacity.B;
                ByteCode bcDest = (_data == null) ? (ByteCode)MultiDefOutput.lMod : (ByteCode)_data[0];
                /*if (_data == null) // if opcode was 80 same as ^^
                {
                    bcDest = (ByteCode)MultiDefOutput.lMod;
                }
                else //otherwise use provided bytecode
                {
                    bcDest = (ByteCode)_data[0];
                }*/

                byte bImmediate8 = ControlUnit.FetchNext();
                byte bDestVal = ControlUnit.FetchRegister(bcDest, _regcap)[0];

                ControlUnit.SetRegister(bcDest, (byte)(bImmediate8 + bDestVal));
            }
        }

        public class AddImm32 : Opcode // 05=EAX, 81=any
        {
            public AddImm32(byte[] baInputData=null) { _data = baInputData; _string = "ADD"; }
            public override void Execute()
            {
                ByteCode bcDest = (_data == null) ? (ByteCode)MultiDefOutput.lMod : (ByteCode)_data[0];
                RegisterCapacity _regcap = GetRegCap();

                byte[] baDestBytes = ControlUnit.FetchRegister(bcDest, _regcap);
                byte[] baImmediate;
                switch (_regcap)
                {
                    case RegisterCapacity.E:

                        baImmediate = ControlUnit.FetchNext(4);
                        uint iImm = BitConverter.ToUInt32(baImmediate, 0);
                        uint iDest = BitConverter.ToUInt32(baDestBytes, 0);
                        ControlUnit.SetRegister(bcDest, BitConverter.GetBytes(iImm + iDest), _regcap);
                        break;

                    case RegisterCapacity.R:
                        baImmediate = ControlUnit.FetchNext(8);
                        ulong lImm = BitConverter.ToUInt64(baImmediate, 0);
                        ulong lDest = BitConverter.ToUInt64(baDestBytes, 0);
                        ControlUnit.SetRegister(bcDest, BitConverter.GetBytes(lImm + lDest), _regcap);
                        break;
                    case RegisterCapacity.X:
                        baImmediate = ControlUnit.FetchNext(2);
                        ushort sImm = BitConverter.ToUInt16(baImmediate, 0);
                        ushort sDest = BitConverter.ToUInt16(baDestBytes, 0);
                        ControlUnit.SetRegister(bcDest, BitConverter.GetBytes((ushort)(sImm + sDest)), _regcap);
                        break;
                }



            }
        }

        public class AddImm8Extended : Opcode // 0x83
        {
            // Prefixes REXW SIZEOVR
            public AddImm8Extended() { _string = "ADDE"; }

            public override void Execute()
            {
                RegisterCapacity _regcap = GetRegCap();

                ByteCode bcDest = (ByteCode)MultiDefOutput.lMod;
                byte[] baDestBytes = ControlUnit.FetchRegister(bcDest, _regcap);

                byte bImmediate8 = ControlUnit.FetchNext();
                byte[] baResult = null;
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        //ulong lMask = 0xFFFFFFFFFFFFFF00;
                        ulong lDest = BitConverter.ToUInt64(baDestBytes, 0);
                        baResult = BitConverter.GetBytes(lDest + bImmediate8);
                        break;
                    case RegisterCapacity.E:
                        //uint iMask = 0xFFFFFF00;
                        uint iDest = BitConverter.ToUInt32(baDestBytes, 0);
                        baResult = BitConverter.GetBytes(iDest + bImmediate8);
                        break;
                    case RegisterCapacity.X:
                        //ushort sMask = 0xFF00;
                        ushort sDest = BitConverter.ToUInt16(baDestBytes, 0);
                        baResult = BitConverter.GetBytes(sDest + bImmediate8);
                        break;
                }
                ControlUnit.SetRegister(bcDest, Bitwise.ZeroExtend(baResult, (byte)_regcap), _regcap);
            }
        }

        public class Add8 : Opcode // 00
        {
            bool _swap;
            //Data [Dest, Src]
            public Add8(bool Swap = false) { _swap = Swap; _string = "ADD"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                _regcap = RegisterCapacity.B;

                if (_swap)
                {
                    if (DestSrc.IsAddress)
                    {
                        ulong lDestAddr = DestSrc.lMod;
                        byte bSrc = ControlUnit.FetchRegister(bcSrcReg, _regcap)[0];
                        byte bDest = ControlUnit.Fetch(lDestAddr)[0];
                        ControlUnit.SetMemory(lDestAddr, (byte)(bSrc + bDest));
                    }
                    else
                    {
                        ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                        byte bSrc = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                        byte bDest = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                        ControlUnit.SetRegister(bcDestReg, (byte)(bSrc + bDest));
                    }
                }
                else
                {
                    if (DestSrc.IsAddress)
                    {
                        ulong lDestAddr = DestSrc.lMod;
                        byte bSrc = ControlUnit.FetchRegister(bcSrcReg, _regcap)[0];
                        byte bDest = ControlUnit.Fetch(lDestAddr)[0];
                        ControlUnit.SetRegister(bcSrcReg, (byte)(bSrc + bDest));
                    }
                    else
                    {
                        ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                        byte bSrc = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                        byte bDest = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                        ControlUnit.SetRegister(bcSrcReg, (byte)(bSrc + bDest));
                    }
                }

                

                
            }
        }

        public class Add32 : Opcode // 01
        {
            //Data [Dest, Src]
            //Prefixes REXW SIZEOVR
            bool _swap;
            public Add32(bool Swap = false) { _swap = Swap; _string = "ADD"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());

                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                _regcap = GetRegCap();

                byte[] baResult = null;
                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;

                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            ulong lSrc = BitConverter.ToUInt64(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            ulong lDest = BitConverter.ToUInt64(ControlUnit.Fetch(lDestAddr, 8), 0);
                            baResult = BitConverter.GetBytes(lSrc + lDest);
                            break;
                        case RegisterCapacity.E:
                            uint iSrc = BitConverter.ToUInt32(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            uint iDest = BitConverter.ToUInt32(ControlUnit.Fetch(lDestAddr, 8), 0);
                            baResult = BitConverter.GetBytes(iSrc + iDest);
                            break;
                        case RegisterCapacity.X:
                            ushort sSrc = BitConverter.ToUInt16(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            ushort sDest = BitConverter.ToUInt16(ControlUnit.Fetch(lDestAddr, 8), 0);
                            baResult = BitConverter.GetBytes(sSrc + sDest);

                            break;
                    }

                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult, _regcap);
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, baResult);
                    }                  
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            ulong lSrc = BitConverter.ToUInt64(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            ulong lDest = BitConverter.ToUInt64(ControlUnit.FetchRegister(bcDestReg, _regcap), 0);
                            baResult = BitConverter.GetBytes(lSrc + lDest);
                            break;
                        case RegisterCapacity.E:
                            uint iSrc = BitConverter.ToUInt32(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            uint iDest = BitConverter.ToUInt32(ControlUnit.FetchRegister(bcDestReg, _regcap), 0);
                            baResult = BitConverter.GetBytes(iSrc + iDest);
                            break;
                        case RegisterCapacity.X:
                            ushort sSrc = BitConverter.ToUInt16(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                            ushort sDest = BitConverter.ToUInt16(ControlUnit.FetchRegister(bcDestReg, _regcap), 0);
                            baResult = BitConverter.GetBytes(sSrc + sDest);
                            break;
                    }

                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrcReg, baResult, _regcap);
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDestReg, baResult, _regcap);
                    }
                    
                }
            }
        }

        public class Or8 : Opcode
        {
            bool _swap;
            public Or8(bool Swap = false) { _swap = Swap; _string = "OR"; }

            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                _regcap = RegisterCapacity.B;

                ByteCode bcDest;
                ulong lDestAddr;
                string sResult;                
                string sDestBits;
                ByteCode bcSrc = (ByteCode)DestSrc.lReg;
                string sSrcBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcSrc, _regcap)[0]);

                if (DestSrc.IsAddress)
                {
                    lDestAddr = DestSrc.lMod;
                    sDestBits = Bitwise.GetBits(ControlUnit.Fetch(lDestAddr)[0]);
                    sResult = Bitwise.LogicalOr(sDestBits,sSrcBits);
                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrc, Convert.ToByte(sResult, 2));
                    }
                    else
                    {
                        ControlUnit.SetMemory(lDestAddr, Convert.ToByte(sResult, 2));
                    }
                }
                else
                {
                    bcDest = (ByteCode)DestSrc.lMod;
                    sDestBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcDest, _regcap)[0]);
                    sSrcBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcSrc, _regcap)[0]);
                    sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                    if (_swap)
                    {
                        ControlUnit.SetRegister(bcSrc, Convert.ToByte(sResult, 2));
                    }
                    else
                    {
                        ControlUnit.SetRegister(bcDest, Convert.ToByte(sResult, 2));
                    }
                }
            }
        }

        public class Or32 : Opcode
        {
            bool _swap;
            public Or32(bool Swap = false) { _swap = Swap; _string = "OR"; }

            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());

                _regcap = GetRegCap();

                ByteCode bcDest;
                ByteCode bcSrc = (ByteCode)DestSrc.lReg;
                ulong lDestAddr;

                string sResult;
                string sDestBits;
                string sSrcBits;

                if (DestSrc.IsAddress)
                {
                    lDestAddr = DestSrc.lMod;
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.Fetch(lDestAddr, 8), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                            if(_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt64(sResult, 2));
                            } else
                            {
                                ControlUnit.SetMemory(lDestAddr, BitConverter.GetBytes(Convert.ToUInt64(sResult, 2)));
                            }
                            
                            break;
                        case RegisterCapacity.E:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.Fetch(lDestAddr, 8), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                            if (_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt32(sResult, 2));
                            }
                            else
                            {
                                ControlUnit.SetMemory(lDestAddr, BitConverter.GetBytes(Convert.ToUInt32(sResult, 2)));
                            }
                            break;
                        case RegisterCapacity.X:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.Fetch(lDestAddr, 8), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);                           
                            if (_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt16(sResult, 2));
                            }
                            else
                            {
                                ControlUnit.SetMemory(lDestAddr, Convert.ToUInt16(sResult, 2));
                            }
                            break;
                    }

                }
                else
                {
                    bcDest = (ByteCode)DestSrc.lMod;
                    switch (_regcap)
                    {
                        case RegisterCapacity.R:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.FetchRegister(bcDest, _regcap), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                            if(_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt64(sResult, 2));
                            }
                            else
                            {
                                ControlUnit.SetRegister(bcDest, Convert.ToUInt64(sResult, 2));
                            }
                            
                            break;
                        case RegisterCapacity.E:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchRegister(bcDest, _regcap), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                            
                            if (_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt32(sResult, 2));
                            } else
                            {
                                ControlUnit.SetRegister(bcDest, Convert.ToUInt32(sResult, 2));
                            }
                            break;
                        case RegisterCapacity.X:
                            sDestBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchRegister(bcDest, _regcap), 0));
                            sSrcBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchRegister(bcSrc, _regcap), 0));
                            sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                            if(_swap)
                            {
                                ControlUnit.SetRegister(bcSrc, Convert.ToUInt16(sResult, 2));
                            } else
                            {
                                ControlUnit.SetRegister(bcDest, Convert.ToUInt16(sResult, 2));
                            }
                            break;
                    }
                }
            }
        }

        public class OrImm8 : Opcode
        {
            public OrImm8(byte[] baInputData = null) { _data = baInputData; _string = "OR"; }

            public override void Execute()
            {
                ByteCode bcDest;

                if (_data == null)
                {
                    bcDest = (ByteCode)ControlUnit.FetchNext();

                }
                else
                {
                    bcDest = (ByteCode)_data[0];
                }

                byte bSrcVal = ControlUnit.FetchNext();
                string sDestBits = Bitwise.GetBits(ControlUnit.FetchRegister(bcDest, _regcap)[0]); // base 2
                string sSrcBits = Convert.ToString(bSrcVal, 2);
                string sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                
                ControlUnit.SetRegister(bcDest, Convert.ToByte(sResult, 2));
            }
        }

        public class OrImm32 : Opcode
        {
            public OrImm32(byte[] baInputData=null) { _data = baInputData; _string = "OR"; }
            public override void Execute()
            {
                ByteCode bcDest = (_data == null) ? (ByteCode)MultiDefOutput.lMod : (ByteCode)_data[0];
                RegisterCapacity _regcap = GetRegCap();
                 // If no ModRM was decoded in the MultiDefDecoder, it means that we came from the 0x0D instruction, which always has a dest of EAX 
                string sDestBits;
                string sSrcBits;
                string sResult;
                switch (_regcap)
                {
                    case RegisterCapacity.R:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt64(ControlUnit.FetchRegister(bcDest, _regcap),0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0)).PadLeft(64, '1'); // mask fffffffffffffffff
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits).PadLeft(64,'0');
                        ControlUnit.SetRegister(bcDest, Convert.ToUInt64(sResult, 2));
                        break;
                    case RegisterCapacity.E:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchRegister(bcDest, _regcap), 0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0));
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDest, Convert.ToUInt32(sResult, 2));
                        break;
                    case RegisterCapacity.X:
                        sDestBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchRegister(bcDest, _regcap), 0));
                        sSrcBits = Bitwise.GetBits(BitConverter.ToUInt16(ControlUnit.FetchNext(2),0));
                        sResult = Bitwise.LogicalOr(sDestBits, sSrcBits);
                        ControlUnit.SetRegister(bcDest, Convert.ToUInt16(sResult, 2));
                        break;
                }
            }
        }

        public class Push : Opcode
        {
            public Push(byte[] baInputData) { _data = baInputData; _string = "PUSH"; }

            public override void Execute()
            {
                ByteCode bcDest = (ByteCode)_data[0];
                _regcap = GetRegCap(RegisterCapacity.R); // no 32 bit mode for reg push
                RSP -= (uint)_regcap / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
                ControlUnit.SetMemory(RSP, ControlUnit.FetchRegister(bcDest, _regcap)); // pointer rsp ^ IMPORTNAT THIS IS BEFORE
                
            }
        }

        public class PushImm16 : Opcode
        {
            public PushImm16() { _string = "PUSH"; }
            public override void Execute()
            {
                byte[] ImmediateData = Bitwise.ZeroExtend(ControlUnit.FetchNext(2), 8);
                ControlUnit.SetMemory(RSP, ImmediateData); // pointer rsp
                RSP -= 2; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
            }
        }

        public class PushImm32 : Opcode
        {
            public PushImm32() { _string = "PUSH"; }
            public override void Execute()
            {
                byte[] ImmediateData = Bitwise.ZeroExtend(ControlUnit.FetchNext(4),8);
                ControlUnit.SetMemory(RSP, ImmediateData); // pointer rsp
                RSP -= 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
            }
        }

        public class PushRM : Opcode
        {
            public PushRM() { _string = "PUSH"; }
            public override void Execute()
            {
                _regcap = GetRegCap(RegisterCapacity.R);
                ulong lPointer = MultiDefOutput.lMod;
                ControlUnit.SetMemory(RSP, ControlUnit.Fetch(lPointer, (byte)((int)_regcap / 8)));
                RSP -= 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases
            }

            
        }

        public class Pop : Opcode
        {
            public Pop(byte[] baInputData) { _data = baInputData; _string = "POP"; }
            public override void Execute()
            {
                ByteCode bcDest = (ByteCode)_data[0];
                _regcap = GetRegCap(RegisterCapacity.R); // no 32 bit mode for reg pop
                ControlUnit.SetRegister(bcDest, ControlUnit.Fetch(RSP, _regcap), _regcap); // pointer rsp
                RSP += (uint)_regcap / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases, ADD AFTER ^ IS IMPORTANT
            }
        }

        public class PopRM : Opcode
        {
            public PopRM() { _string = "POP"; }
            public override void Execute()
            {
                _regcap = GetRegCap(RegisterCapacity.R); // no 32 bit mode for reg pop
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext(), true); // 8F technichally is a multi def byte because it has 1 oprand, but there is only this instruction
                ulong lPointer = DestSrc.lMod;                                          //associated with it so we dont care, its faster if we skip that part..
                ControlUnit.SetMemory(lPointer, ControlUnit.Fetch(RSP, (byte)((int)_regcap / 8))); // pointer rsp
                RSP += (uint)_regcap / 8; // cheap way to add to rsp, use full ControlUnit.setreg in other cases, ADD AFTER ^ IS IMPORTANT
            }
        }

        public class Imul8 : Opcode // 00
        {
            bool _swap;
            //Data [Dest, Src]
            public Imul8(byte[] baInputData=null) { _data = baInputData; _string = "ADD"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcSrcReg;
                _regcap = RegisterCapacity.B;

                if (_data == null)
                {
                    bcSrcReg = (ByteCode)DestSrc.lReg;
                }
                else
                {
                    bcSrcReg = (ByteCode)_data[0];
                }

                if (DestSrc.IsAddress)
                {
                    ulong lDestAddr = DestSrc.lMod;
                    byte bSrc = ControlUnit.FetchRegister(bcSrcReg, _regcap)[0];
                    byte bDest = ControlUnit.Fetch(lDestAddr)[0];
                    ControlUnit.SetRegister(bcSrcReg, (byte)(bSrc + bDest));
                }
                else
                {
                    ByteCode bcDestReg = (ByteCode)DestSrc.lMod;
                    byte bSrc = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                    byte bDest = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                    ControlUnit.SetRegister(bcSrcReg, (byte)(bSrc + bDest));
                }
                




            }
        }

        public class Imul32 : Opcode // 01
        {
            //Data [Dest, Src]
            // or [eax, src]
            //Prefixes REXW SIZEOVR
            bool _swap;
            public Imul32(byte[] baInputData=null) { _data = baInputData; _string = "ADD"; }
            public override void Execute()
            {
                ModRM DestSrc = ControlUnit.ModRMDecode(ControlUnit.FetchNext());
                ByteCode bcDestReg = (ByteCode?)_data[0] ?? (ByteCode)DestSrc.lMod;
                ByteCode bcSrcReg = (ByteCode)DestSrc.lReg;
                _regcap = GetRegCap();

                byte[] baResult;
                switch(_regcap)
                {
                    case RegisterCapacity.R:
                        ulong lSrcVal = BitConverter.ToUInt64(ControlUnit.FetchRegister(bcSrcReg, _regcap),0);
                        ulong lDstVal = BitConverter.ToUInt64(ControlUnit.FetchRegister(bcDestReg, _regcap),0);
                        baResult = BitConverter.GetBytes(lSrcVal * lDstVal);
                        break;
                    case RegisterCapacity.E:
                        uint iSrcVal = BitConverter.ToUInt32(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                        uint iDstVal = BitConverter.ToUInt32(ControlUnit.FetchRegister(bcDestReg, _regcap), 0);
                        baResult = BitConverter.GetBytes(iSrcVal * iDstVal);
                        break;
                    case RegisterCapacity.X:
                        ushort sSrcVal = BitConverter.ToUInt16(ControlUnit.FetchRegister(bcSrcReg, _regcap), 0);
                        ushort sDstVal = BitConverter.ToUInt16(ControlUnit.FetchRegister(bcDestReg, _regcap), 0);
                        baResult = BitConverter.GetBytes(sSrcVal * sDstVal);
                        break;
                    case RegisterCapacity.B:
                        ulong lSrcVal = ControlUnit.FetchRegister(bcSrcReg, _regcap)[0];
                        ulong lDstVal = ControlUnit.FetchRegister(bcDestReg, _regcap)[0];
                        baResult = BitConverter.GetBytes(sSrcVal * sDstVal);
                        break;
                }
            }
            
        }

    }

    
}