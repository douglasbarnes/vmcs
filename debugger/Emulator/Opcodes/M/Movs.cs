namespace debugger.Emulator.Opcodes
{
    public class Movs : Opcode
    {
        readonly byte[] SourceBytes;
        public Movs(DecodedTypes.StringOperation input, OpcodeSettings settings = OpcodeSettings.NONE) : base("MOVS", input, RegisterCapacity.GP_QWORD, settings)
        {            
            SourceBytes = Fetch()[1];
        } 
        public override void Execute()
        {
            Set(SourceBytes);
        }
    }
}
