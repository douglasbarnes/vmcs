using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Scas : StringOperation 
    {
        readonly FlagSet ResultFlags;
        public Scas(StringOpSettings settings = StringOpSettings.NONE) : base("SCAS", settings | StringOpSettings.A_SRC | StringOpSettings.COMPARE)
        {
            List<byte[]> Operands = Fetch();
            ResultFlags = Bitwise.Subtract(Operands[1], Operands[0], (int)Capacity, out _);
        }
        protected override void OnExecute()
        {
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
