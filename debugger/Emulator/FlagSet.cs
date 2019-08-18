using System.Linq;
using debugger.Util;
namespace debugger.Emulator
{
    public enum FlagState
    {
        Undefined=0,
        Off=1,
        On=2,        
    }
    public struct FlagSet
    {

        public FlagState Carry;
        public FlagState Auxiliary;
        public FlagState Overflow;  // true = overflow
        public FlagState Zero;     // zero = false
        public FlagState Sign;     // false = positive
        public FlagState Parity;   
        public FlagSet(FlagState InitialiseAs = FlagState.Undefined)
        {
            Carry = InitialiseAs;
            Auxiliary = InitialiseAs;
            Overflow = InitialiseAs; // true = overflow
            Zero = InitialiseAs; // zero = false
            Sign = InitialiseAs; // false = positive
            Parity = InitialiseAs;
        }
        public FlagSet(byte[] input) //Auto calculate zf/sf/pf
        {
            Carry = FlagState.Undefined;
            Auxiliary = FlagState.Undefined;
            Overflow = FlagState.Undefined;
            Zero = input.IsZero() ? FlagState.On : FlagState.Off;
            Sign = input.IsNegative() ? FlagState.On : FlagState.Off;
            Parity = Bitwise.GetBits(input).Count(x => x == 1) % 2 == 0 ? FlagState.On : FlagState.Off; //parity: even no of 1 bits       
        }
        public void Overlap(FlagSet input)
        {
            Carry = input.Carry == FlagState.Undefined ? Carry : input.Carry;
            Auxiliary = input.Auxiliary == FlagState.Undefined ? Auxiliary : input.Auxiliary;
            Overflow = input.Overflow == FlagState.Undefined ? Overflow : input.Overflow;
            Zero = input.Zero == FlagState.Undefined ? Zero : input.Zero;
            Sign = input.Sign == FlagState.Undefined ? Sign : input.Sign;
            Parity = input.Parity == FlagState.Undefined ? Parity : input.Parity;
        }
    }
}
