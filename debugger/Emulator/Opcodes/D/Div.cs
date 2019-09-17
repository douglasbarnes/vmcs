using System;
using System.Collections.Generic;
using debugger.Util;
namespace debugger.Emulator.Opcodes
{
    public class Div : Opcode 
    {
        private readonly byte[] Buffer;
        private readonly DecodedTypes.IMyMultiDecoded Input;
        public Div(DecodedTypes.IMyMultiDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) 
            : base((settings | OpcodeSettings.SIGNED) == settings ? "IDIV" : "DIV", input, settings)
        {            
            List<byte[]> DestSource = Fetch();
            int HalfLength = (int)Capacity;
            Buffer = new byte[2 * HalfLength];
            byte[] Quotient;
            byte[] Modulo;
            if((settings | OpcodeSettings.SIGNED) == settings)
            {
                DestSource[1] = Bitwise.SignExtend(DestSource[1], (byte)((byte)Capacity * 2));
            }
            else
            {
                DestSource[1] = Bitwise.ZeroExtend(DestSource[1], (byte)((byte)Capacity * 2));
            }
            Bitwise.Divide(DestSource[0], DestSource[1], (Settings | OpcodeSettings.SIGNED) == Settings, (int)Capacity*2, out Quotient, out Modulo);
            Array.Copy(Quotient, Buffer, HalfLength);
            Array.Copy(Modulo, 0, Buffer, HalfLength - 1, HalfLength);
        }
        public override void Execute()
        {
            Set(Buffer);
        }
    }
}
