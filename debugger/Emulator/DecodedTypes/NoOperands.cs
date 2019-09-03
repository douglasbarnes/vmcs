using System;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public class NoOperands : IMyDecoded
    {
        private static readonly Exception NoOperandsException = new Exception("Invalid operation on a NoOperands input encoding");
        public RegisterCapacity Size { get { throw NoOperandsException; } private set; }
        public void Initialise(RegisterCapacity size) { }
        public List<string> Disassemble() => new List<string>();
        public List<byte[]> Fetch() => new List<byte[]>();
        public void Set(byte[] data) => throw NoOperandsException;
    }    
}
