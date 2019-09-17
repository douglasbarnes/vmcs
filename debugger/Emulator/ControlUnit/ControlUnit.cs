using System;
using System.Collections.Generic;
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
    public struct Register
    {
        public readonly byte[] Value;
        public readonly string Mnemonic;
        public Register(ControlUnit.RegisterHandle input)
        {
            Value = input.FetchOnce();
            Mnemonic = input.DisassembleOnce();
        }
    }
    public static partial class ControlUnit
    {
        private static class StaticHandles
        {
            public static readonly RegisterHandle _CL = new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.BYTE, RegisterHandleSettings.NO_INIT);
            
        }
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
        public static FlagSet Flags { get => CurrentContext.Flags; set { CurrentContext.Flags = value; } }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer; private set { CurrentContext.InstructionPointer = value; } }
        public static REX RexByte = REX.NONE;        
        private static Status Execute(bool step)
        {
            byte OpcodeWidth = 1;
            List<string> TempLastDisas = new List<string>();
            while (CurrentContext.InstructionPointer < CurrentContext.Memory.End)
            {
#if DEBUG
                ulong Debug_InstructionPointer = CurrentContext.InstructionPointer;
#endif
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
                    if(Fetched == 0xC5)
                    {
                        DecodeVEX(ref Fetched);
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
        public static void SetMemory(ulong address, byte[] data)
        {
            for (uint Offset = 0; Offset < data.Length; Offset++)
            {
                CurrentContext.Memory[address + Offset] = data[Offset];
            }
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
        public static byte FetchNext()
        {
            byte Fetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            CurrentContext.InstructionPointer++;
            return Fetched;
        }
        public static byte[] FetchNext(int count)
        {
            byte[] Output = new byte[count];
            for (int i = 0; i < count; i++)
            {
                Output[i] = FetchNext();
            }
            return Output;
        }
        public static void Jump(ulong address)
        {
            if ((CurrentHandle.HandleSettings | HandleParameters.NOJMP) != CurrentHandle.HandleSettings)
            {
                CurrentContext.InstructionPointer = address;
            }
        }        
        private static void DecodeVEX(ref byte fetched)
        {
            RexByte = (REX)((~fetched << 1) & 0b00001000); // ~ = negated, ones compliment

            fetched = FetchNext();
        }
        public static void SetFlags(FlagSet input) => Flags = Flags.Overlap(input);       
        public static List<Register> FetchAll(RegisterCapacity size)
        {
            List<Register> Output = new List<Register>(0xf);
            for (int i = 0; i < 0xf; i++)
            {
                Output.Add(new Register(new RegisterHandle((XRegCode)i, RegisterTable.GP, size)));
            }
            return Output;
        }
    }
}
