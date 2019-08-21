using System;
using System.Collections.Generic;
using debugger.Emulator.DecodedTypes;
using debugger.Util;
using static debugger.Emulator.ControlUnit;
namespace debugger.Emulator
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
        None = 0,
        BYTEMODE = 1,
        SIGNED = 2,
        SXTBYTE = 4,
        ALLOWIMM64 = 8,
        IMMEDIATE = 32,
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
            if ((Settings | OpcodeSettings.IMMEDIATE) == Settings)
            {
                if (ImmediateBuffer == null) // prevents fetching into adjacent instructions, still dont think its a good idea to call fetch more than once
                {
                    if((Settings | OpcodeSettings.SXTBYTE) == Settings)
                    {
                        ImmediateBuffer = Bitwise.SignExtend(FetchNext(1), (byte)Capacity);
                    }
                    else if(Capacity == RegisterCapacity.GP_QWORD)
                    {
                        if ((Settings | OpcodeSettings.ALLOWIMM64) == Settings)
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
                Output.Add($"0x{Core.Atoi(ImmediateBuffer)}"); //atoi also converts little to big endian
            }
            return Output;
        }
        protected void SetRegCap(RegisterCapacity defaultCapacity = RegisterCapacity.GP_DWORD)
        {
            if ((Settings | OpcodeSettings.BYTEMODE) == Settings) Capacity = RegisterCapacity.GP_BYTE;
            else if ((RexByte | REX.W) == RexByte) Capacity = RegisterCapacity.GP_QWORD;
            else if (PrefixBuffer.Contains(PrefixByte.SIZEOVR)) Capacity = RegisterCapacity.GP_WORD;
            else Capacity = defaultCapacity;
        }
        protected bool TestCondition(Condition condition)
        {
            switch(condition)
            {
                case Condition.A: // used for signed
                    return Flags.Carry == FlagState.OFF && Flags.Zero == FlagState.OFF;
                case Condition.NA: // used for signed
                    return Flags.Carry == FlagState.ON || Flags.Zero == FlagState.ON;
                case Condition.C: // used for signed
                    return Flags.Carry == FlagState.ON;
                case Condition.NC: // used for signed
                    return Flags.Carry == FlagState.OFF;
                case Condition.RCXZ: //exists because of loops using C reg as the iterator, have to hard code it like this, risky using Capacity here
                    return (PrefixBuffer.Contains(PrefixByte.ADDR32) && FetchRegister(XRegCode.A, RegisterCapacity.GP_DWORD).IsZero()) || FetchRegister(XRegCode.A, RegisterCapacity.GP_QWORD).IsZero();
                case Condition.Z:
                    return Flags.Zero == FlagState.ON;
                case Condition.NZ:
                    return Flags.Zero == FlagState.OFF;
                case Condition.G: // used for unsigned
                    return Flags.Zero == FlagState.OFF && Flags.Sign == Flags.Overflow;
                case Condition.GE: // used for unsigned
                    return Flags.Sign == Flags.Overflow;
                case Condition.L: // used for unsigned
                    return Flags.Sign != Flags.Overflow;
                case Condition.LE: // used for unsigned
                    return Flags.Zero == FlagState.ON || Flags.Sign != Flags.Overflow;
                case Condition.O:
                    return Flags.Overflow == FlagState.ON;
                case Condition.NO:
                    return Flags.Overflow == FlagState.OFF;
                case Condition.S:
                    return Flags.Sign == FlagState.ON;
                case Condition.NS:
                    return Flags.Sign == FlagState.OFF;
                case Condition.P:
                    return Flags.Parity == FlagState.ON;
                case Condition.NP:
                    return Flags.Parity == FlagState.OFF;
                default:
                    return true; //Condition.None
            }
        }
    }

}
