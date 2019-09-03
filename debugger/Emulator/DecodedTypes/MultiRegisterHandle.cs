using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public class MultiRegisterHandle : IMyMultiDecoded
    {
        public RegisterCapacity Size { get; private set; }
        private ControlUnit.RegisterHandle Destination;
        private ControlUnit.RegisterHandle Source;
        private XRegCode DestCode;
        private XRegCode SrcCode;
        public MultiRegisterHandle(XRegCode destination, XRegCode source)
        {
            DestCode = destination;
            SrcCode = source;
        }
        public void Initialise(RegisterCapacity size)
        {
            Size = size;
            Destination = new ControlUnit.RegisterHandle(DestCode, RegisterTable.GP, size);
            Source = new ControlUnit.RegisterHandle(SrcCode, RegisterTable.GP, size);
        }
        public List<string> Disassemble()
            => new List<string>() { Destination.Disassemble()[0], Source.Disassemble()[0] };

        public List<byte[]> Fetch()
            => new List<byte[]> { Destination.Fetch()[0], Source.Fetch()[0] };
        public void Set(byte[] data) => Destination.Set(data);
        public void SetSource(byte[] data) => Source.Set(data);
    }
}
