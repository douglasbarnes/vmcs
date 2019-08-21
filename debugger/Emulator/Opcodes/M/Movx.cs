using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Movx : Opcode
    {
        readonly byte[] Result;
        public Movx(DecodedTypes.IMyDecoded input, string mnemonic, bool signExtend, RegisterCapacity sourceSize, OpcodeSettings settings=OpcodeSettings.NONE) : base(mnemonic, input, settings)
        {
            RegisterCapacity DestSize = Capacity;
            Capacity = sourceSize; // hacky workaround for taking half the size then extending that
            byte[] SourceBytes = Fetch()[1];
            Capacity = DestSize;
            Result = (signExtend) ? Bitwise.SignExtend(SourceBytes, (byte)Capacity) : Bitwise.ZeroExtend(SourceBytes, (byte)Capacity);           
        }
        public override void Execute()
        {
            Set(Result);
        }
    }

}
