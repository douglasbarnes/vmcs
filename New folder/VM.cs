using System;
using System.Collections.Generic;
using System.Linq;
using static debugger.ControlUnit;
using System.Threading;
using System.Collections.Concurrent;
using static debugger.Opcodes;
using static debugger.Util;
using static debugger.Primitives;
using static debugger.RegisterGroup;
using System.ComponentModel;
using System.Collections;
using System.Threading.Tasks;

namespace debugger
{
    public abstract class EmulatorBase : IDisposable
    {
        public readonly Handle Handle;
        public event Action RunComplete = () => { };
       // protected ulong InstructionPointer { get => ContextHandler.CloneContext(Handle).InstructionPointer; }
        public EmulatorBase(string inputName, Context inputContext) //new handle from context
        {
            Handle = new Handle(inputName, inputContext);
        }
        public void RegisterBreakpoint(ulong Address)
        { //overwrites
            Handle.DeepCopy().Breakpoints.Add(Address);
        }
        public virtual Status Run(bool Step = false)
        {
            return Handle.Run(Step);
        }
        public virtual async Task<Status> RunAsync(bool Step = false)
        {
            Task<Status> RunTask = new Task<Status>(() => Run(Step));
            RunTask.Start();
            Status Result = await RunTask;
            RunComplete.Invoke();
            return Result;
        }        
        public Dictionary<string, ulong> GetRegisters(RegisterCapacity RegisterSize)
        {
            if (Enum.IsDefined(typeof(RegisterCapacity), RegisterSize))
            {
                Context VMContext = Handle.ShallowCopy();
                Dictionary<string, ulong> Registers = VMContext.Registers.Format(RegisterSize);
                Registers.Add("RIP", VMContext.InstructionPointer);
                return Registers;
            }
            else
            {
                throw new Exception();
            }
        }
        public Dictionary<string, bool> GetFlags()
        {
            FlagSet VMFlags = Handle.ShallowCopy().Flags;
            return new Dictionary<string, bool>()
                {
                {"CF", VMFlags.Carry },
                {"PF", VMFlags.Parity },
                {"AF", VMFlags.Adjust },
                {"ZF", VMFlags.Zero },
                {"SF", VMFlags.Sign },
                {"OF", VMFlags.Overflow },
                };
        }
        public MemorySpace GetMemory()
        {
            return Handle.ShallowCopy().Memory;
        }
        public void Dispose()
        {
            Handle.Dispose();
        }
        
    }
    public class VM : EmulatorBase
    {
        public struct VMSettings
        {
            public int UndoHistoryLength;
            public Action RunCallback;
        }
        private readonly MemorySpace SavedMemory;
        private List<Context> CachedContexts;
       
