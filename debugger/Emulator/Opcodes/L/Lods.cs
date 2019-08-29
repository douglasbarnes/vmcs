namespace debugger.Emulator.Opcodes
{
    public class Lods : StringOperation
    {
        readonly byte[] SourceBytes;
        public Lods(StringOpSettings settings = StringOpSettings.NONE) : base("LODS", settings | StringOpSettings.A_DEST)
        {
            SourceBytes = Fetch()[1];
        }
        protected override void OnExecute()
        {
            Set(SourceBytes);
        }
    }
}
