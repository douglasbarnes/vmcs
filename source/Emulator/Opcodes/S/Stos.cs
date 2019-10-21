// Store string, set *DI to $A in a string operation fashion. $RDI will be increased by the length of bytes moved(or decreased if
// the direction flag is set). See StringOperation.
namespace debugger.Emulator.Opcodes
{
    public class Stos : StringOperation
    {
        public Stos(StringOpSettings settings = StringOpSettings.NONE)

            // Set up the destination to be DI and source to be A. DI will automatically be
            // treat as a pointer by the base class.
            : base("STOS", XRegCode.DI, XRegCode.A, settings) { }
        protected override void OnInitialise()
        {
        }
        protected override void OnExecute()
        {
            // Fetch()[1] by convention will be the source bytes from the A register.
            Set(Fetch()[1]);

            // Change DI by the amount of bytes that were fetched(automatically handled by base class).
            AdjustDI();
        }
    }
}
