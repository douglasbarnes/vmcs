using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Stos : Opcode 
    {
        readonly byte[] SourceBytes;
        public Stos(DecodedTypes.StringOperation input, OpcodeSettings settings = OpcodeSettings.NONE) : base("STOS", input, settings)
        {
            SourceBytes = ControlUnit.FetchRegister(XRegCode.A, Capacity);
            input.IncrementDI(Capacity);
        }
        public override void Execute()
        {
            Set(SourceBytes);
        }
        public override List<string> Disassemble()
        {
            List<string> Output = base.Disassemble();
            Output[1] = Disassembly.DisassembleRegister(XRegCode.A, Capacity, REX.NONE);
            return Output;
        }
    }
}
