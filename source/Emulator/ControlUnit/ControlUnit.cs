using System;
using System.Collections.Generic;
using debugger.Emulator.Opcodes;
namespace debugger.Emulator
{
    public enum AddressInfo
    {
        NONE=0,
        RIP=1,
        BREAKPOINT=2,
        BAD=4,
    }
    public struct DisassembledLine
    {
        public ulong Address;
        public List<string> Line;
        public AddressInfo Info;
        public DisassembledLine(List<string> line, AddressInfo info, ulong address)
        {
            Line = line;
            Info = info;
            Address = address;
        }
    }
    public struct Status
    {
        public List<DisassembledLine> Disassembly;
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
        public static class LPrefixBuffer
        {
            // https://wiki.osdev.org/X86-64_Instruction_Encoding#Legacy_Prefixes
            // OSDev describes 4 different layers of prefixes that can be applied at one time that can be used to
            // simplify the process of deciding which prefixes replace another, because certain prefixes cannot
            // be present when certain ones are.
            private static readonly PrefixByte[] Prefixes = new PrefixByte[4];
            private static int DetermineIndex(PrefixByte input)
            {                
                if (input == PrefixByte.ADDROVR)       //
                {                                      //
                    return 3;                          // ADDROVR and SIZEOVR are completely independent of any other prefix
                }                                      //
                else if (input == PrefixByte.SIZEOVR)  //
                {
                    return 2;
                }
                else if (input == PrefixByte.REPZ || input == PrefixByte.REPNZ || input == PrefixByte.LOCK) // Only one of REPZ,REPNZ and LOCK can be present at one time
                {
                    return 1;
                }
                else // Anything else covers segment override prefixes. E.g if there is a code segment override byte, then after that a data segment override byte, obviously these two wont work together.
                {
                    return 0;
                }
            }
            public static PrefixByte GetGroup(int group) => Prefixes[group-1];            
            public static void Add(PrefixByte input)
            {
                Prefixes[DetermineIndex(input)] = input; // Take the most recent definition for that prefix index, be it the first or hundreth.
            }
            public static void Clear() => Array.Clear(Prefixes, 0, 4); // Arguably faster than creating a new instance. The old one would have to be destructed at some point.
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
        public static readonly Handle EmptyHandle = new Handle("None", new Context(new MemorySpace(new byte[] { 0x00 })), HandleParameters.NONE); // Create a new handle that will be assigned when the ControlUnit is not in use.
        public static Handle CurrentHandle = EmptyHandle; // On startup, default to no handle
        private static Context CurrentContext { get => CurrentHandle.ShallowCopy(); } // Provide several aliases useful for opcodes
        public static FlagSet Flags { get => CurrentContext.Flags; set { CurrentContext.Flags = value; } }
        public static ulong InstructionPointer { get => CurrentContext.InstructionPointer; private set { CurrentContext.InstructionPointer = value; } }
        public static REX RexByte { get; private set; } = REX.NONE; // A public getter to allow the RexByte of the ControlUnit to be read elsewhere but not set.
        private static Status Execute(bool step)
        {
            // A variable to prevent #UD spam.
            bool HasUDed = false;

            // By default, each opcode has a width of one.
            byte OpcodeWidth = 1;

            // Store the disassembled instructions in a list so it can be reported back to the disassembler after stepping.
            List<DisassembledLine> DisassemblyBuffer = new List<DisassembledLine>(); ;

            while (CurrentContext.InstructionPointer < CurrentContext.Memory.End)
            {
#if DEBUG
                // CurrentContext.InstructionPointer loads native code therefore cannot be viewed in break mode.
                // This makes debugging easier because its easy to see instruction pointer.
                ulong Debug_InstructionPointer = CurrentContext.InstructionPointer;

#endif
                // Fetch next instruction.
                byte Fetched = FetchNext();

                // Check if what was fetched is a prefix
                if (LPrefixBuffer.IsPrefix(Fetched)) 
                {
                    LPrefixBuffer.Add((PrefixByte)Fetched);
                }
                else
                {
                    // The following conditions can only be met immediately before an opcode, i.e no legacy prefixes can follow these.
                    // Any byte that is inclusively in the range of 0x40 and 0x4F must be a REX.
                    // This is because technically a REX prefix is any byte which has an upper nibble
                    // equal to [0100] in binary. If you extend this nibble to include the lower half
                    // of the byte, it becomes [01000000] which is equal to 0x40, or 64 decimal.
                    // After identifying the prefix, I can identify its characteristics.
                    // However firstly, there is a universal characteristic that is applied to all cases
                    // where a REX prefix is present. If I wanted to access the lower byte of the stack pointer,
                    // base pointer, source index or destination registers, well I wouldn't be able to without
                    // fetching the whole lower word. Now with a REX prefix, I can opt to lose my access to
                    // the higher byte registers(AH,CH,DH,BH) and be able to access SPL, BPL, SIL, DIL, which
                    // are accessed with the same XRegCode as you would use to access larger selections of the register.
                    // A quick demonstration,
                    //           BYTES        DISASSEMBLY
                    //             B4 00     mov ah, 0x00 
                    //          40 B4 00     mov spl, 0x00
                    // This is exactly the case when you would use a REX prefix without any other undesired
                    // characteristics.
                    // There are 4 different "fields" that can be combined:
                    //  Rex.W: If the 3rd bit is set, [01001000](When talking about an individual byte,
                    //         byte positions are zero based), the opcode will use 64 bit registers instead
                    //         of the default 32. There is some variation on this such as MOVSXD which will
                    //         take a 64 bit register destination and a 32 bit R/M as its operands. Another
                    //         exception would be for a few instructions which default to 64 bit, such as
                    //         push, pop and string operations.
                    //  Rex.R: If the 2nd bit is set, [01000100], this means that the reg field of a ModRM
                    //         byte for the next instruction will be extended. For example in a ModRM byte,
                    //         the reg and R/M fields are denoted by 3 bits in the ModRM byte, however there
                    //         are 16 GP registers available in x86-64, so by setting the Rex.R bit, you are 
                    //         lending the reg field another bit(which is only going to matter if the Rex.R
                    //         is set). For example, the A register is selected by its XRegCode, 0. If I took
                    //         an instruction that targeted EAX, say
                    //           BYTES        DISASSEMBLY
                    //             B8 10     mov eax, 0x10
                    //         then put a Rex.R byte infront of it...
                    //          42 B8 10     mov r8d, 0x10
                    //         It now targets the lower 32 bits of the r8 register(because there was no Rex.W to
                    //         make it target the whole 64). XRegCode is defined in Emulator.RegisterGroup.cs.
                    //         If there is no ModRM byte in the next instruction, the Rex.R is ignored.
                    //  Rex.X: If the 1st bit is set, [01000010], the same princple as Rex.R is applied to
                    //         the index field in the SIB byte of the next opcode. It is also ignored if there
                    //         if no ModRM or SIB in the next instruction. Eight is added to the index field of
                    //         the SIB, and can then target the extra registers added in x86-64 just like a ModRM
                    //         reg field could in Rex.R. To remember which is the index field, just think of the
                    //         name "Scaled Index Byte", and it become obvious that the index is the field multiplied.
                    //  Rex.B: If the 0th bit is set, a more general approach to Rex.R/Rex.X is taken. Every field
                    //         that hasn't been mentioned already(anything that isn't the index or reg field) will be
                    //         extended, so the R/M field of the ModRM, the base field of the SIB, or if neither of
                    //         those are used, the implicit register inferred from the opcode will be extended.
                    //
                    // Lets take an example with some crazy MOV instructions. I will write the lower
                    // nibble of the REX byte next to the disassembly.
                    //              BYTES                DISASSEMBLY
                    //           89 00                  mov [rax], eax // Plain and simple, no REX
                    //        41 89 00                  mov [r8], eax // Rex.B [0001] 
                    //        44 89 00                  mov [rax], r8d // Rex.R [0100]
                    //        42 89 04 05 00 00 00 00   mov [r8], eax // Rex.X [0010] with a SIB that does nothing
                    //           89 04 00               mov [rax*1+rax], eax // A plain SIB where rax is the index and base
                    //        41 89 04 00               mov [rax*1+r8], eax // Rex.B [0001] with a SIB
                    //        41 B8 78 56 34 12         mov r8d, 0x12345678 // 0xB8 is the implicit immediate register move for 
                    //                                                      // the A register. This demonstrates the change because
                    //                                                      // of the Rex.B prefix                                                      
                    // (Why do the pointers include 64 bit registers? All pointer values default to 64 bit.
                    //  An ADDROVR legacy prefix would change them to 32 bit, but I want to keep it simple)
                    // But what if we wanted a to mix and match?         
                    //              BYTES                DISASSEMBLY        
                    //         49 B8 00                 mov r8, rax // Rex.WB [1001](Rex.W promotes all operands)
                    //         46 89 04 00              mov [r8*1+rax], r8d // Rex.RX [0110] 
                    //         4F 89 04 00              mov [r8*1+r8],  r8 // Rex.WRXB [1111]                 
                    if (Fetched >= 0x40 && Fetched < 0x50) 
                    {
                        // This assignment does two things,
                        //  1. Mask out the upper values representing the 0x40 because I've already identified it's a REX byte, I don't need them any more
                        //  2. Shift the masked byte left once. This allows the enum to differentiate between an empty rex and no rex at all. An empty rex
                        //     being a rex with no extra characteristics(W,R,X,B).
                        // Then after this, fetch again because otherwise the REX byte would be used as an opcode.
                        RexByte = (REX)((Fetched << 1) & 0b00011110); 
                        Fetched = FetchNext();                        
                    }                                                                           
                    if (Fetched == 0x0F)
                    {
                        // 0x0F is a prefix of sort to declare that the two-byte opcode map will be used for the next instruction. There are a lot of instructions in assembly and can
                        // definitely not be covered in a single byte, so less common operations are pushed onto this second opcode map where a completely different opcode will be used.
                        OpcodeWidth = 2;                                          
                        Fetched = FetchNext();
                    }

                    // Mechanism for detecting and handling invalid/unimplemented opcodes. Errors in alternative tables return null.
                    OpcodeCaller CurrentCaller;
                    IMyOpcode CurrentOpcode;
                    if (OpcodeTable[OpcodeWidth].TryGetValue(Fetched, out CurrentCaller) && (CurrentOpcode = CurrentCaller()) != null)
                    {                       
                        // If disassembling, whether the instruction is executed or not is not of importance(so long as the opcode class is written along with convention),
                        // Conversely, if executing, whether the instruction is disassembled or not doesn't matter. Together, this check speeds up the program a lot.
                        if ((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
                        {
                            DisassemblyBuffer.Add(
                                new DisassembledLine(CurrentOpcode.Disassemble(), 
                                                     CurrentContext.Breakpoints.Contains(InstructionPointer) ? AddressInfo.BREAKPOINT : AddressInfo.NONE
                                                     , InstructionPointer));
                        }
                        else
                        {
                            CurrentOpcode.Execute();
                        }
                    }
                    else
                    {
                        // If the opcode is not present in the OpcodeTable, 
                        //  - Tell the user by changing the disassembly
                        //  - Throw a #UD when executed.
                        if ((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
                        {
                            DisassemblyBuffer.Add(new DisassembledLine(new List<string> { "BAD INSTRUCTION" }, AddressInfo.BAD, InstructionPointer));
                        }

                        // Only tell the user that there was a UD once per step/run. This stops them getting spammed with message boxes.
                        else if(!HasUDed)
                        {
                            HasUDed = true;
                            RaiseException(Logging.LogCode.INVALID_OPCODE);
                        }
                    }
                    
                    // Reset variables after opcode is executed
                    OpcodeWidth = 1;
                    LPrefixBuffer.Clear();
                    RexByte = REX.NONE;
                                    
                    // If stepping or hit a breakpoint(and honouring breakpoints), stop executing.
                    if (step || ((CurrentHandle.HandleSettings | HandleParameters.NOBREAK) == CurrentHandle.HandleSettings && CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer)))
                    {
                        break;
                    }                    
                }
            }            
            return new Status { Disassembly = DisassemblyBuffer, InstructionPointer = InstructionPointer };
        }
        public static void SetMemory(ulong address, byte[] data)
        {
            // Set $data.Length bytes after and including $address with the bytes in data[].
            for (uint Offset = 0; Offset < data.Length; Offset++)
            {
                CurrentContext.Memory[address + Offset] = data[Offset];
            }
        }
        public static byte[] Fetch(ulong address, int length = 1)
        {
            // Fetch $length bytes after and including address
            byte[] output = new byte[length];
            for (byte i = 0; i < length; i++)
            {
                output[i] = CurrentContext.Memory[address + i];
            }
            return output;
        }
        public static byte FetchNext()
        {
            // Fetch the next byte at $instruction_pointer
            byte Fetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            CurrentContext.InstructionPointer++;
            return Fetched;
        }
        public static byte[] FetchNext(int count)
        {
            // Fetch the next x bytes after and including $instruction_pointer
            byte[] Output = new byte[count];
            for (int i = 0; i < count; i++)
            {
                Output[i] = FetchNext();
            }
            return Output;
        }
        public static void Jump(ulong address)
        {
            // Change the instruction pointer to $address, such that the next byte read will be $address if the handle has NOJMP set.
            if ((CurrentHandle.HandleSettings | HandleParameters.NOJMP) != CurrentHandle.HandleSettings)
            {
                CurrentContext.InstructionPointer = address;
            }
        }        
        public static void SetFlags(FlagSet input) => Flags = Flags.Overlap(input);       
        public static List<Register> FetchAll(RegisterCapacity size)
        {
            // Turn all GP registers into a register struct list, which contains all the information higher layered caller need to know.
            // A list of byte arrays would be unhelpful in disassembling.
            // A dictionary is unnecessary as the output can be referenced like Output[(int)XRegCode.A] to get the A register etc.
            List<Register> Output = new List<Register>(0xf);
            for (int i = 0; i < 0xf; i++)
            {
                Output.Add(new Register(new RegisterHandle((XRegCode)i, RegisterTable.GP, size)));
            }
            return Output;
        }
        public static void RaiseException(Logging.LogCode exception)
        {
            // It is expected that divide instructions will throw this error when disassembling. This is because 
            if((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
            {
                return;
            }
            Logging.Logger.Log(exception, $"State: \nHandle:{CurrentHandle.HandleName}\nInstruction Pointer:{InstructionPointer}");
        }
    }
}
