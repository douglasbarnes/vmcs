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
        protected RegisterCapacity Capacity { get; set; }
        public readonly OpcodeSettings Settings;
        private readonly string Mnemonic;
        private readonly IMyDecoded Input;
        public Opcode(string opcodeMnemonic, IMyDecoded input,  OpcodeSettings settings=OpcodeSettings.NONE, RegisterCapacity opcodeCapacity = RegisterCapacity.NONE)
        {
            Mnemonic = opcodeMnemonic;
            Input = input;
            Settings = settings;
            Capacity = (opcodeCapacity == RegisterCapacity.NONE) ? SetRegCap() : opcodeCapacity;
            Initialise();
        }
        private void Initialise()
        {
            for (int i = 0; i < Input.Length; i++)
            {
                Input[i].Initialise(Capacity);
            }         
        }
        protected List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>();
            for (int i = 0; i < Input.Length; i++)
            {
                Output.AddRange(Input[i].Fetch());
            }
            return Output;
        }
        protected void Set(byte[] data) => Input.Set(Bitwise.ZeroExtend(data, (byte)Capacity));
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
        private static readonly RegisterHandle StackPointer = new RegisterHandle(XRegCode.SP, RegisterTable.GP, RegisterCapacity.QWORD);
        protected void StackPush(byte[] data)
        {
            byte[] NewSP;
            Bitwise.Subtract(StackPointer.Value, new byte[] { (byte)data.Length, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            StackPointer.Value = NewSP;
            SetMemory(BitConverter.ToUInt64(StackPointer.Value, 0), data);
        }
        protected byte[] StackPop(RegisterCapacity size)
        {
            byte[] Output = ControlUnit.Fetch(BitConverter.ToUInt64(StackPointer.Value, 0), (int)size);
            byte[] NewSP;
            Bitwise.Add(StackPointer.Value, new byte[] { (byte)size, 0, 0, 0, 0, 0, 0, 0 }, 8, out NewSP);
            StackPointer.Value = NewSP;
            return Output;
        }
        protected byte[] StackPop() => StackPop(Capacity);
    }

}
