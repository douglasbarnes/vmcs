using System;
using System.Collections.Generic;
using debugger.Emulator.Opcodes;
namespace debugger.Emulator
{
    public struct Status
    {
        // A struct that is returned to the caller of ControlUnit.Execute() with some useful data that saves
        // more time in handle.Invoke()
        public enum ExitStatus
        {
            BreakpointReached=0
        }
        public ExitStatus ExitCode; // initialises to breakpoint reached
        public List<string> LastDisassembled;
        public ulong InstructionPointer;
    }
    public enum PrefixByte
    {
        // An enum for defining all legacy prefixes for use and identification throughout the program.
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
        // A flags-attributed enum for defining REX prefixes that can contain a number of characteristics.
        NONE=0,
        EMPTY=1,
        B=2,
        X=4,
        R=8,
        W=16
    }
    public struct Register
    {
        // A struct for holding the values of registers without access to them directly.
        // Essentially a deep copy of a register without unnecessary methods and data that the
        // higher levels of operation do not need.
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
            // LegacyPrefixBuffer
            //
            // https://wiki.osdev.org/X86-64_Instruction_Encoding#Legacy_Prefixes
            // OSDev describes 4 different layers of prefixes that can be applied at one time that can be used to
            // simplify the process of deciding which prefixes replace another, because certain prefixes cannot
            // be present when certain ones are. LPrefixBuffer is defines this behaviour.
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
        private static Context CurrentContext 
        { 
            // "CurrentContext" is much more intuitive than "CurrentHandle.ShallowCopy()"            
            // ShallowCopy() is used because the context needs to be interacted directly, so the caller sees the changes made.
            // If DeepCopy() was used, a separate instance would be created and the changes would only be seen by the callee.
            get => CurrentHandle.ShallowCopy(); 
        }
        public static FlagSet Flags 
        {            
            get => CurrentContext.Flags; 
            set 
            { 
                CurrentContext.Flags = value; 
            } 
        }
        public static ulong InstructionPointer 
        { 
            // CurrentContext is private, but classes still have a need to view the instruction pointer. For example, RIP relative addressing in ModRM bytes.
            get => CurrentContext.InstructionPointer; 
            
            // Jump() is available for setting the instruction pointer. There are cases where this would want to be ignored.
            private set 
            { 
                CurrentContext.InstructionPointer = value; 
            } 
        }
        
