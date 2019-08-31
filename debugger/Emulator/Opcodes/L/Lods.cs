namespace debugger.Emulator.Opcodes
{
    public class Lods : StringOperation
    {
        byte[] SourceBytes;
        public Lods(StringOpSettings settings = StringOpSettings.NONE) : base("LODS", settings | StringOpSettings.A_DEST)
        {
        }
        protected override void OnInitialise()
        {
            SourceBytes = Fetch()[1];
        }
        protected override void OnExecute()
        {
            Set(SourceBytes);
        }
    }
}
