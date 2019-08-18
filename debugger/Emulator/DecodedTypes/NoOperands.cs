using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace debugger.Emulator.DecodedTypes
{
    class NoOperands : IMyDecoded
    {
        public string[] Disassemble(RegisterCapacity size) => new string[0];
        public List<byte[]> Fetch(RegisterCapacity length) => throw new Exception("NoOperands.cs Attempt to fetch from no operands encoding");
        public void Set(byte[] data) => throw new Exception("NoOperands.cs Attempt to set a no operands encoding");
    }    
}
