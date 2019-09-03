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
    {//everything here is MR encoded by default. doesn't affect anything
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
        private RegisterCapacity _size;
        public RegisterCapacity Size { get => _size; private set; }
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
        public void Initialise(RegisterCapacity size)
        {
            _size = size;
            Source = new ControlUnit.RegisterHandle((XRegCode)Fields.Reg, RegisterTable.GP, Size);
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
            string Bits = Bitwise.GetBits(input);
            Fields = new DeconstructedModRM(Convert.ToByte(Bits.Substring(0, 2), 2), Convert.ToByte(Bits.Substring(2, 3), 2), Convert.ToByte(Bits.Substring(5, 3), 2));
            if ((ControlUnit.RexByte | REX.B) == ControlUnit.RexByte)
            {
                Fields.Mem |= 8;
            }
            if ((ControlUnit.RexByte | REX.R) == ControlUnit.RexByte)
            {
                Fields.Reg |= 8;
            }
            Offset = 0;
            DecodedSIB = null;
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
                    Destination = BitConverter.ToUInt64(new ControlUnit.RegisterHandle((XRegCode)Fields.Mem, RegisterTable.GP, RegisterCapacity.QWORD).Value, 0);
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
        public List<string> Disassemble()
        {
            Disassembly.Pointer DestPtr = new Disassembly.Pointer() { BaseReg = Disassembly.DisassembleRegister(new ControlUnit.RegisterHandle((XRegCode)Fields.Mem, RegisterTable.GP, Size)) };
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
                if ((Settings | ModRMSettings.HIDEPTR) != Settings)
                {
                    Dest = $"{Disassembly.DisassembleSize(Size)} PTR {Dest}";
                }
            }
            if ((Settings | ModRMSettings.EXTENDED) == Settings)
            {
                return new List<string> { Dest };
            }
            else
            {
                string Source = Disassembly.DisassembleRegister(new ControlUnit.RegisterHandle((XRegCode)Fields.Reg, RegisterTable.GP, Size));
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
        public List<byte[]> Fetch()
        {
            List<byte[]> Output = new List<byte[]>();
            byte[] DestBytes;
            if (Fields.Mod == Mod.Register)
            {
                DestBytes = new ControlUnit.RegisterHandle((XRegCode)Destination, RegisterTable.GP, Size).Fetch()[0];
            }
            else
            {
                DestBytes = ControlUnit.Fetch(Destination + (ulong)Offset, (int)Size);
            }
            Output.Add(DestBytes);
            if (Settings != ModRMSettings.EXTENDED)
            {
                Output.Add(Source.Fetch()[0]);
            }
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
            forceSwap ^= Settings == ModRMSettings.SWAP;
            if (Fields.Mod == Mod.Register)
            {
                if (forceSwap)
                {
                    Source.Value = data;
                }
                else
                {
                    ControlUnit.RegisterHandle DestHandle = new ControlUnit.RegisterHandle((XRegCode)Destination, RegisterTable.GP, Size)
                    {
                        Value = data
                    };
                }
            }
            else
            {
                if (forceSwap)
                {
                    Source.Value = data;
                }
                else
                {
                    ControlUnit.SetMemory(Destination + (ulong)Offset, data);
                }
            }
        }
    }
}
