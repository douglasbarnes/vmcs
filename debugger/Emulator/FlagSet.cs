using System.Linq;
using debugger.Util;
namespace debugger.Emulator
{
    public enum FlagState
    {
        UNDEFINED=0,
        OFF=1,
        ON=2,        
    }
    public struct FlagSet
    {
        public FlagState Carry;
        public FlagState Auxiliary;
        public FlagState Overflow;  // true = overflow
        public FlagState Zero;     // zero = false
        public FlagState Sign;     // false = positive
        public FlagState Parity;
        public FlagState Direction; // off = increment, on = decrement
        public FlagState Interrupt; //off = disallow on = allow
        public FlagSet(FlagState InitialiseAs = FlagState.UNDEFINED)
        {
            Carry = InitialiseAs;
            Auxiliary = InitialiseAs;
            Overflow = InitialiseAs; // true = overflow
            Zero = InitialiseAs; // zero = false
            Sign = InitialiseAs; // false = positive
            Parity = InitialiseAs;
            Direction = InitialiseAs;
            Interrupt = InitialiseAs;
        }
        public void Set(FlagState SetTo)
        {
            this = new FlagSet(SetTo);
        }
        public FlagSet(byte[] input) //Auto calculate zf/sf/pf
        {
            Carry = FlagState.UNDEFINED;
            Auxiliary = FlagState.UNDEFINED;
            Overflow = FlagState.UNDEFINED;
            Direction = FlagState.UNDEFINED;
            Interrupt = FlagState.UNDEFINED;
            Zero = input.IsZero() ? FlagState.ON : FlagState.OFF;
            Sign = input.IsNegative() ? FlagState.ON : FlagState.OFF;
            Parity = Bitwise.GetBits(input[0]).Count(x => x == '1') % 2 == 0 ? FlagState.ON : FlagState.OFF; //parity: even no of 1 bits       
        }
        public void Overlap(FlagSet input)
        {
            Carry = input.Carry == FlagState.UNDEFINED ? Carry : input.Carry;
            Auxiliary = input.Auxiliary == FlagState.UNDEFINED ? Auxiliary : input.Auxiliary;
            Overflow = input.Overflow == FlagState.UNDEFINED ? Overflow : input.Overflow;
            Zero = input.Zero == FlagState.UNDEFINED ? Zero : input.Zero;
            Sign = input.Sign == FlagState.UNDEFINED ? Sign : input.Sign;
            Parity = input.Parity == FlagState.UNDEFINED ? Parity : input.Parity;
        }
        public bool EqualsOrUndefined(FlagSet toCompare)
        => And(toCompare) == ToString();
        public string And(FlagSet toCompare)
        {
            string Output = "";
            Output += (Carry & toCompare.Carry) == FlagState.ON ? "CF" : "";
            Output += (Overflow & toCompare.Overflow) == FlagState.ON ? "OF" : "";
            Output += (Sign & toCompare.Sign) == FlagState.ON ? "SF" : "";
            Output += (Zero & toCompare.Zero) == FlagState.ON ? "ZF" : "";
            Output += (Auxiliary & toCompare.Auxiliary) == FlagState.ON ? "AF" : "";
            Output += (Parity & toCompare.Parity) == FlagState.ON ? "PF" : "";
            return Output;                                   
        }
        public FlagState this[string name]
        {
            get
            {
                switch(name.ToLower())
                {
                    case "carry":
                        return Carry;
                    case "sign":
                        return Sign;                        
                    case "overflow":
                        return Overflow; 
                    case "parity":
                        return Parity; 
                    case "zero":
                        return Zero; 
                    case "auxiliary":
                        return Auxiliary;
                    default:
                        throw new System.Exception("Flag name invalid");
                }
            
            }
            set
            {
                switch (name.ToLower())
                {
                    case "carry":
                        Carry = value;
                        return;
                    case "sign":
                        Sign = value;
                        return;
                    case "overflow":
                        Overflow = value;
                        return;
                    case "parity":
                        Parity = value;
                        return;
                    case "zero":
                        Zero = value;
                        return;
                    case "auxiliary":
                        Auxiliary = value;
                        return;
                    default:
                        throw new System.Exception("Flag name invalid");
                }
            }
        }
        public override string ToString()
        {
            string Output = "";
            Output += Carry == FlagState.ON ? "CF" : "";
            Output += Overflow == FlagState.ON ? "OF" : "";
            Output += Sign == FlagState.ON ? "SF" : "";
            Output += Zero == FlagState.ON ? "ZF" : "";
            Output += Auxiliary == FlagState.ON ? "AF" : "";
            Output += Parity == FlagState.ON ? "PF" : "";
            return Output;
        }
    }
}
