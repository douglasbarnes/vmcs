namespace debugger.Emulator.Opcodes
{
    public class Lods : StringOperation
    {
        byte[] SourceBytes;
        public Lods(StringOpSettings settings = StringOpSettings.NONE) 
            : base("LODS", new DecodedTypes.MultiRegisterHandle(XRegCode.A, XRegCode.SI),settings)
        {
        }
        protected override void OnInitialise()
        {
            SourceBytes = Fetch()[1];
        }
        protected override void OnExecute()
        {
            Set(SourceBytes);
            AdjustSI();
        }
    }
}
