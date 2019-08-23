using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Lods : Opcode 
    {
        readonly byte[] SourceBytes;
        public Lods(DecodedTypes.StringOperation input, OpcodeSettings settings = OpcodeSettings.NONE) : base("LODS", input, settings)
        {
            SourceBytes = Fetch()[1];
            input.IncrementSI(Capacity);
        }
        public override void Execute()
        {
            ControlUnit.SetRegister(XRegCode.A, SourceBytes, true);
        }
        public override List<string> Disassemble()
        {
            List<string> Output = base.Disassemble();
            Output[0] = Disassembly.DisassembleRegister(XRegCode.A, Capacity, REX.NONE);
            return Output;
        }
    }
}
