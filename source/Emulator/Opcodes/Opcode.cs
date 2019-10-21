// Opcode provides the base class for the vast majority of opcodes in the program. It provides the back-end almost entirely,
// such that opcodes will rarely ever have to interact with the control unit. An exception for this would be setting flags. This
// is because its already super simple, see ControlUnit.
// The following features are provided,
//  - Generalied condition codes and condition testing(See Util.Disassembly and TestCondition()).
//  - Definitive handling of IMyDecoded objects, including disassembly, fetching and setting(See Set() and Fetch().
//  - Synchronisation of input and opcode capacities(See Capacity).
//  - Settings that can be assigned to each opcode to define implied behaviour(See SetRegCap()).
//  - Simple and consistent stack management methods(See StackPush(), StackPop()).
using debugger.Emulator.DecodedTypes;
using debugger.Util;
using System;
using System.Collections.Generic;
using static debugger.Emulator.ControlUnit;
namespace debugger.Emulator.Opcodes
{
    public enum Condition // See Util.Disassembly
    {
        NONE,
        A,
        NC,
        C,
        NA,
        RCXZ,
        Z,
        NZ,
        G,
        GE,
        L,
        LE,
        O,
        NO,
        S,
        NS,
        P,
        NP
    }
    [Flags]
    public enum OpcodeSettings
    {
        // Default behaviour
        NONE = 0,

        // Force byte capacity on the opcode
        BYTEMODE = 1,

        // Nothing explicitly in this class, but can be tested for by derived classes. 
        SIGNED = 2,
    }
    public abstract class Opcode : IMyOpcode
    {
        private RegisterCapacity _capacity;
        protected RegisterCapacity Capacity
        {
            get => _capacity;
            set
            {
                // Re-initialise the input with the new capacity. Certain decoded types such as Immediate will ignore this.
                Input.Initialise(value);

                _capacity = value;
            }
        }
        public readonly OpcodeSettings Settings;
        private readonly string Mnemonic;
        private readonly IMyDecoded Input;
        public Opcode(string opcodeMnemonic, IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE, RegisterCapacity opcodeCapacity = RegisterCapacity.NONE)
        {
            Mnemonic = opcodeMnemonic;
            Input = input;
            Settings = settings;

            // If no capacity was provided, use the SetRegCap() method to try and automatically infer it. This will
            // work with some opcodes, but not others. See SetRegCap().
            Capacity = (opcodeCapacity == RegisterCapacity.NONE) ? SetRegCap() : opcodeCapacity;
        }
        protected List<byte[]> Fetch() => Input.Fetch();
        protected void Set(byte[] data) => Input.Set(data);
        protected void Set(byte[] data, int operandIndex) => ((DecodedCompound)Input).Set(data, operandIndex);
        public abstract void Execute();
        public virtual List<string> Disassemble()
        {
            // Start with the mnemonic
            List<string> Output = new List<string>() { Mnemonic };

            // Add the disassembly of the input
            Output.AddRange(Input.Disassemble());

            return Output;
        }
        protected RegisterCapacity SetRegCap()
        {
            // SetRegCap() can be used to determine the register capacity if the opcode has a
            // default working capacity of a DWORD, that can be extended to a QWORD with a rex.w
            // byte, or reduced to a WORD with a SIZEOVR legacy prefix. If the opcode is a
            // BYTE capacity variant, that can also be inferred by calling the constructor
            // with OpcodeSettings.BYTEMODE as a parameter, which will have priority over any
            // other. 
            // Here are some examples,
            //    BYTES           DISASSEMBLY
            //    01 C0           ADD EAX,EAX
            //  48 01 C0          ADD RAX,RAX
            //  66 01 C0          ADD AX,AX
            //    00 C0           ADD AL,AL
            // As you can see, 01 was the byte of the opcode, ADD, C0 was the ModRM for EAX,EAX.
            // When the rex.w prefix was present(0x48), the operands became QWORD registers.
            // When the SIZEOVR prefix was present(0x66), the operands became WORD registers.
            // Finally when the opcode byte 0x00 was used, this implied that the add was for
            // two byte registers rather than two dword registers(as the first defaulted to).
            // Not every opcode exhibits this behaviour, for example
            //    BYTES           DISASSEMBLY
            //     50             PUSH RAX
            //  48 63 C0          MOVSXD RAX, EAX
            //   FF 20            JMP [RAX]
            // Therefore the constructor gives the option for the caller to override the capacity
            // with its own definitions, or SetRegCap() can be called in the function. As meantioned
            // in the summary, the input will automatically be adjusted to the new register capacity
            // when the Capacity here is changed, see the Capacity property. If there are multiple
            // conditions met that would bring ambiguity to the capacity, the priority can be inferred from
            // below, however this would be a case of invalid input, as there is no case where there should
            // be a REX.W as well as a SIZEOVR for example.

            // If byte mode is forced(This could still be overriden by setting a capacity in the constructor,
            // but there really should be no reason to)
            if ((Settings | OpcodeSettings.BYTEMODE) == Settings)
            {
                return RegisterCapacity.BYTE;
            }

            // If a Rex.W is present
            else if ((RexByte | REX.W) == RexByte)
            {
                return RegisterCapacity.QWORD;
            }

            // If a SIZEOVR is present.
            else if (LPrefixBuffer.Contains(PrefixByte.SIZEOVR))
            {
                return RegisterCapacity.WORD;
            }

            // Default to DWORD.
            return RegisterCapacity.DWORD;
        }

