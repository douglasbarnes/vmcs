namespace debugger.Emulator.Opcodes
{
    public class Stos : StringOperation 
    {
        readonly byte[] SourceBytes;
        public Stos(StringOpSettings settings = StringOpSettings.NONE) : base("STOS", settings | StringOpSettings.A_SRC)
        {
            SourceBytes = Fetch()[1];
        }
        protected override void OnExecute()
        {
            Set(SourceBytes);
        }
    }
}
