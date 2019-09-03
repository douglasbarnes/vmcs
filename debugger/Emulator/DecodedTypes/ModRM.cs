using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    public enum ModRMSettings
    {
        NONE = 0,
        SWAP = 1,
        EXTENDED = 2,
        HIDEPTR = 4,
    }
    
    public class ModRM : IMyDecoded
    {//everything here is MR encoded by default. doesn't affect anything
        public enum Mod
        {
            Pointer,
            PointerImm8,
            PointerImm32,
            Register
        }
        public XRegCode Source { get; private set; }
        private ulong Destination;
        private long Offset;
        public ulong EffectiveAddress { get => Destination + (ulong)Offset; }
        private SIB? DecodedSIB;
        private DeconstructedModRM Fields;
        private readonly ModRMSettings Settings;
        private struct DeconstructedModRM
        {
            public readonly Mod Mod; // first 2
            public byte Reg; // 
            public byte Mem;
            public DeconstructedModRM(byte mod, byte reg, byte mem)
            {
                (Mod, Reg, Mem) = ((Mod)mod, reg, mem);
            }
        }        
        public ModRM(byte input, ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            string Bits = Bitwise.GetBits(input);
            Fields = new DeconstructedModRM(Convert.ToByte(Bits.Substring(0, 2), 2), Convert.ToByte(Bits.Substring(2, 3), 2), Convert.ToByte(Bits.Substring(5, 3), 2));
            Initialise();
        }
        public ModRM(byte mod, byte reg, byte mem, ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            Fields = new DeconstructedModRM(mod, reg, mem);
            Initialise();
        }
        private void Initialise()
        {
            if((ControlUnit.RexByte | REX.B) == ControlUnit.RexByte)
            {
                Fields.Mem |= 8;
            }
            if ((ControlUnit.RexByte | REX.R) == ControlUnit.RexByte)
            {
                Fields.Reg |= 8;
            }
            Offset = 0;
            DecodedSIB = null;
            Source = (XRegCode)Fields.Reg;
            if (Fields.Mod == Mod.Register)
            {
                Destination = Fields.Mem;
            }
            else
            {
                if (Fields.Mod == Mod.Pointer && Fields.Mem == 5)
                {
                    Offset = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0); //order of these is important!
                    Destination = ControlUnit.InstructionPointer;
                }
                else if (Fields.Mem == 4)
                {
                    DecodedSIB = new SIB(ControlUnit.FetchNext(), Fields.Mod);
                    Offset = DecodedSIB.Value.OffsetValue;
                    Destination = DecodedSIB.Value.ResultNoOffset;
                }
                else
                {
                    Destination = BitConverter.ToUInt64(ControlUnit.FetchRegister((XRegCode)Fields.Mem, RegisterCapacity.QWORD), 0);
                }

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
        public List<string> Disassemble(RegisterCapacity size)
        {
            Disassembly.Pointer DestPtr = new Disassembly.Pointer() { BaseReg = Disassembly.DisassembleRegister((XRegCode)Fields.Mem, size, ControlUnit.RexByte) };
            if (Fields.Mem == 5 && Fields.Mod == 0)
            {
                DestPtr.BaseReg = "RIP";
            }
            else if (Fields.Mem == 4 && Fields.Mod != Mod.Register) // sib conditions
            {
                (DestPtr.Coefficient, DestPtr.BaseReg, DestPtr.AdditionalReg) = DecodedSIB.Value.Disassemble();
            }
            DestPtr.Offset = Offset;
            string Dest = Disassembly.DisassemblePointer(DestPtr);
            if (Fields.Mod != Mod.Register)
            {
                Dest = $"[{Dest}]";
                if((Settings | ModRMSettings.HIDEPTR) != Settings)
                {
                    Dest = $"{Disassembly.DisassembleSize(size)} PTR {Dest}";
                }
            }
            if ((Settings | ModRMSettings.EXTENDED) == Settings)
            {
                return new List<string> { Dest };
            } else
            {
                string Source = Disassembly.DisassembleRegister((XRegCode)Fields.Reg, size, ControlUnit.RexByte);
                if ((Settings | ModRMSettings.SWAP) == Settings)
                {
                    return new List<string> { Source, Dest };
                }
                else
                {
                    return new List<string> { Dest, Source };
                }
            }
        }
        public List<byte[]> Fetch(RegisterCapacity length)
        {
            List<byte[]> Output = new List<byte[]>();            
            byte[] DestBytes;
            if (Fields.Mod == Mod.Register)
            {
                DestBytes = ControlUnit.FetchRegister((XRegCode)Destination, length);
            }
            else
            {
                DestBytes = ControlUnit.Fetch(Destination + (ulong)Offset, (int)length);
            }
            Output.Add(DestBytes);
            if(Settings != ModRMSettings.EXTENDED)
            {
                Output.Add(ControlUnit.FetchRegister(Source, length));
            }         
            if(Settings == ModRMSettings.SWAP)
            {
                Output.Reverse();
            }
            return Output;
        }
        public void Set(byte[] data) => SetInternal(data, false);
        public void SetSource(byte[] data) => SetInternal(data, true);
        private void SetInternal(byte[] data, bool forceSwap)
        {
            forceSwap ^= Settings == ModRMSettings.SWAP;
            if (Fields.Mod == Mod.Register)
            {
                if (forceSwap)
                {
                    ControlUnit.SetRegister(Source, data);
                }
                else
                {
                    ControlUnit.SetRegister((XRegCode)Destination, data);
                }

            }
            else
            {
                if (forceSwap)
                {
                    ControlUnit.SetRegister(Source, data);
                }
                else
                {
                    ControlUnit.SetMemory(Destination + (ulong)Offset, data);
                }

            }
        }
    }
    
}
