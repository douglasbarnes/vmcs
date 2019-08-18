using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public interface IMyDecoded
    {
        public string[] Disassemble(RegisterCapacity size);
        public List<byte[]> Fetch(RegisterCapacity length);
        public void Set(byte[] data);
    }
}
