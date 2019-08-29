namespace debugger.Emulator.Opcodes
{
    public class Movs : StringOperation
    {
        readonly byte[] SourceBytes;
        public Movs(StringOpSettings settings = StringOpSettings.NONE) : base("MOVS", settings)
        {            
            SourceBytes = Fetch()[1];
        } 
        protected override void OnExecute()
        {
            Set(SourceBytes);
        }
    }
}
