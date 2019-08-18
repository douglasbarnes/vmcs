using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Mul : Opcode 
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        readonly List<byte[]> Operands;
        public Mul(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.None) : base((settings | OpcodeSettings.IsSigned) == settings ? "IMUL" : "MUL", input, settings)
        {
            Operands = Fetch();
            if(Operands.Count == 1)
            {
                Operands.Add(ControlUnit.FetchRegister(ByteCode.A, Capacity));
            }
            //imul opcodes come in the format,
            //imul rm1 rm2 : rm1 = cut(rm1*rm2, rm1.length)
            //imul rm1 rm2 imm : rm1 = cut(rm2*imm, rm1.length) (rm1 and rm2 always have the same length)
            ResultFlags = Bitwise.Multiply(Operands[Operands.Count], Operands[Operands.Count-1],(Settings | OpcodeSettings.IsSigned) == Settings, (int)Capacity, out Result); 
        }
        public override void Execute()
        {
            if(Operands.Count == 1)
            {
                if(Capacity == RegisterCapacity.BYTE)
                {
                    ControlUnit.SetRegister(ByteCode.A, Result);//everything goes into ax
                }
                else
                {
                    ControlUnit.SetRegister(ByteCode.A, Bitwise.Cut(Result, (int)Capacity)); // higher bytes to d
                    ControlUnit.SetRegister(ByteCode.D, Bitwise.Subarray(Result, (int)Capacity)); // lower to a=
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
