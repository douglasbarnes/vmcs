using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class Mov : Opcode
    {
        readonly byte[] SourceBytes;
        public Mov(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base("MOV", input, settings)
        {
            List<byte[]> Operands = Fetch();
            SourceBytes = Operands[Operands.Count-1];
        } 
        public override void Execute()
        {
            Set(SourceBytes);
        }
    }
}
