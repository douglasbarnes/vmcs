using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Div : Opcode 
    {
        readonly byte[] Quotient;
        readonly byte[] Modulo;
        public Div(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base((settings | OpcodeSettings.SIGNED) == settings ? "IDIV" : "DIV", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            if(DestSource.Count == 1)
            {
                DestSource.Add(ControlUnit.FetchRegister(XRegCode.A, Capacity));
            }
            // always a reg, atleast for this opcode
            Bitwise.Divide(DestSource[0], DestSource[1], (Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity, out Quotient, out Modulo);
        }
        public override void Execute()
        {
            if(Capacity == RegisterCapacity.GP_BYTE)
            {
                ControlUnit.SetRegister(XRegCode.A, Quotient);
                ControlUnit.SetRegister(XRegCode.SP, Modulo);//ah
            } else
            {
                ControlUnit.SetRegister(XRegCode.A, Quotient);             
                ControlUnit.SetRegister(XRegCode.D, Modulo);
            }
                        
        }
    }
}
