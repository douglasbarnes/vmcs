using System;
using System.Collections.Generic;
using debugger.Util;
using debugger.Emulator.Opcodes;
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
        public ulong InstructionPointer;
    }
    public enum PrefixByte
    {
        NONE = 0,
        CS = 0x2E,
        SS = 0x36,
        DS = 0x3E,
        ES = 0x26,
        FS = 0x64,
        GS = 0x65,
        ADDROVR = 0x67,
        SIZEOVR = 0x66,
        LOCK = 0xF0,
        REPNZ =0xF2,
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
        public static class LPrefixBuffer
        {            
            private static readonly PrefixByte[] Prefixes = new PrefixByte[4];
            private static int DetermineIndex(PrefixByte input)
            {
                if (input == PrefixByte.ADDROVR) //https://wiki.osdev.org/X86-64_Instruction_Encoding#Legacy_Prefixes
                {
                    return 3;
                }
                else if (input == PrefixByte.SIZEOVR)
                {
                    return 2;
                }
                else if (input == PrefixByte.REPZ || input == PrefixByte.REPNZ || input == PrefixByte.LOCK)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            public static PrefixByte GetGroup(int group) => Prefixes[group-1];            
            public static void Add(PrefixByte input)
            {
                Prefixes[DetermineIndex(input)] = input;
            }
            public static void Clear() => Array.Clear(Prefixes, 0, 4);
            public static bool Contains(PrefixByte input) => Prefixes[DetermineIndex(input)] == input;
            public static bool IsPrefix(byte input) => input == (byte)PrefixByte.CS ||
                                                       input == (byte)PrefixByte.SS ||
                                                       input == (byte)PrefixByte.DS ||
                                                       input == (byte)PrefixByte.ES ||
                                                       input == (byte)PrefixByte.FS ||
                                                       input == (byte)PrefixByte.GS ||
                                                       input == (byte)PrefixByte.ADDROVR ||
                                                       input == (byte)PrefixByte.SIZEOVR ||
                                                       input == (byte)PrefixByte.LOCK ||
                                                       input == (byte)PrefixByte.REPNZ ||
                                                       input == (byte)PrefixByte.REPZ;
        }
        public static readonly Handle EmptyHandle = new Handle("None", new Context(new MemorySpace(new byte[] { 0x00 })), HandleParameters.NONE);
        public static Handle CurrentHandle = EmptyHandle;
        private static Context CurrentContext { get => CurrentHandle.ShallowCopy(); }
        public static FlagSet Flags { get => CurrentContext.Flags; }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer;
            private set
            {
                CurrentContext.InstructionPointer = value;
            }
        }
        public static REX RexByte = REX.NONE;
        private static Status Execute(bool step)
        {
            byte OpcodeWidth = 1;
            List<string> TempLastDisas = new List<string>();
            while (CurrentContext.InstructionPointer <= CurrentContext.Memory.End)
            {
                byte Fetched = FetchNext();                
                if (LPrefixBuffer.IsPrefix(Fetched))
                {
                    LPrefixBuffer.Add((PrefixByte)Fetched);
                }
                else
                {
                    if (Fetched >= 0x40 && Fetched < 0x50 && OpcodeWidth == 1) //Rex must be immediately before the opcode
                    {// shift to avoid 0(NONE)
                        RexByte = (REX)((Fetched << 1) & 0b00011110); // remove upper half so [flags] can be inferred
                        Fetched = FetchNext();
                    }
                    if (Fetched == 0x0F)
                    {
                        OpcodeWidth = 2;
                        Fetched = FetchNext();
                    }
                    IMyOpcode CurrentOpcode = OpcodeTable[OpcodeWidth][Fetched]();
                    if ((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
                    {
                        TempLastDisas = CurrentOpcode.Disassemble();
                    } 
                    else
                    {
                        CurrentOpcode.Execute();
                    }
                    
                    OpcodeWidth = 1;
                    LPrefixBuffer.Clear();
                    RexByte = REX.NONE;
                                    
                    if (step || CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer))
                    {
                        break;
                    }                    
                }
            }
            return new Status { LastDisassembled = TempLastDisas, InstructionPointer = InstructionPointer };
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
        public static void Jump(ulong address)
        {
            if ((CurrentHandle.HandleSettings | HandleParameters.NOJMP) != CurrentHandle.HandleSettings)
            {
                CurrentContext.InstructionPointer = address;
            }
        }
    }
}
