using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.DecodedTypes
{
    class ImplicitRegister : IMyDecoded
    {
        private readonly ByteCode Destination;
        private readonly Immediate NextImmediate;
        private readonly bool HasImmediate;
        public ImplicitRegister(ByteCode destination)
        {// O Encoding
            Destination = destination;
            HasImmediate = false;
        }
        //public ImplicitRegister(ByteCode destination, Immediate extraImmediate)
        //{//OI encoding
        //    Destination = destination;
        //    NextImmediate = extraImmediate;
        //    HasImmediate = true;
        //}
        public string[] Disassemble(RegisterCapacity size)
        {
            if(HasImmediate)
            {
                return new string[] { Disassembly.DisassembleRegister(Destination, size), Core.Atoi(NextImmediate.Fetch(size)) };
            } else
            {
                return new string[] { Disassembly.DisassembleRegister(Destination, size) };
            }
        }

        public List<byte[]> Fetch(RegisterCapacity length)
        {
            List<byte[]> Output = new List<byte[]>() { ControlUnit.FetchRegister(Destination, length) };
            if(HasImmediate)
            {
                Output.Add(NextImmediate.Fetch(length)[0]);
            }
            return Output;
        }
        public void Set(byte[] data) => ControlUnit.SetRegister(Destination, data);
    }
}
