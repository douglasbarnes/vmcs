using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.DecodedTypes
{
    class ImplicitRegister : IMyDecoded
    {
        private readonly XRegCode Destination;
        public ImplicitRegister(XRegCode destination)
        {
            Destination = destination;
        }
        public List<string> Disassemble(RegisterCapacity size) => new List<string> { Disassembly.DisassembleRegister(Destination, size, ControlUnit.RexByte) };

        public List<byte[]> Fetch(RegisterCapacity length) => new List<byte[]>() { ControlUnit.FetchRegister(Destination, length) };
        public void Set(byte[] data) => ControlUnit.SetRegister(Destination, data);
    }
}
