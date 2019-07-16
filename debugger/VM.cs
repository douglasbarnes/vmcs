using System;
using System.Collections.Generic;
using System.Linq;
using static debugger.ControlUnit;
using System.Threading;
using System.Collections.Concurrent;
using static debugger.Opcodes;
using static debugger.Util;
using static debugger.Primitives;

namespace debugger
{
    public class VM
    {
        public VM(MemorySpace InputMemory)
        {
            OpcodeLookup.Refresh();
            ControlUnit.Initialise(InputMemory);
        }
        public void Run()
        {
            ControlUnit.ClockStart();
        }

        public void Step()
        {
            ControlUnit.ClockStart(Step: true);
        }

        public List<Dictionary<string, ulong>> GetRegisters()
        {
            return new List<Dictionary<string, ulong>>() {
                 new Dictionary<string, ulong>()
                {
                { "RIP", RIP},
                { "RSP", BitConverter.ToUInt64(FetchRegister(ByteCode.SP, RegisterCapacity.R),0)},
                { "RBP", BitConverter.ToUInt64(FetchRegister(ByteCode.BP, RegisterCapacity.R),0)},
                { "RSI", BitConverter.ToUInt64(FetchRegister(ByteCode.SI, RegisterCapacity.R),0)},
                { "RDI", BitConverter.ToUInt64(FetchRegister(ByteCode.DI, RegisterCapacity.R),0)}
                },

                new Dictionary<string, ulong>()
                {
                { "RAX", BitConverter.ToUInt64(FetchRegister(ByteCode.A, RegisterCapacity.R),0)},
                { "RBX", BitConverter.ToUInt64(FetchRegister(ByteCode.B, RegisterCapacity.R),0)},
                { "RCX", BitConverter.ToUInt64(FetchRegister(ByteCode.C, RegisterCapacity.R),0)},
                { "RDX", BitConverter.ToUInt64(FetchRegister(ByteCode.D, RegisterCapacity.R),0)}
                },
                new Dictionary<string, ulong>()
                {
                {"CF", (ulong)(Eflags.Carry ? 1 : 0) },
                {"PF", (ulong)(Eflags.Parity ? 1 : 0) },
                {"AF", (ulong)(Eflags.Adjust ? 1 : 0) },
                {"ZF", (ulong)(Eflags.Zero ? 1 : 0) },
                {"SF", (ulong)(Eflags.Sign ? 1 : 0) },
                {"OF", (ulong)(Eflags.Overflow ? 1 : 0) },
                }
           };
        }
        public Dictionary<ulong, byte> GetMemory()
        {
            return Memory;
        }
    }

    public static class ControlUnit
    {
        
        public class Eflags
        {
            public static bool Carry = false;
            public static bool Parity = false;
            public static bool Adjust = false;
            public static bool Zero = false; // zero = false
            public static bool Sign = false; // false = positive
            public static bool Overflow = false; // true = overflow
        }
        public static MemorySpace Memory = new MemorySpace();
        private static RegisterGroup Registers = new RegisterGroup();
        public static RegisterCapacity CurrentCapacity;
        public static ulong RIP;
        public static ulong BytePointer;
        public static List<PrefixByte> Prefixes = new List<PrefixByte>();
        private static byte _opbytes = 1;

