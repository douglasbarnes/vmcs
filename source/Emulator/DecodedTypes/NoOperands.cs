// A NoOperands input method is very restricted in what it can do.
// As far as I know, in every case a NoOperands is used, there is no alternative input method for said opcode. However for
// compatability with the Opcode base class, it still inherits from IMyDecoded. This means its safe to throw an exception
// whenever certain methods are called(except Initialise() of course). In the Intel manual pre-2019, a NoOperands is referred
// to as "NP". In the may 2019 release, it is called "ZO". I can't find anywhere that officially confirms the names, however
// "No Parameters" and "Zero Operands" seem most likely. I don't really like the sound of either of those, so as the naming
// is not officially states, I went for a happy medium.
using System;
using System.Collections.Generic;
namespace debugger.Emulator.DecodedTypes
{
    public class NoOperands : IMyDecoded
    {

        private static readonly Exception NoOperandsException = new Exception("Invalid operation on a NoOperands input encoding");
        public RegisterCapacity Size
        {
            get
            {
                throw NoOperandsException;
            }
        }
        public void Initialise(RegisterCapacity size) { }
        public List<string> Disassemble() => new List<string>();
        public List<byte[]> Fetch() => new List<byte[]>();
        public void Set(byte[] data) => throw NoOperandsException;
    }
}
