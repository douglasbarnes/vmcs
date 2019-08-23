using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator
{
    public struct Status
    {
        public enum ExitStatus
        {
            BreakpointReached
        }
        public ExitStatus ExitCode; // initialises to breakpoint reached
        public List<string> LastDisassembled;
    }
    public enum PrefixByte
    {
        ADDR32 = 0x67,
        SIZEOVR = 0x66,
        REPNZ=0xF2,
        REPZ =0XF3,
    }
    [Flags]
    public enum REX
    {
        NONE=0,
        EMPTY=1,
        B=2,
        X=4,
        R=8,
        W=16
    }
    public static partial class ControlUnit
    {

        public static readonly Handle EmptyHandle = new Handle("None", new Context(new MemorySpace(new byte[] { 0x00 })), HandleParameters.NONE);
        public static Handle CurrentHandle = EmptyHandle;
        private static Context CurrentContext { get => CurrentHandle.DeepCopy(); }
        public static FlagSet Flags { get => CurrentContext.Flags; }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer;
            set {
                if ((CurrentHandle.HandleSettings | HandleParameters.NOJMP) != CurrentHandle.HandleSettings)
                {
                    CurrentContext.InstructionPointer = value;
                }
            }
        }
        public static MemorySpace Memory { get => CurrentContext.Memory; }
        public static readonly List<PrefixByte> PrefixBuffer  = new List<PrefixByte>();
        public static REX RexByte = REX.NONE;
        private static Status Execute(bool step)
        {
            byte OpcodeWidth = 1;
            Opcode CurrentOpcode = null;
            List<string> TempLastDisas = new List<string>();
            while (CurrentContext.InstructionPointer <= CurrentContext.Memory.End)
            {
                byte Fetched = FetchNext();
                if(Fetched >= 0x40 && Fetched < 0x50 && OpcodeWidth == 1)
                {// shift to avoid 0(NONE)
                    RexByte = (REX)((Fetched << 1) & 0b00011110); // remove upper half so [flags] can be inferred
                }
                else if (Fetched == 0x0F)
                {
                    OpcodeWidth = 2;
                }
                else if (Enum.IsDefined(typeof(PrefixByte), (int)Fetched))
                {
                    PrefixBuffer.Add((PrefixByte)Fetched);
                }
                else
                {                    
                    CurrentOpcode = OpcodeTable[OpcodeWidth][Fetched]();
                    CurrentOpcode.Execute();
                    bool IsRepZ = PrefixBuffer.Contains(PrefixByte.REPZ); // save buffer.contains later, dont trust compiler here
                    if (IsRepZ || PrefixBuffer.Contains(PrefixByte.REPNZ))
                    {
                        uint Count = BitConverter.ToUInt32(FetchRegister(XRegCode.C, RegisterCapacity.GP_DWORD),0);
                        if ((CurrentOpcode.Settings | OpcodeSettings.STRINGOP) == CurrentOpcode.Settings)
                        {
                            for (; Count > 0; Count--)
                            {
                                CurrentOpcode = OpcodeTable[OpcodeWidth][Fetched]();
                                CurrentOpcode.Execute();
                            }
                            SetRegister(XRegCode.C, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                        }
                        else if((CurrentOpcode.Settings | OpcodeSettings.STRINGOPCMP) == CurrentOpcode.Settings)
                        {
                            for (; Count > 0 || (IsRepZ && Flags.Zero == FlagState.ON) || (!IsRepZ && Flags.Zero == FlagState.OFF); Count--)
                            {
                                CurrentOpcode = OpcodeTable[OpcodeWidth][Fetched]();
                                CurrentOpcode.Execute();
                            }
                            SetRegister(XRegCode.C, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                        }                        
                    }
                    OpcodeWidth = 1;
                    PrefixBuffer.Clear();
                    RexByte = REX.NONE;
                    if ((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
                    {
                        TempLastDisas = CurrentOpcode.Disassemble();
                    }                   
                    if (step || CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer))
                    {
                        break;
                    }                    
                }
            }
            return new Status { LastDisassembled = TempLastDisas };
        }
        public static byte[] Fetch(ulong address, int length = 1)
        {
            byte[] output = new byte[length];
            for (byte i = 0; i < length; i++)
            {
                output[i] = CurrentContext.Memory[address + i];
            }
            return output;
        }
        public static byte[] FetchRegister(XRegCode register, RegisterCapacity size, bool IgnoreRex=false)
        {
            if (size == RegisterCapacity.GP_BYTE && register > XRegCode.B && (RexByte == REX.NONE || IgnoreRex))
            {
                return Bitwise.Subarray(CurrentContext.Registers[RegisterCapacity.GP_WORD, register - 4], 1);
            }
            else
            {
                return CurrentContext.Registers[size, register];
            }
        }
        public static void SetRegister(XRegCode register, byte[] data, bool IgnoreRex=false)
        {
            // pretty way has a cost http://prntscr.com/os6et3
            if (data.Length == 1 || data.Length == 2 || data.Length == 4 || data.Length == 8)
            {
                if (data.Length == (int)RegisterCapacity.GP_BYTE && (int)register > 3 && (RexByte == REX.NONE || IgnoreRex)) // setting higher bit of word reg
                { // e.g AH has the same integer value as SP(SP has no higher bit register) so when 0b101 is accessed with byte width we need to sub 4 to get the normal reg code for that reg then set higher bit ourselves 
                    CurrentContext.Registers[RegisterCapacity.GP_WORD, register - 4] = new byte[] { CurrentContext.Registers[RegisterCapacity.GP_WORD, register - 4][0], data[0] };
                }
                else
                {
                    if (data.Length == 4) { data = Bitwise.ZeroExtend(data, 8); }
                    CurrentContext.Registers[(RegisterCapacity)data.Length, register] = data;
                }
            }
            else
            {
                throw new Exception("Control Unit: Cannot infer size of target register");
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
            CurrentContext.Flags.Overlap(input);
        }
        public static byte FetchNext()
        {
            byte Fetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            CurrentContext.InstructionPointer++;
            return Fetched;
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
    }
}
