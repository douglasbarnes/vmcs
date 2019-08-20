using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.DecodedTypes
{
    [Flags]
    public enum ImmediateSettings
    {
        NONE=0,
        RELATIVE=1,
        SXTBYTE=4,
    }
    public class Immediate : IMyDecoded
    {
        private byte[] ImmediateBuffer;
        public readonly ImmediateSettings Settings;
        public RegisterCapacity Size { get; private set; }
        public Immediate(RegisterCapacity size, ImmediateSettings settings = ImmediateSettings.NONE)
        {
            Settings = settings;            
            SetBuffer(ControlUnit.FetchNext((byte)size));
        }
        public Immediate(byte[] input, ImmediateSettings settings = ImmediateSettings.NONE)
        {
            Settings = settings;
            SetBuffer(input);
        }

        public Immediate(ImmediateSettings settings = ImmediateSettings.NONE)
        {
            Settings = settings;
            ImmediateBuffer = null;
        }
        private void SetBuffer(byte[] input)
        {
            if ((Settings | ImmediateSettings.SXTBYTE) == Settings) 
            {
                Size = RegisterCapacity.GP_BYTE;
                ImmediateBuffer = Bitwise.SignExtend(new byte[] { input[0] }, 8);
            }
            else if ((Settings | ImmediateSettings.RELATIVE) == Settings)
            {
                Util.Bitwise.Add(input, BitConverter.GetBytes(ControlUnit.InstructionPointer), input.Length, out ImmediateBuffer);
            }
            else
            {
                ImmediateBuffer = input;
            }
            Size = (RegisterCapacity)input.Length; // If theres an error here, something is wrong
        }
        public List<string> Disassemble(RegisterCapacity size) => new List<string> { $"0x{Core.Atoi(ImmediateBuffer)}" };
        public List<byte[]> Fetch(RegisterCapacity length)
        {
            if(ImmediateBuffer == null)
            {
                SetBuffer(ControlUnit.FetchNext((byte)length));
            }
            return new List<byte[]>() { ImmediateBuffer };
        }
        public void Set(byte[] data) => new Exception("Immediate.cs Attempt to set value of immediate");        
    }
}
