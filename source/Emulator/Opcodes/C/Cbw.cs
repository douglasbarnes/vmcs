// Cbw; Convert byte to word, convert word to dword, convert dword to qword. All of these are short forms of
// sign extending the A register. Its behaviour is no different from movsx aside from this. See Movx.
// Technichally it is a "Zero operands" instruction, but for simplicity when dealing with the base class,
// it considers the A register as an implied operand.
// The operand's capacity will always be one word size greater than what will be extended, i.e the capacity
// will be the size to sign extend to. This makes the process a lot simpler in many ways, as there would have
// to be a way to derive the capacity. The derivation of a capacity in the base class follows this pattern already,
// so it would only be creating more work for no gain.
using debugger.Util;

namespace debugger.Emulator.Opcodes
{
    public class Cbw : Opcode
    {
        public Cbw(OpcodeSettings settings = OpcodeSettings.NONE) : base("CBW", new ControlUnit.RegisterHandle(XRegCode.A, RegisterTable.GP), settings)
        {            
            
        }
        public override void Execute()
        {
            // Fetch the operand. There will only be one because it is always the A register which is destination and source.
            byte[] Bytes = Fetch()[0];

            // Half the fetched bytes(because the capacity is that of the destination, see summary), and sign extend it
            // back to its former size.
            // E.g, in pseudo,
            //  CBW()
            //  {
            //      Bytes = $AX;
            //      Bytes = Cut($Bytes, 1);
            //      AX = $Bytes;
            //  }
            // From here it would be very simple to create CWDE and CDQE methods. 
            // Fortunately because of the interface the Opcode base class provides, this can be generalised.
            Set(Bitwise.SignExtend(Bitwise.Cut(Bytes, (int)Capacity / 2), (byte)Capacity));
        }
    }
}
