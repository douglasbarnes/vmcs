using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    /*
    public struct ModRM
    {
        private ulong Destination;               
        private long Offset;//for disas only
        public ulong DestPtr => Destination + (ulong)Offset;
        public bool IsPtr => Bits.Mod != 3;
        public ByteCode Source;
        private SIB? DecodedSIB;
        private readonly DeconstructedModRM Bits;
        
        public ModRM(byte input, bool swap=false)
        {
            string Bits = Bitwise.GetBits(input);
            this = new ModRM(Convert.ToByte(Bits.Substring(0, 2), 2), Convert.ToByte(Bits.Substring(2, 3), 2), Convert.ToByte(Bits.Substring(5, 3), 2), swap );
        }
        public ModRM(byte mod, byte reg, byte mem, bool swap=false)
        {
            if(swap) (reg, mem) = (mem, reg);
            Offset = 0;
            DecodedSIB = null;
            Bits = new DeconstructedModRM(mod,reg,mem);
            Source = (ByteCode)reg;
            if(mod == 3)
            {
                Destination = mem;
            } else
            {             
                if(mod == 0 && mem == 5)
                {
                    Offset = BitConverter.ToUInt32(ControlUnit.FetchNext(4), 0);
                    Destination = ControlUnit.InstructionPointer;                    
                }
                else if(mem == 4)
                {
                    DecodedSIB = new SIB(ControlUnit.FetchNext(), mod);
                    Offset = DecodedSIB.Value.OffsetValue;
                    Destination = DecodedSIB.Value.ResultNoOffset;
                }
                else
                {                    
                    Destination = BitConverter.ToUInt64(ControlUnit.FetchRegister((ByteCode)mem, RegisterCapacity.QWORD), 0);                               
                }

                if(mod == 1)
                {
                    Offset += (sbyte)ControlUnit.FetchNext();
                }
                else if(mod == 2)
                {
                    Offset += BitConverter.ToInt32(ControlUnit.FetchNext(4), 0);
                }
            }
        }
        public string[] Disassemble(RegisterCapacity regCap)
        {
            Pointer DestPtr = new Pointer() { BaseReg = RegisterMnemonics[Bits.Mem][regCap] };
            if (Bits.Mem == 5 && Bits.Mod == 0)
            {
                DestPtr.BaseReg = "RIP";
            }
            else if (Bits.Mem == 4 && Bits.Mod != 3) // sib conditions
            {
                (DestPtr.Coefficient, DestPtr.BaseReg, DestPtr.AdditionalReg) = DecodedSIB.Value.Disassemble();
            }
            DestPtr.Offset = Offset;
            string Source = RegisterMnemonics[Bits.Reg][regCap];
            string Dest = DisassemblePointer(DestPtr);
            if (Bits.Mod != 3)
            {
                Dest = $"[{Dest}]";
            }
            return new string[] { Dest, Source };
        }
    }
    */
    public class ModRM : IMyDecoded
    {
        public bool ExtendedOpcode = false;
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
        public ModRM(byte input, bool swap = false)
        {
            string Bits = Bitwise.GetBits(input);
            Initialise(Convert.ToByte(Bits.Substring(0, 2), 2), Convert.ToByte(Bits.Substring(2, 3), 2), Convert.ToByte(Bits.Substring(5, 3), 2), swap);
        }
        public ModRM(byte mod, byte reg, byte mem, bool swap = false)
        {
            Initialise(mod, reg, mem, swap);
        }
        private void Initialise(byte mod, byte reg, byte mem, bool swap)
        {
            if (swap) (reg, mem) = (mem, reg);
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
        public string[] Disassemble(RegisterCapacity size)
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
            string Source = Disassembly.RegisterMnemonics[Bits.Reg][size];
            string Dest = Disassembly.DisassemblePointer(DestPtr);
            if (Bits.Mod != Mod.Register)
            {
                Dest = $"[{Dest}]";
            }
            return new string[] { Dest, Source };
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
            if(!ExtendedOpcode)
            {
                Output.Add(ControlUnit.FetchRegister(Source, length));
            }            
            return Output;
        }
        public void Set(byte[] data)
        {
            if (Bits.Mod == Mod.Register) {
                ControlUnit.SetRegister((ByteCode)Destination, data);
            } else
            {
                ControlUnit.SetMemory(Destination+(ulong)Offset, data);
            }
        }
    }
}
