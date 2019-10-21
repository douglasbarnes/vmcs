// A constant represents an input to an opcode that is always a specific value.
// Current usages of this exist in rotate and shift opcodes, where opcodes for single rotates/shifts exist.
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
            // Initialise from a ulong and store it as bytes in the buffer
            Buffer = BitConverter.GetBytes(value);
        }

        public List<string> Disassemble()
        {
            // Return a hex representation of the buffer
            return new List<string>() { $"0x{Util.Core.Itoa(Buffer)}" };
        }

        public List<byte[]> Fetch()
        {
            // Return a List<byte[] containing the buffer. This is necessary as it is part of the interface.
            return new List<byte[]>() { Buffer };
        }

        public void Initialise(RegisterCapacity size)
        {
            // Constants always stay the same size.
        }

        public void Set(byte[] data)
        {
            // The most likely cause of this would be that the constant class had been put as the first element of a DecodedCompound. Or bad coding.
            throw new Exception("Attempt to set value of constant");
        }
    }
}
