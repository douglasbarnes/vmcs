using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public interface IMyDecoded
    {
        public RegisterCapacity Size { get; }
        public List<string> Disassemble();
        public List<byte[]> Fetch();
        public void Initialise(RegisterCapacity size);
        public void Set(byte[] data);
    }
    public interface IMyMultiDecoded : IMyDecoded
    {
        public void SetSource(byte[] data);
    }
}
