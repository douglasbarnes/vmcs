// The ModRM provides the decoding the most common operand encoding, the ModRM byte. It is the most true-to-nature IMyDecoded type.
// By default, ModRM bytes are decoded with MR encoding. This assumes the R/M bits to hold the destination and the reg bits to
// hold the source. The only difference between MR and RM encoding is the interpretation. The encoding of the ModRM is determined
// by the opcode itself. The vast majority of opcodes will use MR, and many of those will have a separate opcode for RM encoding.
// This is so a memory location can be used as either the source or the destination.
// For example, The ModRM byte for [RAX], ECX is [00001000] = 0x8. (64bit register is default for a pointer)
// (A = 000, C = 001, D = 010, B = 011 etc)
// This can be broken down into,
// MOD  REG  MEM
// [00][001][000]
// However, two different add opcodes could use a different ModRM encoding.
// e.g,
//    BYTES            DISASSEMBLY 
//    01 08          ADD [RAX], ECX
//    04 08          ADD ECX, [RAX]
// As you can see, both used the same ModRM byte 08, but the 01 add opcode used MR encoding and the 04 add opcode used RM encoding.
// This is generally written as,
//    BYTES            PSEUDO
//    01 08          ADD $RM, $R
//    04 08          ADD $R, $RM
// Not every operation has an opcode for each encoding. There is no set pattern but most arithmetic operations use the following order,
//    1st opcode = ADD RM, R where R = a byte register.
//    2nd opcode = ADD RM, R
//    3rd opcode = ADD R, RM where R = a byte register(This is RM encoding)
//    4th opcode = ADD R, RM (MR encoding)
// A RM operand does not have to be a memory location. Whether it is or not is defined by the MOD bits of the byte.
//   MOD BITS          EFFECT
//     [00]        $RM is pointer
//     [01]        $RM is a pointer with a 1 byte immediate offset
//     [10]        $RM is a pointer with a 4 byte immediate offset
//     [11]        $RM is a register
// When the $RM has an immediate offset(MOD is 1 or 2), it is stored immmediately after the ModRM byte unless there is a SIB byte
// present(coming up shortly). In which case, the immediate is after that. 
// Some example bytes with mod bits set,
//  [11001000] = 0xC8 = EAX, ECX
//  [11000001] = 0xC1 = ECX, EAX
//  [01000001] = 0x41 = [ECX+(an offset byte)], EAX
// Some examples in context,
//    BYTES                DISASSEMBLY
//    01 C8              ADD EAX, ECX
//    04 C8              ADD ECX, EAX
//    04 C1              ADD EAX, ECX
//    01 41 05           ADD [ECX+5], EAX
//    01 81 56 34 23 12  ADD [ECX+0x123456], EAX
// In reality, this is most often used for passing arguments to callees.
// Consider this example,
//  MOV RBP, 0xF (in reality this will be crazy huge address in the stack)
//  MOV EAX, 0x11111111
//  MOV [RBP-0x4], EAX
//  MOV [RBP-0xF], EAX
//  MOV RBX, 0x2222222222222222
//  MOV [RBP-0xC], RBX
// Now the memory at $RBP would look like,
// 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
// 11 11 11 11 22 22 22 22 22 22 22 22 11 11 11 11
// And the next method can read that. 
// But it took quite a few lines to achieve that. What if I wanted to generate a large arithmetic sequence, would it all have to be hard-coded?
// Definitely not! This is where performance can be traded off for reduced execution size.
// Here is a a simple nasm program to write the solutions of a linear equation.
// The example is hard coded to solve 2x+1. It wont work for equations where y,x,m or c are > 255
//
// main:
//  mov rbp, rsp ; make sure rbp has a value, sometimes it's not done automatically
//  mov byte [rbp-0x1], 0x14 ;  20 in hex, the number of values wanted
//  mov byte [rbp-0x2], 0x2 ; the gradient of the slope, coefficient of x
//  mov byte [rbp-0x3], 0x1 ; the y intercept, the constant on the end
//  sub rsp, 0x3 ; 3 bytes were just used at $rsp($rbp was set to $rsp). call would overwrite these with the return pointer otherwise.
//  call slope_calculator
//  nop 
// slope_calculator:
//  movzx rcx, byte [rbp-0x1] ; set up the loop. move the number of results wanted to rcx
//  neg rcx ; negate $rcx, so the counter goes from -20 to 0 for example.(makes life easier later)
// calculator_loop:
//  mov al, cl ; move the counter value into $al
//  neg al ; negate $al so it is now the value of X(but still working backwards, highest to lowest)
//  mul byte [rbp-0x2] ; multiplying can give a bigger result than a byte, so whilst this works nicely for the given example, larger values wont work
//  add al, [rbp-0x3] ; add the constant to the result afterwards to preserve order of operations.
//  mov [rbp+rcx-0xc], al  ; Store the solution of Y in rbp - counter - 0xc($rcx is a twos compliment signed negative, 0xc is the size of the return pointer + argument bytes
//  inc cl ; increment the counter
//  test cl, cl ; check if the new count is equal to 0
//  jnz calculator_loop ; if it isnt, loop again, otherwise return.
//  ret
//
// As you can see, ModRM bytes made accessing the arguments super easy. But how did [rbp+rcx-0xc]  work? 
// Here a SIB byte was used. SIB bytes allow crazy ways of forming pointers. This was a very tame but practical example. A SIB byte allows
// the RM operand of a ModRM byte to be formed from 3 different factors, a scale, a index and a base. There can also be an immediate, but 
// as explained earlier that is encoded in the ModRM. The index is a register that can be multiplied by a coefficient, the scale. Then
// an addition offset register, the base, can be added to that. It is possible to have coefficients 1,2,4,8 and to have no base, no index or neither.
// Neither would imply a 4 byte immediate exact pointer to memory. Generally in x86_64, RIP-relative addressing is favoured, so in this case
// a RIP-relative 4 byte displacement can be encoded in the ModRM byte(doesn't need a SIB). Coefficients of 3, 5, 7 and 9 are possible by using
// the base register as the index register.
// In pseudo,
// Effective address = [($coefficient * $index) + $base + $immediate(if there is one)] 
// Some example SIBs,
//      BYTES                  DISASSEMBLY
//   8D 44 0D F4          LEA EAX, [RCX+RBP-0xC3] 
//   8D 04 00             LEA EAX, [EAX+EAX]
//   8D 04 40             LEA EAX, [EAX*2+EAX]
//   8D 84 05 78 56 34 12 LEA EAX, [0x12345678]
//   8D 04 C1             LEA EAX, [EAX*8+ECX] 
// See the class file for SIB and the ControlUnit for more about SIB bytes.
// 
// It has 3 settings to define behaviour
//  SWAP     - The input will be decoded as a RM encoded ModRM
//  EXTENDED - When a ModRM byte is used in an opcode from the extended table, the reg bits are actually used to encode the opcode. This 
//             turns one byte into 8 different opcodes depending on the ModRM, which is awesome for when said opcode only takes
//             one input, e.g INC,DEC. So by setting EXTENDED, the reg bits will not be interpreted as a source, rather just ignored.
//             Otherwise some arbitary register would be added to the Fetch() call output. 
//  HIDEPTR  - If an operand is a pointer, the preceeding "BYTE PTR" or alike will be ommitted from the disassembly. This is useful for when 
//             the data pointed to is not necessarily important, e.g in a LEA instruction.
using debugger.Util;
using System;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public enum ModRMSettings
    {
        NONE = 0,
        SWAP = 1,
        EXTENDED = 2,
        HIDEPTR = 4,
    }
    public class ModRM : IMyMultiDecoded
    {
        // The mod value can only be one of 4 values/
        public enum Mod
        {
            Pointer,
            PointerImm8,
            PointerImm32,
            Register
        }
        public ControlUnit.RegisterHandle Source;
        private ulong Destination;
        private long Offset;
        public RegisterCapacity Size { get; private set; }
        public ulong EffectiveAddress { get => Destination + (ulong)Offset; }
        private SIB? DecodedSIB;
        private DeconstructedModRM Fields;
        private readonly ModRMSettings Settings;
        private struct DeconstructedModRM
        {
            // This struct provides easily readable properties for masking the correct bits in a ModRM byte to get the desired field.
            // This makes the code a lot more readable and reduces margin of error.
            // A ModRM is constructed like,
            // ------------------------------
            // |   7  6 | 5  4  3 | 2  1  0 |
            // |   MOD  |   REG   |   MEM   |
            // ------------------------------
            // This mask will only return bit 7 and 6 [11000000]
            public readonly Mod Mod { get => (Mod)((Internal_ModRM & 0xC0) >> 6); }

            // This mask will only return bits 5, 4, and 3. The value is increased by 8 if ExtendReg.
            public byte Reg { get => (byte)(((Internal_ModRM & 0x38) >> 3) | (ExtendReg ? 8 : 0)); }

            // Finally, this mask will return bits 2, 1, and 0. The value is increased by 8 if ExtendMem.
            public byte Mem { get => (byte)((Internal_ModRM & 0x7) | (ExtendMem ? 8 : 0)); }

            private readonly byte Internal_ModRM;
            public bool ExtendReg;
            public bool ExtendMem;
            public DeconstructedModRM(byte input)
            {
                Internal_ModRM = input;
                ExtendReg = false;
                ExtendMem = false;
            }
        }
        public void Initialise(RegisterCapacity size)
        {
            Size = size;

            // The source is always a register, so its instance can be stored in a variable.
            Source.Initialise(size);
        }
        public ModRM(ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            Construct(ControlUnit.FetchNext());
        }
        public ModRM(byte input, ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            Construct(input);
        }
        private void Construct(byte input)
        {
            // A method to construct the class from a given input byte.

            Fields = new DeconstructedModRM(input)
            {

                // If the current REX byte has the B or R field set, adjust Mem and Reg accordingly(See ControlUnit)
                ExtendMem = (ControlUnit.RexByte | REX.B) == ControlUnit.RexByte,
                ExtendReg = (ControlUnit.RexByte | REX.R) == ControlUnit.RexByte
            };

            // The source is always known to be a register, so only one instance of a handle ever has to be created.
            Source = new ControlUnit.RegisterHandle((XRegCode)Fields.Reg, RegisterTable.GP);

            // Assume no offset immediately
            Offset = 0;

            // Null $DecodedSIB so it can be checked later if there is a SIB or not.
            DecodedSIB = null;

            // If the Mod is 3, the operands are both registers, so all that needs to happen is that the Destination is equal to the mem field,
            // which is implicitly convertible to an XRegCode.
            if (Fields.Mod == Mod.Register)
            {
                Destination = Fields.Mem;
            }
            else
            {
                // If the Mod is 0 and Mem is 5, the destination is an immediate 4 byte RIP relative displacement.
                if (Fields.Mod == Mod.Pointer && Fields.Mem == 5)
                {
                    // Fetch the next 4 bytes, convert them to an unsigned integer. Remember that FetchNext(),
                    // will automatically increment the InstructionPointer, so it is important to do this first.
                    Offset = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);

                    // Then since the offset is a RIP relative displacement, set the destination to the instruction pointer.
                    Destination = ControlUnit.InstructionPointer;
                }

                // If the mem field is 4, there is a SIB byte to decode.
                else if (Fields.Mem == 4)
                {
                    // Decode a new sib from the next byte
                    DecodedSIB = new SIB(ControlUnit.FetchNext(), Fields.Mod);

                    // Assign variables to the output of the SIB accordingly.
                    Offset = DecodedSIB.Value.OffsetValue;
                    Destination = DecodedSIB.Value.Destination;
                }

                // In any other case, the mem bits represent a pointer. These mem bits represent the same registers as an XRegCode
                else
                {
                    Destination = BitConverter.ToUInt64(new ControlUnit.RegisterHandle((XRegCode)Fields.Mem, RegisterTable.GP, RegisterCapacity.QWORD).FetchOnce(), 0);
                }

                // If the mod bits indicate an immediate offset, add that offset to $offset.
                if (Fields.Mod == Mod.PointerImm8)
                {
                    Offset += (sbyte)ControlUnit.FetchNext();
                }
                else if (Fields.Mod == Mod.PointerImm32)
                {
                    Offset += BitConverter.ToInt32(ControlUnit.FetchNext(4), 0);
                }
            }
        }
        public List<string> Disassemble()
        {
            // A method for disassembling a ModRM class.
            // See Construct()
            // Create a new instance of the DissassembledPointer class to make disassembly easier.
            Disassembly.DeconstructedPointer DestPtr = new Disassembly.DeconstructedPointer() { IndexReg = new ControlUnit.RegisterHandle((XRegCode)Fields.Mem, RegisterTable.GP, Size).Disassemble()[0] };

            // RIP relative offset
            if (Fields.Mem == 5 && Fields.Mod == 0)
            {
                DestPtr.IndexReg = "RIP";
            }

            // Conditions for a SIB
            else if (Fields.Mem == 4 && Fields.Mod != Mod.Register)
            {
                DestPtr = DecodedSIB.Value.Disassemble;
            }


            DestPtr.Offset = Offset;

            // Finalise the disassembly of the destination, 
            string Dest = Disassembly.DisassemblePointer(DestPtr);

            // If the destination is a pointer, add square braces around it.
            // e.g EAX -> [EAX]
            if (Fields.Mod != Mod.Register)
            {
                Dest = $"[{Dest}]";

                // Prepend "WORD PTR" etc if HIDEPTR bit isn't set.
                if ((Settings | ModRMSettings.HIDEPTR) != Settings)
                {
                    Dest = $"{Disassembly.DisassembleSize(Size)} PTR {Dest}";
                }
            }

            // If the ModRM is part of an extended opcode, don't disassemble the source because the source(reg bits) is part of the opcode not the operands.
            if ((Settings | ModRMSettings.EXTENDED) == Settings)
            {
                return new List<string> { Dest };
            }
            else
            {
                // Disassemble the already stored Source register handle.
                string SourceMnemonic = Source.DisassembleOnce();

                // If the SWAP bit is set(RM encoding), return the source and dest the other way round.
                if ((Settings | ModRMSettings.SWAP) == Settings)
                {
                    return new List<string> { SourceMnemonic, Dest };
                }
                else
                {
                    return new List<string> { Dest, SourceMnemonic };
                }
            }
        }
        public List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>();
            byte[] DestBytes;

            // If the mod indicates that the destination is a register, fetch that register. Otherwise, fetch the address at $Destination + $Offset.           
            if (Fields.Mod == Mod.Register)
            {
                DestBytes = new ControlUnit.RegisterHandle((XRegCode)Destination, RegisterTable.GP, Size).Fetch()[0];
            }
            else
            {
                DestBytes = ControlUnit.Fetch(Destination + (ulong)Offset, (int)Size);
            }
            Output.Add(DestBytes);

            // If the ModRM isn't part of an extended opcode, add the source to $Output.
            if (Settings != ModRMSettings.EXTENDED)
            {
                Output.Add(Source.Fetch()[0]);
            }

            // Swap the Destination and Source around if the ModRM has SWAP(RM encoding) set.
            if (Settings == ModRMSettings.SWAP)
            {
                Output.Reverse();
            }
            return Output;
        }
        public void Set(byte[] data) => _set(data, false);
        public void SetSource(byte[] data) => _set(data, true);
        private void _set(byte[] data, bool forceSwap)
        {
            // If $forceSwap is true, but the ModRM already has the SWAP bit set, the swap needs to be swapped back to normal, "unswapped".
            // Like swapping two letters visually, the end result is the same as the start.
            //  a b
            //  b a <- Start here if SWAP bit is set
            //  a b
            // Then if swapping, set the source because the source is really the destination, the source will always be a register.               
            if (forceSwap ^ (Settings | ModRMSettings.SWAP) == Settings)
            {
                Source.Set(data);
            }

            // If the mod bits indicate that the destination is a register it must be treat differently.
            else if (Fields.Mod == Mod.Register)
            {
                // Create a new register handle for the destination and set its value.
                new ControlUnit.RegisterHandle((XRegCode)Destination, RegisterTable.GP, Size).Set(data);
            }
            else
            {
                // Set the memory at pointer *($Destination+$Offset) to $data.
                ControlUnit.SetMemory(Destination + (ulong)Offset, data);
            }
        }
    }
}