        public static REX RexByte 
        {
            // A public getter to allow the RexByte of the ControlUnit to be read elsewhere.
            get; 
            // I don't think anything outside of this class has any good reason to be setting the REX byte.
            private set;
            // Default the RexByte to REX.NONE. REX.EMPTY is very different, read the section in Execute() about rex bytes.
        } = REX.NONE; 
        private static Status Execute(bool step)
        {
            // By default, each opcode has a width of one.
            byte OpcodeWidth = 1;

            // Store the last disassembled instruction in a variable so it can be reported back to the disassembler(if disassembling) after stepping.
            List<string> TempLastDisas = new List<string>();
            while (CurrentContext.InstructionPointer < CurrentContext.Memory.End)
            {
#if DEBUG
                // CurrentContext.InstructionPointer loads native code therefore cannot be viewed in break mode. 
                // This makes debugging way easier because the instruction pointer can be seen easily.
                ulong Debug_InstructionPointer = CurrentContext.InstructionPointer; 
#endif
                // Fetch the next instruction or prefix to execute. If the instruction pointer derailed or the opcode is unrecognized, throw a #UD.
                byte Fetched = FetchNext(); 
                
                // If a prefix was fetched, add it to the LPrefixBuffer
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
                        // 0x0F is a prefix of sort to declare that the two-byte opcode map will be used for the next instruction. There are a lot of instructions in x86-64 as it is CISC
                        // based and so can definitely not be covered in a single byte, so less common operations are pushed onto this second opcode map where a completely different opcode will be used.
                        OpcodeWidth = 2;                                          
                        
                        // Once again, the next byte needs to be fetched or 0x0F would be used as an opcode, which would #UD
                        Fetched = FetchNext();
                    }

                    // Try get the delegate for initialising an opcode with the settings that can be implied from this specific byte. For example, there are two different bytes for adding
                    // 32 bit registers and for adding 8 bit registers.
                    OpcodeCaller CurrentOpcodeCaller;
                    if(!OpcodeTable[OpcodeWidth].TryGetValue(Fetched, out CurrentOpcodeCaller))
                    {
                        // If there are no opcodes matching, throw a #UD. This is also true in an actual processor that covers every instruction, there are a few gaps in the tables.
                        RaiseException(Logging.LogCode.INVALID_OPCODE);
                    }

                    // Run the delegate to get an instance of the opcode and call its constructor.
                    IMyOpcode CurrentOpcode = CurrentOpcodeCaller();

                    // If this handle is disassembling, don't waste time executing the opcode(the constructor should do just enough for it to disassemble correctly)
                    // Also, if it isn't disassembling, don't waste time disassembling the opcode, only execute it.
                    if ((CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
                    {
                        TempLastDisas = CurrentOpcode.Disassemble();
                    } 
                    else
                    {
                        CurrentOpcode.Execute();
                    }  

                    // Reset everything that needs to be reset for the next opcode, for example one REX byte doesn't mean every instruction will use that REX byte, it is forgotten
                    // after the opcode it prefixed is executed.
                    OpcodeWidth = 1;
                    LPrefixBuffer.Clear();
                    RexByte = REX.NONE;
                                    
                    // If Execute() was called with step true, or reacheda  breakpoint, break.
                    if (step || CurrentContext.Breakpoints.Contains(CurrentContext.InstructionPointer))
                    {
                        break;
                    }                    
                }
            }
            // Return some things useful to the caller. TempLastDisas will be an empty list if the handle doesn't have DISASSEMBLEMODE set
            return new Status { LastDisassembled = TempLastDisas, InstructionPointer = InstructionPointer };
        }
        public static void SetMemory(ulong address, byte[] data)
        {
            // Take the address passed and overwrite $data.Length bytes after(and including) that address
            for (uint Offset = 0; Offset < data.Length; Offset++)
            {
                CurrentContext.Memory[address + Offset] = data[Offset];
            }
        }
        public static byte[] Fetch(ulong address, int length = 1)
        {
            // Fetch returns a byte array regardless of $length because it works nicely with other byte arrays rather than having to juggle data types.            
            byte[] output = new byte[length];
            for (byte i = 0; i < length; i++)
            {
                output[i] = CurrentContext.Memory[address + i];
            }
            return output;
        }
        public static byte FetchNext()
        {
            // Fetch one byte at the InsturctionPointer, and since there is only one byte take the 0th index of returned array.
            byte Fetched = Fetch(CurrentContext.InstructionPointer, 1)[0];
            // Increment the instruction pointer. I can think of no situation where the address at pointer $IP would be fetched and this would not be wanted.
            CurrentContext.InstructionPointer++;
            return Fetched;
        }
        public static byte[] FetchNext(int count)
        {
            // A loop to run FetchNext() multiple times. Useful for fetching immediates.
            byte[] Output = new byte[count];
            for (int i = 0; i < count; i++)
            {
                Output[i] = FetchNext();
            }
            return Output;
        }
        public static void Jump(ulong address)
        {
            // Give non class members a way to change the instruction pointer. For example, the JMP instruction.
            // However if the handle has NOJMP on, honour that. If the handle is disassembling, it has no need
            // to follow branches, it just wants to disassemble every instruction. If you did offset your code
            // to avoid disassembly, you really shouldn't do that.
            if ((CurrentHandle.HandleSettings | HandleParameters.NOJMP) != CurrentHandle.HandleSettings)
            {
                CurrentContext.InstructionPointer = address;
            }
        }        
        public static void SetFlags(FlagSet input) 
        {
            // Overwrite the flags with an input, but if any flags in the input are FlagState.UNDEFINED, don't set them.
            // Allows a single or couple of flags to be set easily, or all of them if wanted.
            Flags = Flags.Overlap(input);
        }
        public static List<Register> FetchAll(RegisterCapacity size)
        {
            // A method for higher-layered callers to interact with the control unit with handle.Invoke() . E.g, the user interface wants to display the registers            
            List<Register> Output = new List<Register>(0xf);
            for (int i = 0; i < 0xf; i++)
            {
                // I don't want to give the caller an instance of the register, such as RegisterHandle() where they could change register values unconventionally.
                // If this was wanted, the caller could use methods in the Handle instead.
                // As a Register is a struct, if the caller changes any its properties, they do not affect the actual registers.
                // The Register struct constructor sets up everything given a RegisterHandle.
                Output.Add(new Register(new RegisterHandle((XRegCode)i, RegisterTable.GP, size)));
            }
            return Output;
        }
        public static void RaiseException(Logging.LogCode exception)
        {
            // In disassemble mode, no opcode has Execute() called, therefore every register and value will most likely be 0. This means that every division will
            // throw a divide error, but thats fine so long as it's in the dissassemble handle.
            if(exception == Logging.LogCode.DIVIDE_BY_ZERO && (CurrentHandle.HandleSettings | HandleParameters.DISASSEMBLEMODE) == CurrentHandle.HandleSettings)
            {
                return;
            }

            // Let the logger class handle the rest.
            Logging.Logger.Log(exception, $"State: \nHandle:{CurrentHandle.HandleName}\nInstruction Pointer:{InstructionPointer}");
        }
    }
}
