// Load string. Load a string pointed to by the SI register into the A register.
namespace debugger.Emulator.Opcodes
{
    public class Lods : StringOperation
    {
        public Lods(StringOpSettings settings = StringOpSettings.NONE)
            : base("LODS", XRegCode.A, XRegCode.SI, settings)
        {
        }
        protected override void OnInitialise()
        {
        }
        protected override void OnExecute()
        {
            // Set the destination to the source, in this case, set A to the bytes at SI.
            // The base class will automatically dereference the pointer.
            Set(Fetch()[1]);

            // SI was used therefore must be adjusted.
            AdjustSI();
        }
    }
}