        public static void Initialise(MemorySpace _memory)
        {
            BytePointer = _memory.EntryPoint;
            Memory = _memory;
            SetRegister(ByteCode.SP, _memory.SegmentMap[".stack"].StartAddr);
            SetRegister(ByteCode.BP, _memory.SegmentMap[".stack"].StartAddr);
            RIP = Memory.SegmentMap[".main"].StartAddr;
        }
        public static void ClockStart(bool Step=false)
        {
            Thread t = new Thread(() => _step());
            
            if (Step)
            {
                t.Start();
                t.Join();
            }
            else
            {
                while (true)
                {
                    t.Start();
                    t.Join();
                }
            }
            
        }
        private static void _step()
        {
            BytePointer = RIP;
            byte bFetched = FetchNext();
            if (!_decode(bFetched)) // true = no op
            {
                _execute(bFetched);
                Prefixes = new List<PrefixByte>();
                RIP = BytePointer;
            } else
            {
                RIP++;
                _step();
            }           
        }
        public static byte[] Fetch(byte[] Address, int Length=1)
        {
            return Fetch(BitConverter.ToUInt64(Address, 0), Length);
        }
        public static byte[] Fetch(ulong _addr, int _length=1)
        {
            byte[] baOutput = new byte[_length];
            for (byte i = 0; i < _length; i++)
            {
                if (Memory.ContainsAddr(_addr+i))
                {
                    baOutput[i] = Memory[_addr + i];
                } else
                {
                    baOutput[i] = 0x00;
                }
            }
            return baOutput;
        }
        /*public static byte[] Fetch(ulong _addr, RegisterCapacity _regcap)
        {
            return Fetch(_addr, (byte)_regcap);
        }*/
        public static byte[] FetchRegister(ByteCode bcByteCode)
        {
            return FetchRegister(bcByteCode, CurrentCapacity);
        }
        public static byte[] FetchRegister(ByteCode bcByteCode, RegisterCapacity _regcap)
        {
            return Registers.Fetch((byte)bcByteCode, (byte)_regcap);
        }
        public static void SetRegister(ByteCode RegisterCode, byte[] Data, bool HigherBit=false)
        {
            if(HigherBit)
            {
                RegisterCode -= 0x4;
                Registers.Set((byte)RegisterCode, new byte[] { Registers.Fetch((byte)RegisterCode, 2)[0], Data[0]});
            } else
            {
                if(Data.Length >= 4) { Data = Bitwise.ZeroExtend(Data, 8); }
                Registers.Set((byte)RegisterCode, Data);
            }
            
        }
        public static void SetRegister(ByteCode bcByteCode, byte bData)
        {
            SetRegister(bcByteCode, new byte[] { bData });
        }
        public static void SetRegister(ByteCode bcByteCode, ulong lData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(lData));
        }
        public static void SetRegister(ByteCode bcByteCode, uint iData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(iData));
        }
        public static void SetRegister(ByteCode bcByteCode, ushort sData)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(sData));
        }
        public static void SetMemory(byte[] Address, byte[] baData)
        {
            SetMemory(BitConverter.ToUInt64(Address,0), baData);
        }
        public static void SetMemory(ulong lAddress, byte[] baData)
        {
            for (uint iOffset = 0; iOffset < baData.Length; iOffset++)
            {
                if (Memory.ContainsAddr(lAddress + iOffset))
                {
                    Memory[lAddress + iOffset] = baData[iOffset];
                } else
                {
                    Memory.Set(lAddress+iOffset, baData[iOffset]);
                }
            }
            
        }
        public static void SetMemory(ulong lAddress, byte bData)
        {
            if (Memory.ContainsAddr(lAddress))
            {
                Memory[lAddress] = bData;
            }
            else
            {
                Memory.Set(lAddress, bData);
            }
        }
        public static void SetMemory(ulong lDestAddr, ulong lSrcAddr, byte bLength = 1)
        {
            SetMemory(lDestAddr, Fetch(lSrcAddr, bLength));
        }
        public static byte FetchNext(bool Increase=true)
        {
            byte bFetched = Fetch(BytePointer, 1)[0];
            if (Increase)
            {
                BytePointer++;
            }
            return bFetched;
            
        }
        public static byte[] FetchNext(byte bLength)
        {
            byte[] baOutput = new byte[bLength];
            for (int i = 0; i < bLength; i++)
            {
                baOutput[i] = FetchNext();
            }
            return baOutput;
        }
        private static bool _decode(byte bFetched)
        {
            if (bFetched == 0x0F)
            {
                _opbytes = 2;
                return true;
            }
            else if (Enum.IsDefined(typeof(PrefixByte), (int)bFetched)) {
                Prefixes.Add((PrefixByte)bFetched);
                return true;
            } 
            return false;
        }
        private static void _execute(byte bFetched)
        {
            OpcodeLookup.OpcodeTable[_opbytes][bFetched].Invoke().Execute();
            _opbytes = 1;
        }
        public static ModRM ModRMDecode()
        {
            return ModRMDecode(FetchNext());
        }
        public static ModRM ModRMDecode(byte bModRM)
        {
            ModRM Output = new ModRM();
            string sBits = Bitwise.GetBits(bModRM);
            Output.Mod = Convert.ToByte(sBits.Substring(0, 2), 2); // pointer, offset pointer, or reg
            //Output.Reg = Convert.ToByte(sBits.Substring(2, 3), 2); //reg =src
            Output.SourceReg = Convert.ToByte(sBits.Substring(2, 3), 2);
            Output.RM = Convert.ToByte(sBits.Substring(5, 3), 2); // rm = dest

           
            if (Output.Mod == 3)
            {
                // direct register
                Output.DestPtr = Output.RM;
            }
            else
            {
                Output.Offset = 0;
                if(Output.Mod == 0 && Output.RM == 5) //special case, rip relative imm32
                {
                    Output.DestPtr = BytePointer + BitConverter.ToUInt32(FetchNext(4), 0);
                }
                else if (Output.RM == 4)//sib! sib always after modrm 
                {
                    Output.DecodedSIB = SIBDecode(Output.Mod);
                    Output.Offset += Output.DecodedSIB.OffsetValue;
                    Output.DestPtr += Output.DecodedSIB.ResultNoOffset;
                } else
                {
                    Output.DestPtr += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.RM, RegisterCapacity.R), 0);
                }

                //any immediate
                if (Output.Mod == 1) //1B imm
                {
                    Output.Offset += FetchNext();
                }
                else if (Output.Mod == 2) // 1W imm, mod1 rm5 is disp32
                {
                    Output.Offset += BitConverter.ToUInt32(FetchNext(4), 0);
                }



            //   if (Output.RM == 5) // disp32/rbp+disp
            //   {
            //       if (Output.Mod == 0) // either displacement32 if it is just a pointer(mod=00)
            //       {
            //           Output.Offset = BitConverter.ToUInt32(Fetch(BytePointer, 4), 0);
            //       }
            //       else // or ebp + disp, need to use SIb to get ebp without a displacement(mod!=00)
            //       {
            //           Output.DestPtr += BitConverter.ToUInt64(FetchRegister(ByteCode.BH, RegisterCapacity.R), 0);
            //       }
            //   }
                Output.DestPtr += (ulong)Output.Offset;// set it to the offset because its easier for disassembler
            }
            
            return Output;
        }
        public static SIB SIBDecode(int Mod)
        {          
            string SIBbits = Bitwise.GetBits(FetchNext());
            SIB Output = new SIB()
            {
                Scale    = (byte)Math.Pow(2, Convert.ToByte(SIBbits.Substring(0, 2), 2)),  // scale
                ScaledIndex = Convert.ToByte(SIBbits.Substring(2, 3), 2),
                Base  = Convert.ToByte(SIBbits.Substring(5, 3), 2),
                PointerSize = (Prefixes.Contains(PrefixByte.ADDR32) ? RegisterCapacity.E : RegisterCapacity.R)
            };
            if (Output.ScaledIndex != 4)//4 == none
            {
                Output.ScaledIndexValue = Output.Scale * BitConverter.ToUInt64(Bitwise.SignExtend(FetchRegister((ByteCode)Output.ScaledIndex, Output.PointerSize),8),0);
            }            

            if (Output.Base != 5) // 5 = ptr or ebp+ptr
            {
                Output.BaseValue += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.Base, RegisterCapacity.R), 0);
            } else
            {                                                                       // mod1 = imm8 else imm32
                Output.OffsetValue += BitConverter.ToInt64(Bitwise.SignExtend(FetchNext( (byte)((Mod == 1) ? 1 : 4) ), 8), 0);
                if (Mod > 0) // mod1 mod2 = ebp+imm
                {
                    Output.BaseValue += BitConverter.ToUInt64(Bitwise.SignExtend(FetchRegister(ByteCode.BP, Output.PointerSize),8),0);
                }
                
            }
            Output.ResultNoOffset = Output.BaseValue + Output.ScaledIndexValue;
            return Output;
        }
    }
    public class MemorySpace
    {
        
        private Dictionary<ulong, byte> _memory = new Dictionary<ulong, byte>();
        public Dictionary<string, Segment> SegmentMap = new Dictionary<string, Segment>();
        public ulong Size;
        public ulong EntryPoint;
        public ulong LastAddr;
        

        public class Segment
        {
            public ulong StartAddr;
            public ulong LastAddr;
            public byte[] baData = null;
        }

        public static implicit operator Dictionary<ulong, byte>(MemorySpace m)
        {
            return m._memory;
        }

        public MemorySpace() { }
        public MemorySpace(Dictionary<ulong, byte> _inputmemory)
        {
            _memory = _inputmemory;
            Size = (ulong)_inputmemory.LongCount();
            ulong _tmphighest = 0;
            ulong _tmplowest = ulong.MaxValue;
            foreach (ulong Address in _inputmemory.Keys)
            {
                if(Address > _tmphighest)
                {
                    _tmphighest = Address;
                }
                if(Address < _tmplowest)
                {
                    _tmplowest = Address;
                }
            }
            LastAddr = _tmphighest;
            EntryPoint = _tmplowest;
        } //NEEDS UPDATING DON TUSE

        public MemorySpace(byte[] _rawinputmemory)
        {
            EntryPoint = 0x100000;
            SegmentMap.Add(".main", new Segment() { StartAddr = EntryPoint, LastAddr = EntryPoint + (ulong)_rawinputmemory.LongLength, baData = _rawinputmemory });           
            SegmentMap.Add(".heap", new Segment() { StartAddr = 0x400001, LastAddr = 0x0 });
            SegmentMap.Add(".stack", new Segment() { StartAddr = 0x800000, LastAddr = 0x0 });

            foreach (Segment seg in SegmentMap.Values)
            {
                if (seg.baData == null)
                {
                    _memory.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    for (ulong i = 0; i < (seg.LastAddr-seg.StartAddr); i++)
                    {
                        _memory.Add(i + seg.StartAddr, seg.baData[i]);
                    }
                }
                
            }            
        }


       

        public bool ContainsAddr(ulong lAddress)
        {
            return (_memory.ContainsKey(lAddress)) ? true : false;
        }

        public void Set(string sSegName, ulong lOffset, byte bData)
        {
            Set(SegmentMap[sSegName].StartAddr + lOffset, bData);
        }
        public void Set(ulong lAddress, byte bData)
        {
            if (ContainsAddr(lAddress))
            {
                _memory[lAddress] = bData;
            } else
            {
                _memory.Add(lAddress, bData);
            }
            
        }
        public byte this[ulong Address]
        {
            get
            {
                return _memory[Address];
            }

            set
            {
                _memory[Address] = value;
            }
        }

        public void Map(Dictionary<ulong, byte> _inputmemory)
        {
            _memory = _inputmemory;
        }
    }

    

    public class RegisterGroup
    {
        
        public class Register
        {
            private byte[] Data;
            public Register()
            {
                Data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            }
            public Register(byte[] Input)
            {
                Data = Input;
            }
            public byte this[int i] => this[i];
            public static implicit operator byte[] (Register R)
            {
                return R.Data;
            }

            public static implicit operator Register(byte[] Initialiser)
            {
                return new Register(Initialiser);
            }
        }
        private static List<Register> _regTable = new List<Register>() //there isn't a great way/clean to do this, enumerable.repeat returns x instances of the same object e.g list will be 8 pointers to one reg
        { new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register() }; // 8 regs, 0 = eax, 1=ecx,2=edx,3=ebx,4=esp,5=ebp,6=esi,7=edi
        protected internal static List<bool> _Flags = Enumerable.Repeat(false, 6).ToList();
        private static byte[] _setLower(Register DestReg, byte[] Input, int iOffset = 0)
        {
            Array.Copy(Input, 0, DestReg, iOffset, Input.Length - iOffset);
            return DestReg;
        }
        protected internal byte[] Fetch(byte RegCode, byte Size)
        {
            return Bitwise.Cut(_regTable[RegCode], Size);
        }
        protected internal void Set(byte RegCode, byte[] Input)
        {
            if(!new int[] { 1,2,4,8 }.Contains(Input.Length))
            {
                throw new Exception();
            }
            _regTable[RegCode] = _setLower(_regTable[RegCode], Input);
        }

       
    } 
}
