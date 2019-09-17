namespace debugger.Emulator.Opcodes
{
    public class Stos : StringOperation 
    {
        byte[] SourceBytes;
        public Stos(StringOpSettings settings = StringOpSettings.NONE) 
            : base("STOS", new DecodedTypes.MultiRegisterHandle(XRegCode.DI, XRegCode.A),  settings) {  }
        protected override void OnInitialise()
        {
            SourceBytes = Fetch()[1];
        }
        protected override void OnExecute()
        {
            Set(SourceBytes);
            AdjustDI();
        }
    }
}