        public VMSettings CurrentSettings;
        public BindingList<ulong> Breakpoints { get => new BindingList<ulong>(Handle.DeepCopy().Breakpoints); }
        public VM(VMSettings inputSettings, MemorySpace inputMemory) : base("VM", new Context() {
            InstructionPointer = inputMemory.EntryPoint,
            Memory = inputMemory,
            Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
            {
                { ByteCode.SP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) },
                { ByteCode.BP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) } }),
            })
        {
            CurrentSettings = inputSettings;
            RegisterBreakpoint(inputMemory.SegmentMap[".main"].LastAddr);
            RunComplete += CurrentSettings.RunCallback;
            SavedMemory = inputMemory;
        }
        public void Reset()
        {
            Handle.Invoke(() => 
            {
                Context VMContext = Handle.DeepCopy();
                VMContext.InstructionPointer = SavedMemory.EntryPoint;
                VMContext.Memory = SavedMemory;
                VMContext.Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
                {
                            { ByteCode.SP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) },
                            { ByteCode.BP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) }
                });
                
            });            
        }
    }   

    public static class ControlUnit
    {
        public class FlagSet
        {
            public bool Carry   =false;
            public bool Parity  =false;
            public bool Adjust  =false;
            public bool Zero    =false; // zero = false
            public bool Sign    =false; // false = positive
            public bool Overflow=false; // true = overflow
        }
        public struct Status
        {
            public enum ExitStatus
            {
                BreakpointReached
            }
            public ExitStatus ExitCode; // initialises to breakpoint reached
            public string LastDisassembled;            
        }        
        public struct Handle
        {
            private static Dictionary<Handle, Context> StoredContexts = new Dictionary<Handle, Context>();
            public readonly string HandleName;
            public readonly int HandleID;
            public Handle(string handleName, Context inputContext)
            {
                HandleName = handleName;
                HandleID = GetNextHandleID;
                if (StoredContexts.ContainsKey(this))
                {
                    StoredContexts[this] = inputContext;
                }
                else
                {
                    StoredContexts.Add(this, inputContext);
                }
            }
            public void Invoke(Action toExecute) // dont invoke run
            {
                WaitNotBusy();
                IsBusy = true;
                toExecute.Invoke();
                IsBusy = false;
            }
            public Context ShallowCopy() => StoredContexts[this].Clone();
            public Context DeepCopy() => StoredContexts[this];
            public Status Run(bool step)
            {
                Status Result = new Status();                
                WaitNotBusy();
                IsBusy = true;               
                if (CurrentHandle != this)
                {
                    if(CurrentHandle != EmptyHandle)
                    {
                        StoredContexts[CurrentHandle] = CurrentContext;
                    }
                    
                    CurrentHandle = this;
                }
                Result = new Func<Status>(() => Execute(step)).Invoke();                
                IsBusy = false;
                return Result;
            }
            public void Dispose()
            {
                if (CurrentHandle == this)
                {
                    CurrentHandle = EmptyHandle;
                }// could zero everything here if wanted to
                StoredContexts.Remove(this);
            }
            public static bool operator !=(Handle input1, Handle input2)
            {
                return input1.HandleID != input2.HandleID;
            }
            public static bool operator ==(Handle input1, Handle input2)
            {
                return input1.HandleID == input2.HandleID;
            }
        }            
        public class Context
        {            
            public FlagSet Flags;
            public MemorySpace Memory;
            public RegisterGroup Registers;
            public ulong InstructionPointer;
            public List<ulong> Breakpoints = new List<ulong>();
            public List<PrefixByte> PrefixBuffer = new List<PrefixByte>();
            public RegisterCapacity CurrentCapacity;            
            public Context()
            {
                Flags = new FlagSet();
            }

            private Context(Context toClone)
            {
                Flags = toClone.Flags;
                Memory = toClone.Memory;
                InstructionPointer = toClone.InstructionPointer;
                Breakpoints = toClone.Breakpoints;
                PrefixBuffer = toClone.PrefixBuffer;
                CurrentCapacity = toClone.CurrentCapacity;
                Registers = toClone.Registers.Clone();
            }

            public Context Clone()
            {
                return new Context(this);
            }
        }    
        private static int _handleID = 0;
        private static int GetNextHandleID { get { _handleID++; return _handleID; } set { _handleID = value; } }    
        public static readonly Handle EmptyHandle = new Handle("None", new Context());
        private static Handle CurrentHandle = EmptyHandle;
        private static Context CurrentContext { get => CurrentHandle.DeepCopy(); }

        public static RegisterCapacity CurrentCapacity { get => CurrentContext.CurrentCapacity; set => CurrentContext.CurrentCapacity = value; }
        public static FlagSet Flags { get => CurrentContext.Flags; }
        public static List<PrefixByte> PrefixBuffer { get => CurrentContext.PrefixBuffer; }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer; set => CurrentContext.InstructionPointer = value; }
        public static MemorySpace Memory { get => CurrentContext.Memory; }
        private static void WaitNotBusy()
        {
            while(Interlocked.Read(ref _busy) == 1)
            {
                Thread.Sleep(1);
            }
        }
        public static bool IsBusy { get { return Interlocked.Read(ref _busy) == 1; }
            private set {
                if (value == true && Interlocked.Read(ref _busy) == 0)
                {
                    Interlocked.Increment(ref _busy);
                } else if(value == false && Interlocked.Read(ref _busy) == 1)
                    {
                    Interlocked.Decrement(ref _busy);
                }
                else
                {
                    throw new Exception();
                }
            } }
        private static long _busy = 0;        
        private static Status Execute(bool step)
        {             
            byte OpcodeWidth = 1;
            Opcode CurrentOpcode= null;
            string TempLastDisas = "";
            while (true)
            {
                byte Fetched = FetchNext();
                if (Fetched == 0x0F)
                {
                    OpcodeWidth = 2;
                }
                else if (Enum.IsDefined(typeof(PrefixByte), (int)Fetched))
                {
                    CurrentContext.PrefixBuffer.Add((PrefixByte)Fetched);
                }
                else
                {
                    CurrentOpcode = OpcodeLookup.OpcodeTable[OpcodeWidth][Fetched].Invoke();
                    CurrentOpcode.Execute();
                    OpcodeWidth = 1;
                    CurrentContext.PrefixBuffer = new List<PrefixByte>();
                    TempLastDisas = CurrentOpcode.Disassemble();
                    if(step)
                    {
                        break;
                    }
                }  
                
                if(CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer))
                {
                    break;
                }
            }
            return new Status { LastDisassembled = TempLastDisas };
        }
        public static byte[] Fetch(ulong _addr, int _length=1)
        {
            byte[] baOutput = new byte[_length];
            for (byte i = 0; i < _length; i++)
            {
                if (CurrentContext.Memory.ContainsAddr(_addr+i))
                {
                    baOutput[i] = CurrentContext.Memory[_addr + i];
                } else
                {
                    baOutput[i] = 0x90;//CHANGE THIS WHEN WE DO STUFF PROPERLY
                }
            }
            return baOutput;
        } 
        public static byte[] FetchRegister(ByteCode bcByteCode, RegisterCapacity _regcap)
        {
            return CurrentContext.Registers.Fetch((byte)bcByteCode, (byte)_regcap);
        }
        public static void SetRegister(ByteCode RegisterCode, byte[] Data, bool HigherBit=false)
        {
            if(HigherBit)
            {
                RegisterCode -= 0x4;
                CurrentContext.Registers.Set((byte)RegisterCode, new byte[] { CurrentContext.Registers.Fetch((byte)RegisterCode, 2)[0], Data[0]});
            } else
            {
                if(Data.Length >= 4) { Data = Bitwise.ZeroExtend(Data, 8); }
                CurrentContext.Registers.Set((byte)RegisterCode, Data);
            }
            
        }
        public static void SetRegister(ByteCode bcByteCode, dynamic InputNumber)
        {
            SetRegister(bcByteCode, BitConverter.GetBytes(InputNumber));
        }
        public static void SetMemory(ulong Address, byte[] InputData)
        {
            for (uint iOffset = 0; iOffset < InputData.Length; iOffset++)
            {
                if (CurrentContext.Memory.ContainsAddr(Address + iOffset))
                {
                    CurrentContext.Memory[Address + iOffset] = InputData[iOffset];
                } else
                {
                    CurrentContext.Memory.Set(Address+iOffset, InputData[iOffset]);
                }
            }
            
        }
        public static byte FetchNext()
        {
            byte bFetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            CurrentContext.InstructionPointer++;
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
                    Output.DestPtr = CurrentContext.InstructionPointer + BitConverter.ToUInt32(FetchNext(4), 0);
                }
                else if (Output.RM == 4)//sib! sib always after modrm 
                {
                    Output.DecodedSIB = SIBDecode(Output.Mod);
                    Output.Offset += Output.DecodedSIB.OffsetValue;
                    Output.DestPtr += Output.DecodedSIB.ResultNoOffset;
                } else
                {
                    Output.DestPtr += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.RM, RegisterCapacity.Qword), 0);
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
                PointerSize = (CurrentContext.PrefixBuffer.Contains(PrefixByte.ADDR32) ? RegisterCapacity.Dword : RegisterCapacity.Qword)
            };
            if (Output.ScaledIndex != 4)//4 == none
            {
                Output.ScaledIndexValue = Output.Scale * BitConverter.ToUInt64(Bitwise.SignExtend(FetchRegister((ByteCode)Output.ScaledIndex, Output.PointerSize),8),0);
            }            

            if (Output.Base != 5) // 5 = ptr or ebp+ptr
            {
                Output.BaseValue += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.Base, RegisterCapacity.Qword), 0);
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
        private Dictionary<ulong, byte> AddressMap = new Dictionary<ulong, byte>();
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
        public static implicit operator Dictionary<ulong, byte>(MemorySpace m) => m.AddressMap;
        public MemorySpace(Dictionary<ulong, byte> _inputmemory)
        {
            AddressMap = _inputmemory;
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
                    AddressMap.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    for (ulong i = 0; i < (seg.LastAddr-seg.StartAddr); i++)
                    {
                        AddressMap.Add(i + seg.StartAddr, seg.baData[i]);
                    }
                }
                
            }            
        }     
        public bool ContainsAddr(ulong lAddress)
        {
            return (AddressMap.ContainsKey(lAddress)) ? true : false;
        }
        public void Set(string sSegName, ulong lOffset, byte bData)
        {
            Set(SegmentMap[sSegName].StartAddr + lOffset, bData);
        }
        public void Set(ulong lAddress, byte bData)
        {
            if (ContainsAddr(lAddress))
            {
                AddressMap[lAddress] = bData;
            } else
            {
                AddressMap.Add(lAddress, bData);
            }
            
        }
        public byte this[ulong Address]
        {
            get
            {
                return AddressMap[Address];
            }

            set
            {
                AddressMap[Address] = value;
            }
        }
        public void Map(Dictionary<ulong, byte> _inputmemory)
        {
            AddressMap = _inputmemory;
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
            public Register(ulong Input)
            {
                Data = BitConverter.GetBytes(Input);
            }
            
            public byte this[int i] => this[i];
            public static implicit operator byte[] (Register R)
            {
                return R.Data;
            }

            public static implicit operator Register(byte[] Input)
            {
                return new Register(Input);
            }

            private Register(Register toCopy)
            {
                byte[] CopyBuffer = new byte[8];
                Array.Copy(toCopy.Data, CopyBuffer, 8);
                Data = new Register(CopyBuffer);
            }
            public Register Clone()
            {
                return new Register(this);
            }
        }
        public RegisterGroup()
        {//there isn't a great way/clean to do this, enumerable.repeat returns x shallow copies of the same object e.g list will be 8 pointers to one reg
            RegTable = new List<Register>() { new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register() }; // 8 regs, 0 = eax, 1=ecx,2=edx,3=ebx,4=esp,5=ebp,6=esi,7=edi
        }
        public RegisterGroup(Dictionary<ByteCode,Register> Input)
        { // 8 regs
            for (int RegValue = 0; RegValue < 8; RegValue++)
            { 
                if(Input.TryGetValue((ByteCode)RegValue, out Register Current))
                {
                    RegTable.Add(Current);
                }
                else
                {
                    RegTable.Add(new Register());
                }
            }
        }
        private RegisterGroup(RegisterGroup toClone)
        {
            for (int i = 0; i < toClone.RegTable.Count(); i++)
            {
                var a = toClone.RegTable[i].Clone();
                RegTable.Add(a);
            }
        }

        public RegisterGroup Clone()
        {
            return new RegisterGroup(this);
        }
        private readonly List<Register> RegTable = new List<Register>();
        protected internal byte[] Fetch(byte RegCode, byte Size)
        {
            return Bitwise.Cut(RegTable[RegCode], Size);
        }
        protected internal void Set(byte RegCode, byte[] Input)
        {
            if(!new int[] { 1,2,4,8 }.Contains(Input.Length))
            {
                throw new Exception();
            }
            Array.Copy(Input, 0, RegTable[RegCode], 0, Input.Length); // set lower (input.length) bytes of regtable[..] with 0 offset
        }
        public Dictionary<string, ulong> Format(RegisterCapacity RegCap)
        {
            Dictionary<string, ulong> Parsed = new Dictionary<string, ulong>();
            for (byte i_Reg = 0; i_Reg < 8; i_Reg++)
            {
                byte[] RegisterValue = (RegCap == RegisterCapacity.Qword) ? (byte[])RegTable[i_Reg] : Bitwise.SignExtend(Bitwise.Cut(RegTable[i_Reg], (byte)RegCap), 8);
                string RegisterMnemonic = Disassembly.DisassembleRegister((ByteCode)i_Reg, RegCap);
                Parsed.Add(RegisterMnemonic, BitConverter.ToUInt64(RegisterValue, 0));
            }
            return Parsed;
        }
    } 
}
