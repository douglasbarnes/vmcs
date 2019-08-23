using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Scas : Opcode 
    {
        readonly byte[] SourceBytes;
        readonly FlagSet ResultFlags;
        public Scas(DecodedTypes.StringOperation input, OpcodeSettings settings = OpcodeSettings.NONE) : base("SCAS", input, settings)
        {
            SourceBytes = ControlUnit.FetchRegister(XRegCode.A, Capacity);
            ResultFlags = Bitwise.Subtract(SourceBytes, Fetch()[0], (int)Capacity, out _);
            input.IncrementDI(Capacity);
        }
        public override void Execute()
        {
            ControlUnit.SetFlags(ResultFlags);
        }
        public override List<string> Disassemble()
        {
            List<string> Output = base.Disassemble();
            Output[1] = Disassembly.DisassembleRegister(XRegCode.A, Capacity, REX.NONE);
            return Output;
        }
    }
}
