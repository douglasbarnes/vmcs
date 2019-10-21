// A simple interface for an opcode-like object. The two existing currently are Opcode and StringOperation. This interface
// was required because they both have the same outputs, but they have internal methods that act differently. So long as
// these methods are provided, an opcode will work fine with ControlUnit, given that the code is functional.
using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{
    public interface IMyOpcode
    {
        public void Execute();
        public List<string> Disassemble();
    }
}
