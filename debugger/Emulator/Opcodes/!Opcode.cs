using System;
using System.Collections.Generic;
using debugger.Emulator.DecodedTypes;
using debugger.Util;
using static debugger.Emulator.ControlUnit;
namespace debugger.Emulator.Opcodes
{ 
    public enum Condition // for conditional opcodes
    {
        None,
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
        None = 0,
        Is8Bit = 1,
        IsSigned = 2,
        IsSignExtendedByte = 4,
        AllowImm64 = 8,
        ExtraImmediate = 32,
    }
    public abstract class Opcode
    {
        protected RegisterCapacity Capacity;
        protected readonly OpcodeSettings Settings;
        private readonly string Mnemonic;              
        private byte[] ImmediateBuffer = null; //registers and memory can change so only do this for immediate.
        private readonly IMyDecoded InputMethod;
        public Opcode(string opcodeMnemonic, IMyDecoded input, OpcodeSettings settings)
        {
            Mnemonic = opcodeMnemonic;
            InputMethod = input;
            Settings = settings;
            SetRegCap();
        }
        public Opcode(string opcodeMnemonic, IMyDecoded input, RegisterCapacity opcodeCapacity,  OpcodeSettings settings=OpcodeSettings.None)
        {
            Mnemonic = opcodeMnemonic;
            InputMethod = input;
            Settings = settings;
            Capacity = opcodeCapacity;
        }
        protected List<byte[]> Fetch()
        {
            List<byte[]> Output = InputMethod.Fetch(Capacity);
            if ((Settings | OpcodeSettings.ExtraImmediate) == Settings)
            {
                if (ImmediateBuffer == null) // prevents fetching into adjacent instructions, still dont think its a good idea to call fetch more than once
                {
                    if((Settings | OpcodeSettings.IsSignExtendedByte) == Settings)
                    {
                        ImmediateBuffer = Bitwise.SignExtend(FetchNext(1), (byte)Capacity);
                    }
                    else if(Capacity == RegisterCapacity.QWORD)
                    {
                        if ((Settings | OpcodeSettings.AllowImm64) == Settings)
                        {
                            ImmediateBuffer = FetchNext(8);
                        }
                        else
                        {
                            ImmediateBuffer = Bitwise.SignExtend(FetchNext(4), 8);
                        }
                    }
                     else
                    {                                                
                        ImmediateBuffer = FetchNext((byte)Capacity);
                    }
                    
                } 
                Output.Add(ImmediateBuffer);
            }
            return Output;
        }

        protected void Set(byte[] data) => InputMethod.Set(data);
        public abstract void Execute();
        public virtual List<string> Disassemble()
        {
            List<string> Output = new List<string> { Mnemonic };
            Output.AddRange(InputMethod.Disassemble(Capacity));
            if(ImmediateBuffer != null)
            {
                Array.Reverse(ImmediateBuffer); //little to big endian
                Output.Add($"0x{Core.Atoi(ImmediateBuffer)}");
            }
            return Output;
        }
        protected void SetRegCap(RegisterCapacity defaultCapacity = RegisterCapacity.DWORD)
        {
            if ((Settings | OpcodeSettings.Is8Bit) == Settings) Capacity = RegisterCapacity.BYTE;
            else if (PrefixBuffer.Contains(PrefixByte.REXW)) Capacity = RegisterCapacity.QWORD;
            else if (PrefixBuffer.Contains(PrefixByte.SIZEOVR)) Capacity = RegisterCapacity.WORD;
            else Capacity = defaultCapacity;
        }
        protected bool TestCondition(Condition condition)
        {
            switch(condition)
            {
                case Condition.A: // used for signed
                    return Flags.Carry == FlagState.Off && Flags.Zero == FlagState.Off;
                case Condition.NA: // used for signed
                    return Flags.Carry == FlagState.On || Flags.Zero == FlagState.On;
                case Condition.C: // used for signed
                    return Flags.Carry == FlagState.On;
                case Condition.NC: // used for signed
                    return Flags.Carry == FlagState.Off;
                case Condition.RCXZ: //exists because of loops using C reg as the iterator, have to hard code it like this, risky using Capacity here
                    return (PrefixBuffer.Contains(PrefixByte.ADDR32) && FetchRegister(ByteCode.A, RegisterCapacity.DWORD).IsZero()) || FetchRegister(ByteCode.A, RegisterCapacity.QWORD).IsZero();
                case Condition.Z:
                    return Flags.Zero == FlagState.On;
                case Condition.NZ:
                    return Flags.Zero == FlagState.Off;
                case Condition.G: // used for unsigned
                    return Flags.Zero == FlagState.Off && Flags.Sign == Flags.Overflow;
                case Condition.GE: // used for unsigned
                    return Flags.Sign == Flags.Overflow;
                case Condition.L: // used for unsigned
                    return Flags.Sign != Flags.Overflow;
                case Condition.LE: // used for unsigned
                    return Flags.Zero == FlagState.On || Flags.Sign != Flags.Overflow;
                case Condition.O:
                    return Flags.Overflow == FlagState.On;
                case Condition.NO:
                    return Flags.Overflow == FlagState.Off;
                case Condition.S:
                    return Flags.Sign == FlagState.On;
                case Condition.NS:
                    return Flags.Sign == FlagState.Off;
                case Condition.P:
                    return Flags.Parity == FlagState.On;
                case Condition.NP:
                    return Flags.Parity == FlagState.Off;
                default:
                    return true; //Condition.None
            }
        }
    }

}
