using System;
using System.Collections.Generic;

namespace debugger.Emulator.DecodedTypes
{
    public class Immediate : IMyDecoded
    {
        private byte[] ImmediateBuffer;
        public readonly bool RIPRelative;
        public RegisterCapacity Size;
        public Immediate(RegisterCapacity size, bool ripRel=false)
        {
            RIPRelative = ripRel;
            SetBuffer(ControlUnit.FetchNext((byte)size));
        }
        public Immediate(byte[] input, bool ripRel = false)
        {
            RIPRelative = ripRel;
            SetBuffer(input);

        }

        public Immediate(bool ripRel = false)
        {
            RIPRelative = ripRel;
            ImmediateBuffer = null;
        }
        private void SetBuffer(byte[] input)
        {
            if(RIPRelative)
            {
                Util.Bitwise.Add(input, BitConverter.GetBytes(ControlUnit.InstructionPointer), input.Length, out ImmediateBuffer);
            }
            else
            {
                ImmediateBuffer = input;
            }
            Size = (RegisterCapacity)input.Length; // If theres an error here, something is wrong
        }
        public string[] Disassemble(RegisterCapacity size) => new string[] { Util.Core.Atoi(ImmediateBuffer) };
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
