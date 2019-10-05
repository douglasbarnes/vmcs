using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Cbw : Opcode
    {
        readonly byte[] ExtendedBytes;
        public Cbw(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("CBW", input, settings)
        {
            byte[] Bytes = Fetch()[0];
            ExtendedBytes = Bitwise.SignExtend(Bitwise.Cut(Bytes, (int)Capacity/2), (byte)Capacity);
        } 
        public override void Execute()
        {
            Set(ExtendedBytes);
        }
    }
}
