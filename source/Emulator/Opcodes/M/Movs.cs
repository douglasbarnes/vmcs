// Move string, a string operation for moving a string of bytes from one place to another. 
namespace debugger.Emulator.Opcodes
{
    public class Movs : StringOperation
    {
       
        public Movs(StringOpSettings settings = StringOpSettings.NONE)
            : base("MOVS", XRegCode.DI, XRegCode.SI, settings) { }
        protected override void OnInitialise()
        {

        }
        protected override void OnExecute()
        {
            // Set the destination to the source. The source is stored in Fetch()[1] by convention.
            Set(Fetch()[1]);

            // Both must be adjusted as the opcode uses both.
            AdjustDI();
            AdjustSI();
        }
    }
}
