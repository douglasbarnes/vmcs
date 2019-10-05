using System.Collections.Generic;
namespace debugger.Emulator.Opcodes
{ 
    public interface IMyOpcode
    {
        public void Execute();
        public List<string> Disassemble();
    }
}
