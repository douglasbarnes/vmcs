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
        REXW = 0x48
    }
    public static partial class ControlUnit
    {
        
        public static readonly Handle EmptyHandle = new Handle("None", new Context(new MemorySpace(new byte[] { 0x00 })), HandleParameters.None);
        public static Handle CurrentHandle = EmptyHandle;
        private static Context CurrentContext { get => CurrentHandle.DeepCopy(); }
        public static FlagSet Flags { get => CurrentContext.Flags; }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer; set => CurrentContext.InstructionPointer = value; }
        public static MemorySpace Memory { get => CurrentContext.Memory; }
        public static List<PrefixByte> PrefixBuffer { get; private set; } = new List<PrefixByte>();
        //public static readonly RegisterCapacity CurrentCapacity { get; set; }

        private static Status Execute(bool step)
        {
            byte OpcodeWidth = 1;
            Opcodes.Opcode CurrentOpcode = null;
            List<string> TempLastDisas = new List<string>();
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
                    CurrentOpcode = OpcodeTable[OpcodeWidth][Fetched]();
                    CurrentOpcode.Execute();
                    OpcodeWidth = 1;
                    PrefixBuffer = new List<PrefixByte>();
                    if((CurrentHandle.HandleSettings | HandleParameters.IsDisassembling) == CurrentHandle.HandleSettings)
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
        public static byte[] FetchRegister(ByteCode register, RegisterCapacity size)
        {
            if (size == RegisterCapacity.BYTE && (int)register > 3)
            {
                return Bitwise.Subarray(CurrentContext.Registers[register - 4, RegisterCapacity.WORD], 1);
            }
            else
            {
                return CurrentContext.Registers[register, size];
            }
        }
        public static void SetRegister(ByteCode register, byte[] data)
        {
            // pretty way has a cost http://prntscr.com/os6et3
            if (data.Length == 1 || data.Length == 2 || data.Length == 4 || data.Length == 8)
            {
                if (data.Length == 1 && (int)register > 3) // setting higher bit of word reg
                { // e.g AH has the same integer value as SP(SP has no higher bit register) so when 0b101 is accessed with byte width we need to sub 4 to get the normal reg code for that reg then set higher bit ourselves 
                    CurrentContext.Registers[register - 4, RegisterCapacity.WORD] = new byte[] { CurrentContext.Registers[register - 4, RegisterCapacity.BYTE][0], data[0] };
                }
                else
                {
                    if (data.Length == 4) { data = Bitwise.ZeroExtend(data, 8); }
                    CurrentContext.Registers[register, (RegisterCapacity)data.Length] = data;
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