        // Some static register handles to prevent reinstancing them often.
        private static readonly RegisterHandle ECX = new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.DWORD);
        private static readonly RegisterHandle RCX = new RegisterHandle(XRegCode.C, RegisterTable.GP, RegisterCapacity.QWORD);
        private static readonly RegisterHandle StackPointer = new RegisterHandle(XRegCode.SP, RegisterTable.GP, RegisterCapacity.QWORD);
        protected bool TestCondition(Condition condition)
            => condition switch
            {
                // It takes some intuition to understand why these predicates work with flags like this.
                // To understand it every condition must be modelled as a subtraction, you must first
                // consider the output of A - B before considering A < B.
                // Here I will demonstrate a few,
                // If A < B, the result of A - B will be negative. This can also be seen in the converse,
                // as if B > A, the result of B - A will be positive. This is where signs can be used
                // to spot things like this. Lets focus on A < B. If A < B => A - B < 0, therefore if
                // this was in assembly, SUB A,B (pretend these are registers) would set the carry flag because
                // there was a borrow out of the MSB(Reading the Bitwise class would help understanding this).
                // This condition code would be either B(below), NAE(not above or equal), or C(carry). I generally
                // favour the latter where possible because its meaning is easier to infer, and so is used throughout
                // the program, but all three are equal.
                // There is a small detail in assembly where part of the opcode has specific bits set depending on
                // its condition. E.g if the last 4 bits of the opcode are 0100, it will have the equal/zero condition.
                // Naturally this is only for certain opcodes such as jmp, cmp, and set. Another important thing to consider
                // is the meaning of "above", "below", "less", and "greater". Above and below indicate a condition for a
                // signed number, therefore make use of the Carry and Zero flags. Less and greater indicate a condition
                // for an unsigned number, therefore make use of the Overflow, Sign, and Zero flags.
                // More about conditions can be found in Util.Disassembly.

                // If there is no carry and no zero, the subtraction must have yielded a positive non zero result,
                // therefore X - Y > 0 => X > Y.
                Condition.A => Flags.Carry == FlagState.OFF && Flags.Zero == FlagState.OFF,

                // If the above is false, (X - Y <= 0) => (X <= Y)
                Condition.NA => Flags.Carry == FlagState.ON || Flags.Zero == FlagState.ON,

                // If a carry was set, this would indicate (X - Y < 0) => (X < Y), or just that the carry flag is set.
                Condition.C => Flags.Carry == FlagState.ON,

                // Opposite of above, would imply (X - Y >= 0) => (X >= Y)
                Condition.NC => Flags.Carry == FlagState.OFF,

                // A strange, moderately undocumented condition, where a condition is taken only if RCX = 0 or ECX = 0. 
                // In 32bit, an ADDROVR would denote "if CX = 0", but in x86-64 this was changed to "if ECX == 0", otherwise
                // would be "if RCX == 0". I assume this is because of ancient usage of ECX specifically as a "count register"
                // that holds the iterator during loops. This still is generally the case though, and saves a TEST ECX,ECX
                Condition.RCXZ => (LPrefixBuffer.Contains(PrefixByte.ADDROVR) && ECX.FetchOnce().IsZero()) || RCX.FetchOnce().IsZero(),

                // Zero or equal. (X - Y == 0) => (X==Y)
                Condition.Z => Flags.Zero == FlagState.ON,

                // Not zero/ not equal. (X - Y != 0) => (X != Y)
                Condition.NZ => Flags.Zero == FlagState.OFF,

                // Greater than, for signed numbers.
                // Once understood it will be very easy to interpet the other signed specific conditions.
                // It is hard to think of mathematically, rather should be thought of logically.
                // If X is a signed twos compliments negative number and Y is positive, Y - X will actually
                // loop back round because of integer overflow, a borrow out of the sign bit. However, the
                // result will still be positive and so will the overflow flag be set because the result
                // had the same sign as its subtractee(the value being subtracted from).
                // For example,
                //  9 - -1 = 10 => 9 > -1 because 10 > 9
                // Once understanding how flags work, it is a lot easy to apply mathematically,
                // but first you consider how signed numbers work in twos compliment. See Util.Bitwise
                // Now consider the case that they are not, for complexity lets say two negatives.
                // In decimal,
                //  -1 - -10 = -9 => -1 > -10 because -9 > -10
                // In this case, both the sign flag and overflow flag will be true. This works for
                // both cases because the predicate is Sign == Overflow not Sign & Overflow = true
                // In the folowing, != is used in place of XOR. They both have the same meaning, but
                // saves weird casting to booleans(remember that FlagStates are enum members).
                Condition.G => Flags.Zero == FlagState.OFF && Flags.Sign == Flags.Overflow,
                Condition.GE => Flags.Sign == Flags.Overflow,
                Condition.L => Flags.Sign != Flags.Overflow,
                Condition.LE => Flags.Zero == FlagState.ON || Flags.Sign != Flags.Overflow,

                // If overflow is set. Little mathematical meaning.
                Condition.O => Flags.Overflow == FlagState.ON,
                Condition.NO => Flags.Overflow == FlagState.OFF,

                // If result negative/sign flag set.
                Condition.S => Flags.Sign == FlagState.ON,
                Condition.NS => Flags.Sign == FlagState.OFF,

                // If even parity. Only set by some opcodes, a very rare use case.
                // i.e If the number of bits set in the first byte is even.
                Condition.P => Flags.Parity == FlagState.ON,
                Condition.NP => Flags.Parity == FlagState.OFF,
                _ => true, //Condition.None
            };
        protected void StackPush(byte[] data)
        {
            // A byte to hold the new decremented stack pointer.
            byte[] NewSP;

            // Subtract the size of the data from the stack pointer. It is most efficient to create a byte array like this.
            // It is the responsiblity of the caller to make sure that the length of data complies with assembly standards,
            // i.e not to push DWORDs onto the stack.
            Bitwise.Subtract(StackPointer.FetchOnce(), new byte[] { (byte)data.Length, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);

            // Set the new stack pointer
            StackPointer.Set(NewSP);

            // Set the memory at the new stack pointer. It is important that this is last such that existing stack is not overwritten.
            SetMemory(BitConverter.ToUInt64(StackPointer.FetchOnce(), 0), data);
        }
        protected byte[] StackPop(RegisterCapacity size)
        {
            // Fetch $size bytes from the stack using the address of the stack pointer.
            byte[] StackBytes = ControlUnit.Fetch(BitConverter.ToUInt64(StackPointer.FetchOnce(), 0), (int)size);

            // A byte array to hold the new incremented stack pointer.
            byte[] NewSP;

            // Add the number of bytes that were popped of the stack onto the stack pointer.
            Bitwise.Add(StackPointer.FetchOnce(), new byte[] { (byte)size, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);

            // Set the new stack pointer.
            StackPointer.Set(NewSP);

            // Returned the pop result.
            return StackBytes;
        }
        protected byte[] StackPop() => StackPop(Capacity);
    }

}
