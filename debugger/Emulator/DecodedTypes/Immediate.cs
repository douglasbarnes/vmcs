using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    [Flags]
    public enum ImmediateSettings
    {
        NONE=0,
        SXTBYTE=1,
        ALLOWIMM64=2,
        RELATIVE=4,
    }
    public class Immediate : IMyDecoded
    {
        public RegisterCapacity Size { get; private set; }
        private readonly ImmediateSettings Settings;
        private byte[] Buffer = null;
        public Immediate(ImmediateSettings settings = ImmediateSettings.NONE)
        {
            Settings = settings;
        }
        public void Initialise(RegisterCapacity size)
        {
            if(Buffer == null)
            {
                Size = size;
                byte MaxVal = 0b0100;
                if ((Settings | ImmediateSettings.SXTBYTE) == Settings)
                {
                    MaxVal = 0b0001;
                }
                else if ((Settings | ImmediateSettings.ALLOWIMM64) == Settings)
                {
                    MaxVal = 0b1000;
                }
                Buffer = ControlUnit.FetchNext((int)size < MaxVal ? (int)size : MaxVal);
                if ((Settings | ImmediateSettings.RELATIVE) == Settings)
                {
                    Bitwise.Add(Buffer, BitConverter.GetBytes(ControlUnit.InstructionPointer), 8, out Buffer);
                }
            }            
        }
        public List<byte[]> Fetch() => new List<byte[]>() { Bitwise.SignExtend(Buffer, (byte)Size) };
        public void Set(byte[] data) => throw new Exception("Attempt to set value of immediate");
        public List<string> Disassemble() => new List<string>() { $"0x{Core.Atoi(Buffer)}" };
    }
}
