using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Mul : Opcode 
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        readonly List<byte[]> Operands;
        readonly int NumOfOperands;
        public Mul(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base((settings | OpcodeSettings.SIGNED) == settings ? "IMUL" : "MUL", input, settings)
        {
            Operands = Fetch();
            NumOfOperands = Operands.Count;
            if(NumOfOperands == 1)
            {
                Operands.Add(ControlUnit.FetchRegister(XRegCode.A, Capacity));
            }
            //imul opcodes come in the format,
            //imul rm1 : a = upper(rm1*a) d = lower(rm1*a)
            //imul rm1 rm2 : rm1 = cut(rm1*rm2, rm1.length)
            //imul rm1 rm2 imm : rm1 = cut(rm2*imm, rm1.length) (rm1 and rm2 always have the same length)
            ResultFlags = Bitwise.Multiply(Operands[Operands.Count-1], Operands[Operands.Count-2],(Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity, out Result); 
        }
        public override void Execute()
        {
            if(NumOfOperands == 1)
            {
                if(Capacity == RegisterCapacity.BYTE)
                {
                    ControlUnit.SetRegister(XRegCode.A, Result);//everything goes into ax
                }
                else
                {
                    ControlUnit.SetRegister(XRegCode.A, Bitwise.Cut(Result, (int)Capacity)); // higher bytes to d
                    ControlUnit.SetRegister(XRegCode.D, Bitwise.Subarray(Result, (int)Capacity)); // lower to a=
                }
                
            }
            else
            {
                Set(Bitwise.Cut(Result, (int)Capacity));
            }   
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
