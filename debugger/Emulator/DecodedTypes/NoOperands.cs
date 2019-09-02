using System;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public class NoOperands : IMyDecoded
    {
        public List<string> Disassemble(RegisterCapacity size) => new List<string>();
        public List<byte[]> Fetch(RegisterCapacity length) => new List<byte[]>();
        public void Set(byte[] data) => throw new Exception("NoOperands.cs Attempt to set a no operands encoding");
        public void SetSource(byte[] data) => throw new Exception("NoOperands.cs Attempt to set a no operands encoding");
    }    
}
