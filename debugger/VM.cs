using System;
using System.Collections.Generic;
using System.Linq;
using static debugger.ControlUnit;
using System.Threading;
using static debugger.Opcodes;
using static debugger.Util;
using static debugger.Primitives;
using static debugger.RegisterGroup;
using System.ComponentModel;
using System.Threading.Tasks;

namespace debugger
{
    public abstract class EmulatorBase : IDisposable
    {
        protected internal readonly Handle Handle;
        public event Action RunComplete = () => { };
        public EmulatorBase(string inputName, Context inputContext) //new handle from context
        {
            inputContext.Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
            {
                { ByteCode.SP, new Register(inputContext.Memory.SegmentMap[".stack"].StartAddr) },
                { ByteCode.BP, new Register(inputContext.Memory.SegmentMap[".stack"].StartAddr) },
            });
            Handle = new Handle(inputName, inputContext);
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
        public Dictionary<string, bool> GetFlags()
        {
            FlagSet VMFlags = Handle.ShallowCopy().Flags;
            return new Dictionary<string, bool>()
                {
                {"Carry", VMFlags.Carry         == FlagSet.FlagState.On},
                {"Parity", VMFlags.Parity       == FlagSet.FlagState.On},
                {"Auxiliary", VMFlags.Auxiliary == FlagSet.FlagState.On},
                {"Zero", VMFlags.Zero           == FlagSet.FlagState.On},
                {"Sign", VMFlags.Sign           == FlagSet.FlagState.On},
                {"Overflow", VMFlags.Overflow   == FlagSet.FlagState.On},
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
        public VM(VMSettings inputSettings, MemorySpace inputMemory) : base("VM", new Context(inputMemory) {
            InstructionPointer = inputMemory.EntryPoint,
            Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
            {
                { ByteCode.SP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) },
                { ByteCode.BP, new Register(inputMemory.SegmentMap[".stack"].StartAddr) } }),          
            })            
        {
            CurrentSettings = inputSettings;
            RunComplete += CurrentSettings.RunCallback;
            SavedMemory = inputMemory;
        }
        public void Reset()
        {
            Handle.Invoke(() =>
            {
                Context VMContext = Handle.DeepCopy();
                VMContext.Memory = SavedMemory;
                VMContext.InstructionPointer = SavedMemory.EntryPoint;
                VMContext.Registers = new RegisterGroup(new Dictionary<ByteCode, Register>()
                    {
                                { ByteCode.SP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) },
                                { ByteCode.BP, new Register(SavedMemory.SegmentMap[".stack"].StartAddr) }
                    });
                VMContext.Flags = new FlagSet();
            });            
        }
        public Dictionary<string, ulong> GetRegisters(RegisterCapacity registerSize)
        {
            Context Cloned = Handle.ShallowCopy();
            Dictionary<ByteCode, byte[]> Registers = Cloned.Registers.FetchAll();
            Dictionary<string, ulong> ParsedRegisters = new Dictionary<string, ulong>();
            foreach (var Reg in Registers)
            {
                ParsedRegisters.Add(Disassembly.DisassembleRegister(Reg.Key, registerSize), BitConverter.ToUInt64(Reg.Value,0));
            }
            ParsedRegisters.Add("RIP", Cloned.InstructionPointer);
            return ParsedRegisters;
        }
    }   

    public static class ControlUnit
    {
        public class FlagSet
        {
            public enum FlagState
            {
                Off,
                On,
                Undefined
            }
            public FlagState Carry = FlagState.Undefined;            
            public FlagState Auxiliary = FlagState.Undefined;            
            public FlagState Overflow = FlagState.Undefined; // true = overflow
            public FlagState Zero = FlagState.Undefined; // zero = false
            public FlagState Sign = FlagState.Undefined; // false = positive
            public FlagState Parity = FlagState.Undefined;
            public FlagSet() { }
            public FlagSet(byte[] input) //Auto calculate zf/sf/pf
            {
                Zero = input.IsZero() ? FlagState.On : FlagState.Off;
                Sign = input.IsNegative() ? FlagState.On : FlagState.Off;
                Parity = Bitwise.GetBits(input).Count(x => x == 1) % 2 == 0 ? FlagState.On : FlagState.Off; //parity: even no of 1 bits       
            }            
            public FlagSet DeepCopy() => new FlagSet()
            {//enums are value types
                Carry = Carry,
                Auxiliary = Auxiliary,
                Overflow = Overflow,
                Zero = Zero,
                Sign = Sign,
                Parity = Parity
            };
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
            public Context ShallowCopy() => StoredContexts[this].DeepCopy();
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
            public FlagSet Flags = new FlagSet();
            public MemorySpace Memory;
            public RegisterGroup Registers = new RegisterGroup();
            public ulong InstructionPointer;
            public List<ulong> Breakpoints = new List<ulong>();       
            public Context(MemorySpace memory)
            {
                Memory = memory;
                InstructionPointer = Memory.EntryPoint;              
            }

            private Context(Context toClone)
            {
                Flags = toClone.Flags.DeepCopy();
                Memory = toClone.Memory.DeepCopy();
                InstructionPointer = toClone.InstructionPointer; // val type
                Breakpoints = toClone.Breakpoints.DeepCopy();
                Registers = toClone.Registers.DeepCopy();
            }
            public Context DeepCopy()
            {
                return new Context(this);
            }
        }    
        private static int _handleID = 0;
        private static int GetNextHandleID { get { _handleID++; return _handleID; } set { _handleID = value; } }    
        public static readonly Handle EmptyHandle = new Handle("None", new Context(new MemorySpace(new byte[] { 0x00 })));
        private static Handle CurrentHandle = EmptyHandle;
        private static Context CurrentContext { get => CurrentHandle.DeepCopy(); }        
        public static FlagSet Flags { get => CurrentContext.Flags; }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer; set => CurrentContext.InstructionPointer = value; }
        public static MemorySpace Memory { get => CurrentContext.Memory; }
        public static List<PrefixByte> PrefixBuffer { get; private set; } = new List<PrefixByte>();
        public static RegisterCapacity CurrentCapacity { get; set; }
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
            while (CurrentContext.InstructionPointer != CurrentContext.Memory.End)
            {
                byte Fetched = FetchNext();
                if (Fetched == 0x0F)
                {
                    OpcodeWidth = 2;
                }
                else if (Enum.IsDefined(typeof(PrefixByte), (int)Fetched))
                {
                    PrefixBuffer.Add((PrefixByte)Fetched);
                }
                else
                {
                    CurrentOpcode = OpcodeLookup.OpcodeTable[OpcodeWidth][Fetched].Invoke();
                    CurrentOpcode.Execute();
                    OpcodeWidth = 1;
                    PrefixBuffer = new List<PrefixByte>();
                    TempLastDisas = CurrentOpcode.Disassemble();
                    if(step || CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer))
                    {
                        break;
                    }
                }
            }
            return new Status { LastDisassembled = TempLastDisas };
        }
        public static byte[] Fetch(ulong address, int length=1)
        {
            byte[] output = new byte[length];
            for (byte i = 0; i < length; i++)
            {
                output[i] = CurrentContext.Memory[address + i];
            }
            return output;
        }
        public static byte[] FetchRegister(ByteCode register, RegisterCapacity size)
        {
            if (size == RegisterCapacity.BYTE && (int)register > 3)
            {
                return Bitwise.Subarray(CurrentContext.Registers[register-4, RegisterCapacity.WORD], 1);
            } else
            {
                return CurrentContext.Registers[register, size];
            }
        }
        public static void SetRegister(ByteCode register, byte[] data)
        {
            data = Bitwise.Cut(data, (int)CurrentCapacity);
            if(CurrentCapacity == RegisterCapacity.BYTE && (int)register > 3) // setting higher bit of word reg
            { // e.g AH has the same integer value as SP(SP has no higher bit register) so when 0b101 is accessed with byte width we need to sub 4 to get the normal reg code for that reg then set higher bit ourselves 
                CurrentContext.Registers[register-4, RegisterCapacity.WORD] = new byte[] { CurrentContext.Registers[register-4, RegisterCapacity.BYTE][0], data[0] };
            } else
            {
                if(data.Length == 4) { data = Bitwise.ZeroExtend(data, 8); }
                CurrentContext.Registers[register, (RegisterCapacity)data.Length] = data;
            }            
        }
        public static void SetMemory(ulong address, byte[] data)
        {
            for (uint iOffset = 0; iOffset < data.Length; iOffset++)
            {
                CurrentContext.Memory[address + iOffset] = data[iOffset];
            }
            
        }
        public static void SetFlags(FlagSet input)
        {
            CurrentContext.Flags = input;
        }
        public static byte FetchNext()
        {
            byte bFetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            CurrentContext.InstructionPointer++;
            return bFetched;           
        }
        public static byte[] FetchNext(byte count)
        {
            byte[] baOutput = new byte[count];
            for (int i = 0; i < count; i++)
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
            Output.Reg = Convert.ToByte(sBits.Substring(5, 3), 2); // rm = dest

           
            if (Output.Mod == 3)
            {
                // direct register
                Output.DestPtr = Output.Reg;
            }
            else
            {
                Output.Offset = 0;
                if(Output.Mod == 0 && Output.Reg == 5) //special case, rip relative imm32
                {
                    Output.DestPtr = CurrentContext.InstructionPointer + BitConverter.ToUInt32(FetchNext(4), 0);
                }
                else if (Output.Reg == 4)//sib! sib always after modrm 
                {
                    Output.DecodedSIB = SIBDecode(Output.Mod);
                    Output.Offset += Output.DecodedSIB.OffsetValue;
                    Output.DestPtr += Output.DecodedSIB.ResultNoOffset;
                } else
                {
                    Output.DestPtr += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.Reg, RegisterCapacity.QWORD), 0);
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
                PointerSize = (PrefixBuffer.Contains(PrefixByte.ADDR32) ? RegisterCapacity.DWORD : RegisterCapacity.QWORD)
            };
            if (Output.ScaledIndex != 4)//4 == none
            {
                Output.ScaledIndexValue = Output.Scale * BitConverter.ToUInt64(Bitwise.SignExtend(FetchRegister((ByteCode)Output.ScaledIndex, Output.PointerSize),8),0);
            }            

            if (Output.Base != 5) // 5 = ptr or rbp+ptr
            {
                Output.BaseValue += BitConverter.ToUInt64(FetchRegister((ByteCode)Output.Base, RegisterCapacity.QWORD), 0);
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
        public ulong EntryPoint;
        public ulong End;       
        public class Segment
        {
            public ulong StartAddr;
            public ulong End;
            public byte[] baData = null;
        }
        public static implicit operator Dictionary<ulong, byte>(MemorySpace m) => m.AddressMap;
        public MemorySpace(Dictionary<ulong, byte> memory, Dictionary<string, Segment> segmap)
        {
            AddressMap = memory;
            SegmentMap = segmap;
            EntryPoint = segmap[".main"].StartAddr;
            End = segmap[".main"].End;
        } 
        public MemorySpace(byte[] memory)
        {
            EntryPoint = 0;
            End = (ulong)memory.LongLength;
            SegmentMap.Add(".main", new Segment() { StartAddr = EntryPoint, End = EntryPoint + (ulong)memory.LongLength, baData = memory });           
            SegmentMap.Add(".heap", new Segment() { StartAddr = 0x400001, End = 0x0 });
            SegmentMap.Add(".stack", new Segment() { StartAddr = 0x800000, End = 0x0 });

            foreach (Segment seg in SegmentMap.Values)
            {
                if (seg.baData == null)
                {
                    AddressMap.Add(seg.StartAddr, 0x0);
                }
                else
                {
                    for (ulong i = 0; i < (seg.End-seg.StartAddr); i++)
                    {
                        AddressMap.Add(i + seg.StartAddr, seg.baData[i]);
                    }
                }
                
            }            
        }     
        private MemorySpace(MemorySpace toClone)
        {
            AddressMap = toClone.AddressMap.DeepCopy();
            SegmentMap = toClone.SegmentMap.DeepCopy();
            EntryPoint = toClone.EntryPoint;
            End = toClone.End;
        }
        public MemorySpace DeepCopy()
        {
            return new MemorySpace(this);
        }
        public byte this[ulong address]
        {
            get
            {
                return AddressMap.ContainsKey(address) ? AddressMap[address] : (byte)0x00;
            }
            set
            {
                if(AddressMap.ContainsKey(address))
                {
                    AddressMap[address] = value;
                }
                else
                {
                    AddressMap.Add(address, value);
                }
            }
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

            public static implicit operator byte[](Register R)
            {
                return R.Data;
            }

            public static implicit operator Register(byte[] Input)
            {
                return new Register(Input);
            }
            private Register(Register toCopy)
            {
                Data = toCopy.Data.DeepCopy();
            }
            public Register Clone()
            {
                return new Register(this);
            }
        }
        public RegisterGroup()
        {//there isn't a great way/clean to do this, enumerable.repeat returns x shallow copies of the same object e.g list will be 8 pointers to one reg
            Registers = new List<Register>() { new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register(), new Register() }; // 8 regs, 0 = eax, 1=ecx,2=edx,3=ebx,4=esp,5=ebp,6=esi,7=edi
        }
        public RegisterGroup(Dictionary<ByteCode, Register> Input)
        { // 8 regs
            for (int RegValue = 0; RegValue < 8; RegValue++)
            {
                if (Input.TryGetValue((ByteCode)RegValue, out Register Current))
                {
                    Registers.Add(Current);
                }
                else
                {
                    Registers.Add(new Register());
                }
            }
        }
        private RegisterGroup(RegisterGroup toClone)
        {
            for (int i = 0; i < toClone.Registers.Count(); i++)
            {
                Registers.Add(toClone.Registers[i].Clone());
            }
        }
        private readonly List<Register> Registers = new List<Register>();
        public byte[] this[ByteCode register, RegisterCapacity size]
        {
            get => Bitwise.Cut(Registers[(int)register], (int)size);
            set => Array.Copy(value, 0, Registers[(int)register], 0, (int)size);
        }
        public RegisterGroup DeepCopy()
        {
            var a = new RegisterGroup(this);
            return a;
        }
        public Dictionary<ByteCode, byte[]> FetchAll()
        {
            RegisterGroup Cloned = DeepCopy();
            Dictionary<ByteCode, byte[]> Output = new Dictionary<ByteCode, byte[]>();
            for (int register = 0; register < Cloned.Registers.Count(); register++)
            {
                Output.Add((ByteCode)register, Cloned.Registers[register]);
            }
            return Output;
        }
        
    } 
}
