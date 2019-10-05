using System;
using System.Collections.Generic;
using debugger.Emulator.DecodedTypes;
using debugger.Util;
using static debugger.Emulator.ControlUnit;
namespace debugger.Emulator.Opcodes
{ 
    public enum Condition // for conditional opcodes
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
        NONE = 0,
        BYTEMODE = 1,
        SIGNED = 2,
    }
    public abstract class Opcode : IMyOpcode
    {
        private RegisterCapacity _capacity;
        protected RegisterCapacity Capacity { get => _capacity; set { Input.Initialise(value); _capacity = value; } }
        public readonly OpcodeSettings Settings;
        private readonly string Mnemonic;
        private readonly IMyDecoded Input;
        public Opcode(string opcodeMnemonic, IMyDecoded input,  OpcodeSettings settings=OpcodeSettings.NONE, RegisterCapacity opcodeCapacity = RegisterCapacity.NONE)
        {
            Mnemonic = opcodeMnemonic;
            Input = input;
            Settings = settings;
            Capacity = (opcodeCapacity == RegisterCapacity.NONE) ? SetRegCap() : opcodeCapacity;
        }
        protected List<byte[]> Fetch() => Input.Fetch();
        protected void Set(byte[] data) => Input.Set(data);
        protected void Set(byte[] data, int operandIndex) => ((DecodedCompound)Input).Set(data, operandIndex);
        public abstract void Execute();
        public virtual List<string> Disassemble()
        {
            List<string> Output = new List<string>() { Mnemonic };
            Output.AddRange(Input.Disassemble());
            return Output;
        }
        protected RegisterCapacity SetRegCap()
        {
            if ((Settings | OpcodeSettings.BYTEMODE) == Settings)
            {
                return RegisterCapacity.BYTE;
            }
            else if ((RexByte | REX.W) == RexByte)
            {
                return RegisterCapacity.QWORD;
            }
            else if (LPrefixBuffer.Contains(PrefixByte.SIZEOVR))
            {
                return RegisterCapacity.WORD;
            }
            else
            {
                return RegisterCapacity.DWORD;
            }
        }
        private static readonly RegisterHandle EAX = new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.DWORD);
        private static readonly RegisterHandle RAX = new RegisterHandle(XRegCode.A, RegisterTable.GP, RegisterCapacity.QWORD);
        private static readonly RegisterHandle StackPointer = new RegisterHandle(XRegCode.SP, RegisterTable.GP, RegisterCapacity.QWORD);
        protected bool TestCondition(Condition condition) 
            => condition switch
            {
                Condition.A => Flags.Carry == FlagState.OFF && Flags.Zero == FlagState.OFF,
                Condition.NA => Flags.Carry == FlagState.ON || Flags.Zero == FlagState.ON,
                Condition.C => Flags.Carry == FlagState.ON,
                Condition.NC => Flags.Carry == FlagState.OFF,
                Condition.RCXZ => (LPrefixBuffer.Contains(PrefixByte.ADDROVR) && EAX.Fetch()[0].IsZero()) || RAX.Fetch()[0].IsZero(),
                Condition.Z => Flags.Zero == FlagState.ON,
                Condition.NZ => Flags.Zero == FlagState.OFF,
                Condition.G => Flags.Zero == FlagState.OFF && Flags.Sign == Flags.Overflow,
                Condition.GE => Flags.Sign == Flags.Overflow,
                Condition.L => Flags.Sign != Flags.Overflow,
                Condition.LE => Flags.Zero == FlagState.ON || Flags.Sign != Flags.Overflow,
                Condition.O => Flags.Overflow == FlagState.ON,
                Condition.NO => Flags.Overflow == FlagState.OFF,
                Condition.S => Flags.Sign == FlagState.ON,
                Condition.NS => Flags.Sign == FlagState.OFF,
                Condition.P => Flags.Parity == FlagState.ON,
                Condition.NP => Flags.Parity == FlagState.OFF,
                _ => true, //Condition.None
            };              
        protected void StackPush(byte[] data)
        {
            byte[] NewSP;
            Bitwise.Subtract(StackPointer.FetchOnce(), new byte[] { (byte)data.Length, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            StackPointer.Set(NewSP);
            SetMemory(BitConverter.ToUInt64(StackPointer.FetchOnce(), 0), data);
        }
        protected byte[] StackPop(RegisterCapacity size)
        {
            byte[] Output = ControlUnit.Fetch(BitConverter.ToUInt64(StackPointer.FetchOnce(), 0), (int)size);
            byte[] NewSP;
            Bitwise.Add(StackPointer.FetchOnce(), new byte[] { (byte)size, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            StackPointer.Set(NewSP);
            return Output;
        }
        protected byte[] StackPop() => StackPop(Capacity);
    }

}
