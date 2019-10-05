using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Scas : StringOperation 
    {
        FlagSet ResultFlags;
        public Scas(StringOpSettings settings = StringOpSettings.NONE)
            : base("SCAS", XRegCode.DI, XRegCode.A, settings | StringOpSettings.COMPARE)
        {            
        }
        protected override void OnInitialise()
        {
            List<byte[]> Operands = Fetch();
            ResultFlags = Bitwise.Subtract(Operands[1], Operands[0], (int)Capacity, out _);
        }
        protected override void OnExecute()
        {
            ControlUnit.SetFlags(ResultFlags);
            AdjustDI();
        }
    }
}
