using System.Collections.Generic;
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Movx : Opcode
    {
        readonly byte[] Result;
        readonly DecodedTypes.IMyDecoded Input;
        readonly RegisterCapacity DestSize;
        readonly RegisterCapacity SourceSize;
        public Movx(DecodedTypes.IMyDecoded input, string mnemonic, bool signExtend, RegisterCapacity desiredSourceSize, OpcodeSettings settings=OpcodeSettings.NONE) : base(mnemonic, input, settings)
        {
            Input = input;
            SourceSize = desiredSourceSize;
            DestSize = Capacity;
            Capacity = desiredSourceSize; 
            byte[] SourceBytes = Fetch()[1];
            Capacity = DestSize;
            Result = (signExtend) ? Bitwise.SignExtend(SourceBytes, (byte)Capacity) : Bitwise.ZeroExtend(SourceBytes, (byte)Capacity);           
        }
        public override void Execute()
        {
            Set(Result);
        }
        public override List<string> Disassemble()
        {
            if(Input is DecodedTypes.ModRM modrm)
            {
                modrm.Initialise(SourceSize);
                modrm.Source.Size = (DestSize);
            }            
            return base.Disassemble();
        }
    }

}
