namespace debugger.Emulator.Opcodes
{
    public class Stos : StringOperation 
    {
        byte[] SourceBytes;
        public Stos(StringOpSettings settings = StringOpSettings.NONE) : base("STOS", settings | StringOpSettings.A_SRC) {  }
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
