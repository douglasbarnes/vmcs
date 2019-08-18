using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    public enum ModRMSettings
    {
        NONE = 0,
        SWAP = 1,
        EXTENDED
    }
    
    public class ModRM : IMyDecoded
    {//everything here is MR encoded by default. doesn't affect anything
        private enum Mod
        {
            Pointer,
            PointerImm8,
            PointerImm32,
            Register
        }
        public ByteCode Source { get; private set; }
        private ulong Destination;
        private long Offset;
        private SIB? DecodedSIB;
        private DeconstructedModRM Bits;
        private readonly ModRMSettings Settings;
        private readonly struct DeconstructedModRM
        {
            public readonly Mod Mod; // first 2
            public readonly byte Reg; // 
            public readonly byte Mem;
            public DeconstructedModRM(byte mod, byte reg, byte mem)
            {
                (Mod, Reg, Mem) = ((Mod)mod, reg, mem);
            }
        }        
        public ModRM(byte input, ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            string Bits = Bitwise.GetBits(input);
            Initialise(Convert.ToByte(Bits.Substring(0, 2), 2), Convert.ToByte(Bits.Substring(2, 3), 2), Convert.ToByte(Bits.Substring(5, 3), 2));
        }
        public ModRM(byte mod, byte reg, byte mem, ModRMSettings settings = ModRMSettings.NONE)
        {
            Settings = settings;
            Initialise(mod, reg, mem);
        }
        private void Initialise(byte mod, byte reg, byte mem)
        {
            Offset = 0;
            DecodedSIB = null;
            Bits = new DeconstructedModRM(mod, reg, mem);
            Source = (ByteCode)reg;
            if (Bits.Mod == Mod.Register)
            {
                Destination = mem;
            }
            else
            {
                if (Bits.Mod == Mod.Pointer && mem == 5)
                {
                    Offset = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);
                    Destination = ControlUnit.InstructionPointer;
                }
                else if (mem == 4)
                {
                    DecodedSIB = new SIB(ControlUnit.FetchNext(), mod);
                    Offset = DecodedSIB.Value.OffsetValue;
                    Destination = DecodedSIB.Value.ResultNoOffset;
                }
                else
                {
                    Destination = BitConverter.ToUInt64(ControlUnit.FetchRegister((ByteCode)mem, RegisterCapacity.QWORD), 0);
                }

                if (Bits.Mod == Mod.PointerImm8)
                {
                    Offset += (sbyte)ControlUnit.FetchNext();
                }
                else if (Bits.Mod == Mod.PointerImm32)
                {
                    Offset += BitConverter.ToInt32(ControlUnit.FetchNext(4), 0);
                }
            }
        }
        public List<string> Disassemble(RegisterCapacity size)
        {
            Disassembly.Pointer DestPtr = new Disassembly.Pointer() { BaseReg = Disassembly.RegisterMnemonics[Bits.Mem][size] };
            if (Bits.Mem == 5 && Bits.Mod == 0)
            {
                DestPtr.BaseReg = "RIP";
            }
            else if (Bits.Mem == 4 && Bits.Mod != Mod.Register) // sib conditions
            {
                (DestPtr.Coefficient, DestPtr.BaseReg, DestPtr.AdditionalReg) = DecodedSIB.Value.Disassemble();
            }
            DestPtr.Offset = Offset;
            string Dest = Disassembly.DisassemblePointer(DestPtr);
            if (Bits.Mod != Mod.Register)
            {
                Dest = $"[{Dest}]";
            }
            if (Settings == ModRMSettings.EXTENDED)
            {
                return new List<string> { Dest };
            } else
            {
                string Source = Disassembly.RegisterMnemonics[Bits.Reg][size];
                if (Settings == ModRMSettings.SWAP)
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
            if (Bits.Mod == Mod.Register)
            {
                DestBytes = ControlUnit.FetchRegister((ByteCode)Destination, length);
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
        public void Set(byte[] data)
        {
            if (Bits.Mod == Mod.Register) {
                if(Settings == ModRMSettings.SWAP)
                {
                    ControlUnit.SetRegister(Source, data);                    
                } else
                {
                    ControlUnit.SetRegister((ByteCode)Destination, data);
                }
                
            } else
            {
                if (Settings == ModRMSettings.SWAP)
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
