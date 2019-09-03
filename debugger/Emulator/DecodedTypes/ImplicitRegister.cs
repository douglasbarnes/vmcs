using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.DecodedTypes
{
    public class ImplicitRegister : IMyDecoded
    {
        private readonly ControlUnit.RegisterHandle Destination;
        private readonly ControlUnit.RegisterHandle Source;
        private readonly bool HasSource;
        public ImplicitRegister(XRegCode destination)
        {
            HasSource = false;
            Destination = new ControlUnit.RegisterHandle(destination, RegisterTable.GP);
        }
        public ImplicitRegister(XRegCode destination, XRegCode source)
        {
            HasSource = true;
            Destination = new ControlUnit.RegisterHandle(destination, RegisterTable.GP);
            Source = new ControlUnit.RegisterHandle(source, RegisterTable.GP);
        }
        public List<string> Disassemble(RegisterCapacity size) 
            => HasSource ?
              new List<string> { Disassembly.DisassembleRegister(Destination, size, ControlUnit.RexByte), Disassembly.DisassembleRegister(Source, size, ControlUnit.RexByte) } 
            : new List<string> { Disassembly.DisassembleRegister(Destination, size, ControlUnit.RexByte) };

        public List<byte[]> Fetch(RegisterCapacity length) 
            => HasSource ?
              new List<byte[]>() { ControlUnit.FetchRegister(Destination, length), ControlUnit.FetchRegister(Source, length) }
            : new List<byte[]>() { ControlUnit.FetchRegister(Destination, length) };
        public void Set(byte[] data) => ControlUnit.SetRegister(Destination, data);
        public void SetSource(byte[] data)
        {
            if (HasSource) {
                ControlUnit.SetRegister(Source, data);
            }
            else
            {
                new System.Exception("ImplicitRegister.cs Attempt to set value of ImplicitRegister without source");
            }            
        }
    }
}
