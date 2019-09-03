using System;
using System.Collections.Generic;

namespace debugger.Emulator.DecodedTypes
{
    public class Constant : IMyDecoded
    {
        private byte[] Buffer;
        public RegisterCapacity Size { get; private set; }

        public Constant(ulong value)
        {
            Buffer = BitConverter.GetBytes(value);
        }
        public List<string> Disassemble() => new List<string>() { $"0x{Util.Core.Atoi(Buffer)}" };

        public List<byte[]> Fetch() => new List<byte[]>() { Buffer };

        public void Initialise(RegisterCapacity size) { }

        public void Set(byte[] data) => throw new Exception("Attempt to set value of constant");
    }
}
